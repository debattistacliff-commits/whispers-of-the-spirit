using System.Buffers.Binary;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;

namespace Ghostbox.Core;

public static class RemoteSdr
{
    public static readonly string[] Modes = ["AM", "USB", "LSB", "CW", "NFM"];
    public static async Task RunAsync(RemoteReceiver receiver, ScanSettings settings, string mode,
        Action<byte[]> receive, IProgress<long> tuned, IProgress<string> status, CancellationToken cancellationToken)
    {
        settings.Validate();
        if (!Modes.Contains(mode)) throw new ArgumentException("Select AM, USB, LSB, CW or narrow FM.");
        if (settings.MinimumHz < receiver.MinimumHz || settings.MaximumHz > receiver.MaximumHz)
            throw new ArgumentException($"This receiver covers {receiver.MinimumHz:N0}–{receiver.MaximumHz:N0} Hz. Broadcast FM/DAB needs a different receiver.");
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = lifetime.Token;
        using var ws = new ClientWebSocket();
        var origin = new Uri(receiver.Url);
        var endpoint = new UriBuilder(origin) { Scheme = origin.Scheme == "https" ? "wss" : "ws", Path = $"/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}/SND" }.Uri;
        using (var connect = CancellationTokenSource.CreateLinkedTokenSource(token))
        { connect.CancelAfter(12000); await ws.ConnectAsync(endpoint, connect.Token); }
        using var sendLock = new SemaphoreSlim(1);
        async Task Send(string command)
        {
            await sendLock.WaitAsync(token);
            try { await ws.SendAsync(Encoding.ASCII.GetBytes(command), WebSocketMessageType.Text, true, token); }
            finally { sendLock.Release(); }
        }
        var sequence = new ScanSequence(settings);
        long frequency = sequence.Next();
        string nativeMode = mode == "NFM" ? "nbfm" : mode.ToLowerInvariant();
        (int low, int high) = mode switch { "USB" => (300, 2700), "LSB" => (-2700, -300), "CW" => (300, 700), "NFM" => (-6000, 6000), _ => (-4900, 4900) };
        async Task Tune(long hz)
        {
            await Send(FormattableString.Invariant($"SET mod={nativeMode} low_cut={low} high_cut={high} freq={hz / 1000.0:F3}"));
            tuned.Report(hz);
        }
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        double sampleRate = 12000;
        var resampler = new LinearPcmResampler();
        bool initialized = false, receivedAudio = false;
        await Send("SET auth t=kiwi p=");
        status.Report("Connecting to antenna receiver: " + receiver.Name);
        async Task ReceiveLoop()
        {
            var buffer = new byte[65536];
            DateTime keepalive = DateTime.MinValue;
            while (true)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(15000);
                int count = 0; WebSocketReceiveResult part;
                do
                {
                    if (count >= buffer.Length) throw new IOException("Remote receiver message too large.");
                    part = await ws.ReceiveAsync(new ArraySegment<byte>(buffer, count, buffer.Length - count), timeout.Token);
                    if (part.MessageType == WebSocketMessageType.Close) throw new IOException("Remote receiver closed the session.");
                    count += part.Count;
                } while (!part.EndOfMessage);
                if (count < 4) continue;
                string tag = Encoding.ASCII.GetString(buffer, 0, 3);
                if (tag == "MSG")
                {
                    foreach (string pair in Encoding.ASCII.GetString(buffer, 4, count - 4).Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] item = pair.Split('=', 2); string key = item[0], value = item.Length > 1 ? item[1] : "";
                        if (key is "too_busy" or "down" or "redirect") throw new IOException("Remote receiver unavailable: " + key + " " + value);
                        if (key == "badp" && value != "0") throw new IOException("Receiver rejected this session (password, API quota, or channel availability). Choose another public receiver.");
                        if (key == "audio_rate") await Send("SET AR OK in=" + value + " out=48000");
                        if (key == "sample_rate")
                        {
                            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out sampleRate) || sampleRate < 8000 || sampleRate > 48000) throw new IOException("Unsupported receiver audio rate.");
                            if (!initialized)
                            {
                                initialized = true;
                                await Send("SET ident_user=Ghostbox-sweeper");
                                await Send("SET compression=0");
                                await Send("SET agc=1 hang=0 thresh=-100 slope=6 decay=1000 manGain=50");
                                await Send("SET squelch=0 max=0"); await Send("SET gen=0 mix=-1");
                                await Tune(frequency); await Send("SET keepalive");
                            }
                        }
                    }
                }
                else if (tag == "SND" && initialized && count > 10)
                {
                    byte flags = buffer[3];
                    if ((flags & 0x18) != 0) continue; // Wait for requested uncompressed mono PCM.
                    byte[] pcm = resampler.ProcessBigEndian(buffer.AsSpan(10, count - 10), sampleRate);
                    if (!receivedAudio)
                    { receivedAudio = true; status.Report("LIVE REMOTE SDR ANTENNA AUDIO • " + receiver.Name); ready.TrySetResult(); }
                    receive(pcm);
                    if (DateTime.UtcNow - keepalive > TimeSpan.FromSeconds(2)) { await Send("SET keepalive"); keepalive = DateTime.UtcNow; }
                }
            }
        }
        async Task SweepLoop()
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(20), token);
            while (true) { await Task.Delay(settings.DwellMs, token); frequency = sequence.Next(); await Tune(frequency); }
        }
        var reading = ReceiveLoop(); var sweeping = SweepLoop();
        try { await await Task.WhenAny(reading, sweeping); }
        finally
        {
            lifetime.Cancel(); ws.Abort();
            try { await reading; } catch { }
            try { await sweeping; } catch { }
        }
    }
}

public sealed class LinearPcmResampler
{
    short previous;
    double position;
    public byte[] ProcessBigEndian(ReadOnlySpan<byte> input, double inputRate)
    {
        int samples = input.Length / 2;
        if (samples == 0) return [];
        var data = new short[samples + 1]; data[0] = previous;
        for (int i = 0; i < samples; i++) data[i + 1] = BinaryPrimitives.ReadInt16BigEndian(input.Slice(i * 2, 2));
        var output = new byte[(int)Math.Ceiling((samples + 1) * 48000 / inputRate) * 2]; int count = 0;
        while (position < samples)
        {
            int index = (int)position; double mix = position - index;
            short value = (short)Math.Clamp(Math.Round(data[index] * (1 - mix) + data[index + 1] * mix), short.MinValue, short.MaxValue);
            BinaryPrimitives.WriteInt16LittleEndian(output.AsSpan(count, 2), value); count += 2; position += inputRate / 48000;
        }
        position -= samples; previous = data[^1]; return output[..count];
    }
}
