using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace Ghostbox.Core;

public enum ScanPattern { Forward, Backward, Bounce, Random }

public sealed record ScanSettings(string Host = "127.0.0.1", int Port = 7356,
    long MinimumHz = 87_500_000, long MaximumHz = 108_000_000,
    long StepHz = 100_000, int DwellMs = 100, string Mode = "WFM", int? PassbandHz = null,
    int TimeoutMs = 3000, ScanPattern Pattern = ScanPattern.Forward)
{
    public static readonly string[] Modes = ["WFM", "WFM_ST", "FM", "AM", "USB", "LSB", "CW", "CWL", "CWU", "RAW"];
    public long PointCount => (MaximumHz - MinimumHz) / StepHz + 1;
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host)) throw new ArgumentException("Enter a Gqrx host name or IP address.");
        if (Port is < 1 or > 65535) throw new ArgumentException("Port must be 1–65535.");
        if (MinimumHz <= 0 || MaximumHz < MinimumHz || MaximumHz > 10_000_000_000_000)
            throw new ArgumentException("Use a positive frequency range up to 10 THz, with maximum ≥ minimum.");
        if (StepHz <= 0) throw new ArgumentException("Step must be greater than zero.");
        if (DwellMs is < 10 or > 60000) throw new ArgumentException("Dwell must be 10–60,000 ms.");
        if (TimeoutMs is < 100 or > 60000) throw new ArgumentException("Timeout must be 100–60,000 ms.");
        if (!Modes.Contains(Mode)) throw new ArgumentException("Select a supported demodulator.");
        if (PassbandHz is <= 0) throw new ArgumentException("Passband must be positive or empty for automatic.");
        if (!Enum.IsDefined(Pattern)) throw new ArgumentException("Select a scan pattern.");
    }

    public static long ParseFrequency(string text)
    {
        var match = System.Text.RegularExpressions.Regex.Match(text.Trim(),
            @"^(\d+(?:\.\d*)?|\.\d+)\s*(Hz|kHz|MHz|GHz|k|m|g)?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success) throw new ArgumentException("Use Hz or a suffix, such as 87.5M or 100k.");
        decimal scale = match.Groups[2].Value.ToLowerInvariant() switch
        { "k" or "khz" => 1000, "m" or "mhz" => 1000000, "g" or "ghz" => 1000000000, _ => 1 };
        if (!decimal.TryParse(match.Groups[1].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
            throw new ArgumentException("Frequency is too large.");
        try
        {
            var hz = value * scale;
            if (hz != decimal.Truncate(hz) || hz <= 0 || hz > 10_000_000_000_000)
                throw new ArgumentException("Frequency must be positive whole Hz, up to 10 THz.");
            return (long)hz;
        }
        catch (OverflowException) { throw new ArgumentException("Frequency is too large."); }
    }
}

public sealed class ScanSequence
{
    readonly ScanSettings settings;
    readonly Random random;
    long index;
    int direction;
    public ScanSequence(ScanSettings settings, Random? random = null)
    {
        settings.Validate();
        this.settings = settings;
        this.random = random ?? Random.Shared;
        index = this.random.NextInt64(settings.PointCount);
        direction = settings.Pattern == ScanPattern.Bounce ? (this.random.Next(2) == 0 ? -1 : 1)
            : settings.Pattern == ScanPattern.Backward ? -1 : 1;
    }
    public long Next()
    {
        long frequency = settings.MinimumHz + index * settings.StepHz;
        var count = settings.PointCount;
        switch (settings.Pattern)
        {
            case ScanPattern.Random: index = random.NextInt64(count); break;
            case ScanPattern.Bounce:
                if (count > 1)
                {
                    if (index + direction < 0 || index + direction >= count) direction = -direction;
                    index += direction;
                }
                break;
            default: index = (index + direction + count) % count; break;
        }
        return frequency;
    }
}

public sealed class GqrxConnection : IAsyncDisposable
{
    readonly TcpClient client = new();
    readonly int timeoutMs;
    NetworkStream? stream;
    public GqrxConnection(int timeoutMs) => this.timeoutMs = timeoutMs;
    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(timeoutMs);
        try
        {
            await client.ConnectAsync(host, port, timeout.Token);
            client.NoDelay = true;
            stream = client.GetStream();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new TimeoutException("Gqrx connection timed out. Check the host, port and remote-control settings."); }
    }
    public async Task CommandAsync(string command, CancellationToken cancellationToken)
    {
        if (stream is null) throw new InvalidOperationException("Not connected.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(timeoutMs);
        try
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes(command + "\n"), timeout.Token);
            // One bounded reply per command; never send a new command before its acknowledgement.
            var response = new StringBuilder();
            var one = new byte[1];
            while (true)
            {
                if (await stream.ReadAsync(one, timeout.Token) == 0) throw new IOException("Gqrx disconnected.");
                if (one[0] == '\n') break;
                if (response.Length >= 4096) throw new IOException("Oversized Gqrx reply.");
                response.Append((char)one[0]);
            }
            if (response.ToString().Trim() != "RPRT 0")
                throw new IOException($"Gqrx rejected {command}: {response}");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new TimeoutException("Gqrx did not acknowledge the command in time."); }
    }
    public ValueTask DisposeAsync() { client.Dispose(); return ValueTask.CompletedTask; }
}

public static class Scanner
{
    public static async Task RunAsync(ScanSettings settings, IProgress<long> frequency, CancellationToken cancellationToken)
    {
        settings.Validate();
        await using var remote = new GqrxConnection(settings.TimeoutMs);
        await remote.ConnectAsync(settings.Host.Trim(), settings.Port, cancellationToken);
        await remote.CommandAsync($"M {settings.Mode}" + (settings.PassbandHz is int bandwidth ? $" {bandwidth}" : ""), cancellationToken);
        var sequence = new ScanSequence(settings);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long hz = sequence.Next();
            await remote.CommandAsync($"F {hz}", cancellationToken);
            frequency.Report(hz);
            await Task.Delay(settings.DwellMs, cancellationToken);
        }
    }
}
