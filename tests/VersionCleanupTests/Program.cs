global using System.Text.RegularExpressions;
using Bloxstrap.Utility;

string root = Path.Combine(Path.GetTempPath(), "angestrap-cleanup-tests-" + Guid.NewGuid());
Directory.CreateDirectory(root);
int passed = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); passed++; }
string Folder(string name) { string path = Path.Combine(root, name); Directory.CreateDirectory(path); File.WriteAllText(Path.Combine(path, "RobloxPlayerBeta.exe"), "test executable"); return path; }
var logs = new List<string>();
void Log(string message, Exception? exception) => logs.Add(message);
try
{
    string player = Folder("version-1111111111111111");
    string studio = Folder("version-2222222222222222");
    string running = Folder("version-3333333333333333");
    string obsolete = Folder("version-4444444444444444");
    string unrelated = Folder("user-files");
    string invalid = Folder("version-invalid");
    string locked = Folder("version-5555555555555555");
    using (var stream = File.Open(Path.Combine(locked, "RobloxPlayerBeta.exe"), FileMode.Open, FileAccess.Read, FileShare.None))
    {
        VersionCleanup.Clean(root, new[] { player.ToUpperInvariant(), studio, running }, Log);
        Check(!Directory.Exists(obsolete), "Obsolete version was retained");
        Check(Directory.Exists(player) && Directory.Exists(studio) && Directory.Exists(running), "Active version was deleted");
        Check(Directory.Exists(unrelated) && Directory.Exists(invalid), "Unrecognized directory was deleted");
        Check(File.Exists(Path.Combine(locked, "RobloxPlayerBeta.exe")), "Locked executable was changed");
    }
    VersionCleanup.Clean(root, new[] { player, studio, running }, Log);
    Check(!Directory.Exists(locked), "Unlocked obsolete version was not retried");
    string bothClients = Folder("version-9999999999999999");
    string studioExecutable = Path.Combine(bothClients, "RobloxStudioBeta.exe");
    File.WriteAllText(studioExecutable, "test studio");
    using (var stream = File.Open(studioExecutable, FileMode.Open, FileAccess.Read, FileShare.None))
    {
        VersionCleanup.Clean(root, new[] { player, studio, running }, Log);
        Check(File.Exists(Path.Combine(bothClients, "RobloxPlayerBeta.exe")) && File.Exists(studioExecutable), "Locked Studio left a partially deleted installation");
    }

    string outside = Folder("outside");
    string linked = Path.Combine(root, "version-6666666666666666");
    Directory.CreateSymbolicLink(linked, outside);
    string nested = Folder("version-7777777777777777");
    Directory.CreateSymbolicLink(Path.Combine(nested, "content"), outside);
    VersionCleanup.Clean(root, new[] { player, studio, running }, Log);
    Check(Directory.Exists(linked) && Directory.Exists(nested) && File.Exists(Path.Combine(outside, "RobloxPlayerBeta.exe")), "Symlink directory was traversed or deleted");
    string linkedRoot = Path.Combine(root, "linked-root");
    Directory.CreateSymbolicLink(linkedRoot, root);
    string protectedByRootLink = Folder("version-aaaaaaaaaaaaaaaa");
    VersionCleanup.Clean(linkedRoot, Array.Empty<string>(), Log);
    Check(Directory.Exists(protectedByRootLink), "Linked root was cleaned");
    Directory.Delete(linkedRoot);
    VersionCleanup.Clean(Path.Combine(root, "missing"), Array.Empty<string>(), Log);
    Check(true, "Missing root handling");
    // Invalid protected paths must abort cleanup instead of falling back to deletion.
    string unknown = Folder("version-8888888888888888");
    VersionCleanup.Clean(root, new[] { "\0" }, Log);
    Check(Directory.Exists(unknown), "Failed protection lookup still deleted versions");
    Console.WriteLine($"{passed} cleanup checks passed");
}
finally
{
    Directory.Delete(root, true);
}
