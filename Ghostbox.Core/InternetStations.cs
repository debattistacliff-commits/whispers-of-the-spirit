using System.Net.Http.Headers;
using System.Text.Json;

namespace Ghostbox.Core;

public sealed record InternetStation(string Name, string Url, string Language, string Country)
{
    public override string ToString() => Name;
}

public static class InternetStations
{
    public static async Task<InternetStation[]> FindAsync(string language, CancellationToken token)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(18) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GqrxGhostbox", "1.3"));
        string filter = language switch { "mt" => "countrycode=MT", "it" => "language=italian", _ => "language=english" };
        Exception? last = null;
        foreach (string mirror in new[] { "de1.api.radio-browser.info", "nl1.api.radio-browser.info", "at1.api.radio-browser.info" }.OrderBy(_ => Random.Shared.Next()))
        {
            token.ThrowIfCancellationRequested();
            try
            {
                string json = await http.GetStringAsync($"https://{mirror}/json/stations/search?{filter}&hidebroken=true&order=clickcount&reverse=true&limit=40", token);
                using var doc = JsonDocument.Parse(json);
                var result = new List<InternetStation>();
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    string url = item.GetProperty("url_resolved").GetString() ?? "";
                    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) continue;
                    if (result.Any(station => station.Url == url)) continue;
                    result.Add(new(item.GetProperty("name").GetString() ?? "Unnamed station", url,
                        item.GetProperty("language").GetString() ?? "", item.GetProperty("country").GetString() ?? ""));
                }
                if (result.Count > 0)
                {
                    if (language == "mt")
                    {
                        var maltese = result.Where(s => s.Language.Contains("malt", StringComparison.OrdinalIgnoreCase)).ToArray();
                        if (maltese.Length > 0) return maltese;
                    }
                    return result.ToArray();
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException) { last = ex; }
        }
        throw new IOException("Station directory unavailable. Try again or enter a direct live stream URL.", last);
    }
}
