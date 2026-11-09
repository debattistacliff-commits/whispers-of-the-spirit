using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Ghostbox.Core;
using LibVLCSharp.Shared;

namespace Ghostbox.App;

public static class InternetRadioPlayer
{
    public static async Task RunAsync(InternetStation[] stations, ScanPattern pattern, int dwellSeconds, bool autoHop,
        Action<byte[]> receive, IProgress<string> status, CancellationToken token)
    {
        if (stations.Length == 0) throw new ArgumentException("Load or enter a real online radio station first.");
        LibVLCSharp.Shared.Core.Initialize();
        using var vlc = new LibVLC("--no-video", "--no-video-title-show", "--network-caching=600", "--http-reconnect");
        var sequence = new ScanSequence(new ScanSettings(MinimumHz: 1, MaximumHz: stations.Length, StepHz: 1, Pattern: pattern));
        int consecutiveFailures = 0;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            var station = stations[autoHop ? (int)sequence.Next() - 1 : 0];
            status.Report("Buffering: " + station.Name);
            var queue = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(12) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, SingleWriter = true });
            using var player = new MediaPlayer(vlc);
            player.SetAudioFormat("S16N", 48000, 1);
            player.SetAudioCallbacks((_, samples, count, _) =>
            {
                try
                {
                    if (token.IsCancellationRequested || count > 48000) return;
                    var pcm = new byte[checked((int)count * 2)]; Marshal.Copy(samples, pcm, 0, pcm.Length);
                    queue.Writer.TryWrite(pcm);
                }
                catch (Exception ex) { queue.Writer.TryComplete(ex); }
            }, null, null, null, null);
            player.EncounteredError += (_, _) => queue.Writer.TryComplete(new IOException("Stream could not be decoded: " + station.Name));
            player.EndReached += (_, _) => queue.Writer.TryComplete();
            using var media = new Media(vlc, new Uri(station.Url));
            media.AddOption(":no-video"); media.AddOption(":network-caching=600");
            media.AddOption(":http-user-agent=GqrxGhostbox/1.3");
            bool received = false;
            var audibleTime = new Stopwatch();
            try
            {
                if (!player.Play(media)) throw new IOException("Unable to open stream: " + station.Name);
                while (!autoHop || !received || audibleTime.Elapsed.TotalSeconds < dwellSeconds)
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                    timeout.CancelAfter(TimeSpan.FromSeconds(received ? 8 : 15));
                    byte[] pcm;
                    try { pcm = await queue.Reader.ReadAsync(timeout.Token); }
                    catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw new IOException("No incoming audio from " + station.Name); }
                    catch (ChannelClosedException) { throw new IOException("Station stream ended: " + station.Name); }
                    if (!received)
                    {
                        received = true; consecutiveFailures = 0; audibleTime.Start();
                        status.Report("LIVE INTERNET RADIO: " + station.Name);
                    }
                    receive(pcm);
                    await Task.Delay(TimeSpan.FromSeconds(pcm.Length / 96000.0), token);
                }
            }
            catch (Exception ex) when (autoHop && !token.IsCancellationRequested && ex is IOException)
            {
                status.Report(ex.Message + " • trying next station");
                if (++consecutiveFailures >= stations.Length) throw new IOException("None of the selected online stations supplied audio. Check your internet connection or load another station list.", ex);
            }
            finally { player.Stop(); queue.Writer.TryComplete(); }
        }
    }
}
