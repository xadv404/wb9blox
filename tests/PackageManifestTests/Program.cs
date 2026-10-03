using Bloxstrap.Models.Manifest;

static string Entry(string name) => $"{name}\nsignature\n10\n20\n";
int passed = 0;
foreach (var names in new[] {
    new[] { "RobloxPlayerLauncher.exe", "RobloxApp.zip", "Libraries.zip" },
    new[] { "RobloxApp.zip", "RobloxPlayerLauncher.exe", "Libraries.zip" },
    new[] { "RobloxApp.zip", "Libraries.zip", "RobloxPlayerLauncher.exe" },
    new[] { "RobloxApp.zip", "Libraries.zip" }
})
{
    var manifest = new PackageManifest("v0\n" + string.Concat(names.Select(Entry)));
    if (!manifest.Select(p => p.Name).SequenceEqual(new[] { "RobloxApp.zip", "Libraries.zip" }) ||
        manifest.Any(p => p.PackedSize != 10 || p.Size != 20 || p.Signature != "signature"))
        throw new Exception("Incorrect package manifest parsing");
    passed++;
}
try { new PackageManifest("v1\n"); throw new Exception("Unsupported version accepted"); }
catch (NotSupportedException) { passed++; }
Console.WriteLine($"{passed} manifest checks passed");

namespace Bloxstrap { public static class Paths { public static string Downloads => "/tmp"; } }
