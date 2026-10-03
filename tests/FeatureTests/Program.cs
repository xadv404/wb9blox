global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using Bloxstrap.RobloxInterfaces;
using Bloxstrap.Utility;

int passed = 0;
void Check(bool value, string message) { if (!value) throw new Exception(message); passed++; }
PublicServer Server(int ping, int playing = 1, int max = 10) => new() { Id = Guid.NewGuid().ToString(), Ping = ping, Playing = playing, MaxPlayers = max, Fps = 60 };
Check(ServerOptimizer.TryParsePlace("https://www.roblox.com/games/1818/Crossroads", out var parsed) && parsed == 1818, "Game URL not parsed");
Check(ServerOptimizer.TryParsePlace("1818", out parsed) && parsed == 1818, "Place ID not parsed");
Check(!ServerOptimizer.TryParsePlace("https://example.org/games/1818", out _), "Foreign game URL accepted");
Check(!ServerOptimizer.TryParsePlace("-1", out _), "Negative place accepted");
var slow = Server(200);
var fast = Server(20);
Check(ServerOptimizer.SelectBest(new[] { slow, Server(1, 10), fast }) == fast, "Full server selected or minimum ping not found");
Check(ServerOptimizer.SelectBest(new[] { Server(-1), new PublicServer { Id = "bad", Ping = 0, MaxPlayers = 10 } }) is null, "Invalid server accepted");
var missingPing = Server(0); missingPing.Ping = null;
Check(ServerOptimizer.SelectBest(new[] { missingPing }) is null, "Missing ping invented");
var nan = Server(0); nan.Ping = double.NaN;
Check(ServerOptimizer.SelectBest(new[] { nan }) is null, "NaN ping accepted");
var tied = Server(20); tied.Fps = 30;
Check(ServerOptimizer.SelectBest(new[] { tied, fast }) == fast, "Tie did not prefer higher FPS");
Check(ServerOptimizer.GetJoinUri(1818, fast.Id).Contains($"gameInstanceId={fast.Id}"), "Wrong launch URI");
try { ServerOptimizer.GetJoinUri(0, fast.Id); throw new Exception("Invalid place accepted"); } catch (ArgumentException) { passed++; }

int requests = 0;
using (var client = new HttpClient(new FakeHandler(request =>
{
    requests++;
    Check(request.RequestUri!.Host == "games.roblox.com", "Wrong API host");
    if (requests == 2) Check(request.RequestUri.Query.Contains("cursor=a%2Bb"), "Cursor not escaped");
    string body = requests == 1
        ? $"{{\"data\":[{{\"id\":\"{slow.Id}\",\"playing\":1,\"maxPlayers\":10,\"ping\":200}}],\"nextPageCursor\":\"a+b\"}}"
        : $"{{\"data\":[{{\"id\":\"{fast.Id}\",\"playing\":1,\"maxPlayers\":10,\"ping\":20}}],\"nextPageCursor\":null}}";
    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
})))
{
    var best = await ServerOptimizer.FindBestAsync(client, 1818, CancellationToken.None);
    Check(best?.Id == fast.Id && requests == 2, "Pagination missed best server");
}
using (var client = new HttpClient(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests))))
{
    try { await ServerOptimizer.FindBestAsync(client, 1, CancellationToken.None); throw new Exception("Rate limit ignored"); }
    catch (HttpRequestException ex) { Check(ex.StatusCode == HttpStatusCode.TooManyRequests, "Wrong rate limit result"); }
}
int cappedRequests = 0;
using (var client = new HttpClient(new FakeHandler(_ =>
{
    cappedRequests++;
    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{{\"data\":[],\"nextPageCursor\":\"{cappedRequests}\"}}", Encoding.UTF8, "application/json") };
})))
{
    Check(await ServerOptimizer.FindBestAsync(client, 1, CancellationToken.None) is null && cappedRequests == 3, "Unbounded server queries");
    using var cancel = new CancellationTokenSource(); cancel.Cancel();
    try { await ServerOptimizer.FindBestAsync(client, 1, cancel.Token); throw new Exception("Cancellation ignored"); }
    catch (OperationCanceledException) { passed++; }
}
var flags = new Dictionary<string, object> { ["CustomFlag"] = "keep", ["DFIntTextureQualityOverride"] = "3" };
FastFlagProfiles.Apply(flags, "Performances");
Check((string)flags["CustomFlag"] == "keep" && (string)flags["FIntDebugForceMSAASamples"] == "1" && (string)flags["DFIntTextureQualityOverride"] == "0", "Performance profile incorrect");
FastFlagProfiles.Apply(flags, "Qualité visuelle");
Check((string)flags["DFIntTextureQualityOverride"] == "3", "Quality profile incorrect");
FastFlagProfiles.Apply(flags, "Roblox par défaut");
Check(flags.Count == 1 && flags.ContainsKey("CustomFlag"), "Default profile removed custom flags");
try { FastFlagProfiles.Apply(flags, "unknown"); throw new Exception("Unknown profile accepted"); }
catch (ArgumentException) { Check(flags.Count == 1, "Unknown profile mutated flags"); }

string file = Path.GetTempFileName();
try
{
    await File.WriteAllTextAsync(file, "verified update fixture");
    string hash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(file)));
    Check(UpdateIntegrity.ParseChecksum($"{hash.ToLowerInvariant()}  Angestrap.exe\n", "Angestrap.exe") == hash.ToLowerInvariant(), "Checksum parse failed");
    await UpdateIntegrity.VerifyAsync(file, hash); passed++;
    try { UpdateIntegrity.ParseChecksum($"{hash}  Other.exe", "Angestrap.exe"); throw new Exception("Wrong checksum filename accepted"); }
    catch (InvalidDataException) { passed++; }
    await File.AppendAllTextAsync(file, "tampered");
    try { await UpdateIntegrity.VerifyAsync(file, hash); throw new Exception("Tampered update accepted"); }
    catch (InvalidDataException) { passed++; }
    try { await UpdateIntegrity.VerifyAsync(file, "invalid"); throw new Exception("Invalid hash accepted"); }
    catch (InvalidDataException) { passed++; }
}
finally { File.Delete(file); }
Console.WriteLine($"{passed} feature checks passed");
if (args.Length == 2 && args[0] == "--live-place")
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    var selected = await ServerOptimizer.FindBestAsync(client, long.Parse(args[1]), CancellationToken.None);
    if (selected is null)
        throw new Exception("Live Roblox API returned no eligible server with a ping");
    Console.WriteLine($"Live Roblox API: selected a non-full server with reported ping {selected.Ping} ms ({selected.Playing}/{selected.MaxPlayers} players)");
}

sealed class FakeHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> respond;
    public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => this.respond = respond;
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(respond(request));
    }
}
