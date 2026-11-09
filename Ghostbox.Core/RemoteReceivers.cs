using System.Text.Json;

namespace Ghostbox.Core;

public sealed record RemoteReceiver(string Name, string Url, string Location, long MinimumHz, long MaximumHz)
{
    public override string ToString() => $"{Name} — {Location}";
}

public static class RemoteReceivers
{
    static RemoteReceiver[]? cached;
    static DateTime cachedAt;
    public static async Task<RemoteReceiver[]> FindAsync(CancellationToken token)
    {
        if (cached is not null && DateTime.UtcNow - cachedAt < TimeSpan.FromMinutes(30)) return cached;
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("GqrxGhostbox/1.4");
        string json = await http.GetStringAsync("https://cdn.aethersdr.com/kiwi.json", token);
        if (json.Length > 32_000_000) throw new IOException("Receiver directory is too large.");
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.GetProperty("schema").GetInt32() != 1) throw new IOException("Unsupported receiver-directory format.");
        var result = new List<RemoteReceiver>();
        foreach (var r in doc.RootElement.GetProperty("receivers").EnumerateArray())
        {
            if (!r.TryGetProperty("ext_api", out var api) || !api.TryGetInt32(out int allowance) || allowance <= 0) continue;
            if (r.TryGetProperty("offline", out var offline) && offline.ValueKind == JsonValueKind.True) continue;
            if (r.TryGetProperty("flagged", out var flagged) && flagged.ValueKind == JsonValueKind.True) continue;
            if (r.TryGetProperty("users", out var users) && r.TryGetProperty("users_max", out var max) && users.GetInt32() >= max.GetInt32()) continue;
            if (!r.TryGetProperty("bands", out var bands) || bands.GetArrayLength() != 2) continue;
            long low = (long)bands[0].GetDouble(), high = (long)bands[1].GetDouble();
            if (high <= low || low > 531000 || high < 1602000) continue;
            string url = r.GetProperty("url").GetString() ?? "";
            if (!Uri.TryCreate(url, UriKind.Absolute, out var address) || address.Scheme is not ("http" or "https")) continue;
            result.Add(new(r.GetProperty("name").GetString() ?? address.Host, address.GetLeftPart(UriPartial.Authority), r.TryGetProperty("loc", out var loc) ? loc.GetString() ?? "" : "", Math.Max(1000, low), Math.Min(30000000, high)));
        }
        if (result.Count == 0) throw new IOException("No API-enabled receivers with free channels found.");
        cached = result.OrderBy(r => r.Location.Contains("UK", StringComparison.OrdinalIgnoreCase) || r.Location.Contains("England", StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(r => r.Name).ToArray();
        cachedAt = DateTime.UtcNow; return cached;
    }
}
