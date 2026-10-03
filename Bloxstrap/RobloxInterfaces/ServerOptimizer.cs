using System.Net.Http.Json;

namespace Bloxstrap.RobloxInterfaces
{
    public sealed class PublicServer
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("playing")] public int Playing { get; set; }
        [JsonPropertyName("maxPlayers")] public int MaxPlayers { get; set; }
        [JsonPropertyName("ping")] public double? Ping { get; set; }
        [JsonPropertyName("fps")] public double? Fps { get; set; }
    }

    public sealed class PublicServerPage
    {
        [JsonPropertyName("data")] public List<PublicServer> Data { get; set; } = new();
        [JsonPropertyName("nextPageCursor")] public string? NextPageCursor { get; set; }
    }

    public static class ServerOptimizer
    {
        public static bool TryParsePlace(string input, out long id)
        {
            input = input.Trim();
            if (long.TryParse(input, out id))
                return id > 0;
            if (!Uri.TryCreate(input, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
                !(uri.Host.Equals("roblox.com", StringComparison.OrdinalIgnoreCase) || uri.Host.Equals("www.roblox.com", StringComparison.OrdinalIgnoreCase)))
                return false;
            var match = Regex.Match(uri.AbsolutePath, @"^/games/(\d+)(?:/|$)");
            return match.Success && long.TryParse(match.Groups[1].Value, out id) && id > 0;
        }

        public static PublicServer? SelectBest(IEnumerable<PublicServer> servers) => servers
            .Where(server => Guid.TryParse(server.Id, out _) && server.MaxPlayers > 0 &&
                server.Playing >= 0 && server.Playing < server.MaxPlayers &&
                server.Ping is double ping && double.IsFinite(ping) && ping >= 0)
            .OrderBy(server => server.Ping)
            .ThenByDescending(server => server.Fps is double fps && double.IsFinite(fps) ? fps : 0)
            .ThenBy(server => server.Playing)
            .FirstOrDefault();

        public static async Task<PublicServer?> FindBestAsync(HttpClient client, long placeId, CancellationToken token)
        {
            if (placeId <= 0)
                throw new ArgumentOutOfRangeException(nameof(placeId));

            var servers = new List<PublicServer>();
            var seenCursors = new HashSet<string>();
            string? cursor = null;
            // Bound requests and respect API failures/rate limits rather than scraping indefinitely.
            for (int pageNumber = 0; pageNumber < 3; pageNumber++)
            {
                string url = $"https://games.roblox.com/v1/games/{placeId}/servers/Public?sortOrder=Asc&excludeFullGames=true&limit=100";
                if (cursor is not null)
                    url += "&cursor=" + Uri.EscapeDataString(cursor);
                var page = await client.GetFromJsonAsync<PublicServerPage>(url, token)
                    ?? throw new InvalidDataException("Réponse Roblox vide.");
                if (page.Data is null)
                    throw new InvalidDataException("Liste des serveurs Roblox absente.");
                servers.AddRange(page.Data);
                cursor = page.NextPageCursor;
                if (string.IsNullOrEmpty(cursor) || !seenCursors.Add(cursor))
                    break;
            }
            return SelectBest(servers);
        }

        public static string GetJoinUri(long placeId, string serverId)
        {
            if (placeId <= 0 || !Guid.TryParse(serverId, out var id))
                throw new ArgumentException("Identifiant de jeu ou de serveur invalide.");
            return $"roblox://experiences/start?placeId={placeId}&gameInstanceId={id:D}";
        }
    }
}
