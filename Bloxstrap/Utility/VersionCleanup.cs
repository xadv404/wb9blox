namespace Bloxstrap.Utility
{
    /// <summary>Removes obsolete version folders while preserving active installations.</summary>
    public static class VersionCleanup
    {
        public static void Clean(string root, IEnumerable<string> retainedDirectories, Action<string, Exception?> log)
        {
            if (!Directory.Exists(root))
                return;

            try
            {
                for (var ancestor = new DirectoryInfo(root); ancestor is not null; ancestor = ancestor.Parent)
                {
                    if ((ancestor.Attributes & FileAttributes.ReparsePoint) != 0)
                        return;
                }

                var retained = new HashSet<string>(retainedDirectories
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(path => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                    StringComparer.OrdinalIgnoreCase);

                foreach (string directory in Directory.GetDirectories(root))
                {
                    string name = Path.GetFileName(directory);
                    // Leave caches, user folders, and unrecognized deployment layouts alone.
                    if (!Regex.IsMatch(name, "^version-[a-fA-F0-9]{16}$") ||
                        retained.Contains(Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))
                        continue;

                    try
                    {
                        if (ContainsReparsePoint(directory))
                            continue;

                        // Probe both clients without deleting an executable first. A failed
                        // cleanup must not leave an otherwise usable installation incomplete.
                        foreach (string executable in new[] { "RobloxPlayerBeta.exe", "RobloxStudioBeta.exe" })
                        {
                            string path = Path.Combine(directory, executable);
                            if (File.Exists(path))
                            {
                                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None);
                            }
                        }

                        Directory.Delete(directory, true);
                        log($"Deleted unused Roblox version {directory}", null);
                    }
                    catch (Exception ex)
                    {
                        // Locked files are retried at the next launch; never stop Roblox startup.
                        log($"Could not remove Roblox version {directory}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                log($"Could not inspect Roblox versions in {root}", ex);
            }
        }

        private static bool ContainsReparsePoint(string directory)
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                return true;

            foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0 ||
                    ((attributes & FileAttributes.Directory) != 0 && ContainsReparsePoint(entry)))
                    return true;
            }

            return false;
        }
    }
}
