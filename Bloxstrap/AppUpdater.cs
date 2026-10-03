using System.Windows;

using Bloxstrap.Utility;

namespace Bloxstrap
{
    static class AppUpdater
    {
        public static bool ShouldCheckOnRobloxLaunch()
        {
            if (!App.Settings.Prop.CheckForUpdates)
                return false;

            if (App.LaunchSettings.UpgradeFlag.Active)
                return false;

            if (App.LaunchSettings.BypassUpdateCheck)
                return false;

            if (App.LaunchSettings.BackgroundUpdaterFlag.Active)
                return false;

            return true;
        }

        public static async Task<GithubRelease?> GetAvailableUpdateAsync()
        {
            const string LOG_IDENT = "AppUpdater::GetAvailableUpdate";

#if DEBUG_UPDATER
            return null;
#else
            var releaseInfo = await App.GetLatestRelease();

            if (releaseInfo is null || String.IsNullOrWhiteSpace(releaseInfo.TagName) || releaseInfo.Assets is null)
                return null;

            var versionComparison = Utilities.CompareVersions(App.Version, releaseInfo.TagName);

            if (versionComparison == VersionComparison.GreaterThan)
            {
                App.Logger.WriteLine(LOG_IDENT, "Local version is newer than release");
                return null;
            }

            if (versionComparison == VersionComparison.Equal && App.IsProductionBuild)
            {
                App.Logger.WriteLine(LOG_IDENT, "No updates found");
                return null;
            }

            return releaseInfo;
#endif
        }

        public static async Task CheckForUpdatesFromAboutAsync()
        {
            const string LOG_IDENT = "AppUpdater::CheckForUpdatesFromAbout";

            if (Process.GetProcessesByName(App.ProjectName).Length > 1)
            {
                App.Logger.WriteLine(LOG_IDENT, $"More than one {App.ProjectName} instance running, aborting update check");
                Frontend.ShowMessageBox(Strings.Menu_AlreadyRunning_Title, MessageBoxImage.Warning);
                return;
            }

            App.Logger.WriteLine(LOG_IDENT, "Checking for updates...");

            var releaseInfo = await GetAvailableUpdateAsync();

            if (releaseInfo is null)
            {
                Frontend.ShowMessageBox(
                    String.Format(Strings.Dialog_AppUpdate_None, App.Version),
                    MessageBoxImage.Information);
                return;
            }

            var result = Frontend.ShowMessageBox(
                String.Format(Strings.Dialog_AppUpdate_Available, App.Version, releaseInfo.TagName),
                MessageBoxImage.Information,
                MessageBoxButton.YesNo,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                App.Logger.WriteLine(LOG_IDENT, "User declined update");
                return;
            }

            if (await ApplyUpdateAsync(releaseInfo, quiet: false))
                App.SoftTerminate();
        }

        public static async Task<bool> PromptAndApplyUpdateAsync(
            bool quiet,
            string[]? launchArgs = null,
            LaunchMode launchMode = LaunchMode.None)
        {
            const string LOG_IDENT = "AppUpdater::PromptAndApplyUpdate";

            if (Process.GetProcessesByName(App.ProjectName).Length > 1)
            {
                App.Logger.WriteLine(LOG_IDENT, $"More than one {App.ProjectName} instance running, aborting update check");
                return false;
            }

            App.Logger.WriteLine(LOG_IDENT, "Checking for updates...");

            var releaseInfo = await GetAvailableUpdateAsync();

            if (releaseInfo is null)
                return false;

            if (App.Settings.Prop.AutomaticallyInstallUpdates)
                return await ApplyUpdateAsync(releaseInfo, quiet, launchArgs, launchMode);

            if (quiet)
                return false;

            var result = Frontend.ShowMessageBox(
                String.Format(Strings.Dialog_AppUpdate_Available, App.Version, releaseInfo.TagName),
                MessageBoxImage.Information,
                MessageBoxButton.YesNo,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                App.Logger.WriteLine(LOG_IDENT, "User declined update");
                return false;
            }

            return await ApplyUpdateAsync(releaseInfo, quiet: false, launchArgs: launchArgs, launchMode: launchMode);
        }

        public static async Task<bool> CheckAndApplyUpdateAsync(
            bool quiet,
            Action<string>? setStatus = null,
            IBootstrapperDialog? dialog = null,
            LaunchMode launchMode = LaunchMode.None,
            string[]? launchArgs = null)
        {
            const string LOG_IDENT = "AppUpdater::CheckAndApplyUpdate";

            if (Process.GetProcessesByName(App.ProjectName).Length > 1)
            {
                App.Logger.WriteLine(LOG_IDENT, $"More than one {App.ProjectName} instance running, aborting update check");
                return false;
            }

            App.Logger.WriteLine(LOG_IDENT, "Checking for updates...");

            var releaseInfo = await GetAvailableUpdateAsync();

            if (releaseInfo is null)
                return false;

            if (dialog is not null)
                dialog.CancelEnabled = false;

            return await ApplyUpdateAsync(
                releaseInfo,
                quiet,
                launchArgs,
                launchMode,
                setStatus);
        }

        public static async Task<bool> ApplyUpdateAsync(
            GithubRelease releaseInfo,
            bool quiet,
            string[]? launchArgs = null,
            LaunchMode launchMode = LaunchMode.None,
            Action<string>? setStatus = null)
        {
            const string LOG_IDENT = "AppUpdater::ApplyUpdate";

            string version = releaseInfo.TagName;

            setStatus?.Invoke(Strings.Bootstrapper_Status_UpgradingBloxstrap);

            try
            {
#if DEBUG_UPDATER
                string downloadLocation = Path.Combine(Paths.TempUpdates, $"{App.ProjectName}.exe");

                Directory.CreateDirectory(Paths.TempUpdates);

                File.Copy(Paths.Process, downloadLocation, true);
#else
                if (!Regex.IsMatch(releaseInfo.TagName, @"\Av?\d+\.\d+\.\d+(?:\.\d+)?\z"))
                    throw new InvalidDataException("Invalid update version.");

                string executableName = $"{App.ProjectName}.exe";
                var asset = releaseInfo.Assets?.FirstOrDefault(x => x.Name == executableName);
                if (asset is null)
                    throw new InvalidDataException("Release has no Angestrap executable.");

                void ValidateAssetUrl(string url)
                {
                    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
                        uri.Host != "github.com" || !string.IsNullOrEmpty(uri.UserInfo) ||
                        !uri.AbsolutePath.StartsWith($"/{App.ProjectRepository}/releases/download/", StringComparison.Ordinal))
                        throw new InvalidDataException("Unexpected update download URL.");
                }

                ValidateAssetUrl(asset.BrowserDownloadUrl);
                string expectedHash;
                if (asset.Digest is string digest && Regex.IsMatch(digest, @"\Asha256:[a-fA-F0-9]{64}\z"))
                {
                    expectedHash = digest[7..];
                }
                else
                {
                    var checksumAsset = releaseInfo.Assets!.FirstOrDefault(x => x.Name == executableName + ".sha256")
                        ?? throw new InvalidDataException("Release has no integrity checksum.");
                    ValidateAssetUrl(checksumAsset.BrowserDownloadUrl);
                    string checksum = await App.HttpClient.GetStringAsync(checksumAsset.BrowserDownloadUrl);
                    expectedHash = UpdateIntegrity.ParseChecksum(checksum, executableName);
                }

                string downloadDirectory = Path.Combine(Paths.TempUpdates, releaseInfo.TagName);
                string downloadLocation = Path.Combine(downloadDirectory, executableName);
                Directory.CreateDirectory(downloadDirectory);

                if (File.Exists(downloadLocation))
                {
                    try { await UpdateIntegrity.VerifyAsync(downloadLocation, expectedHash); }
                    catch (InvalidDataException) { File.Delete(downloadLocation); }
                }

                if (!File.Exists(downloadLocation))
                {
                    string temporaryDownload = downloadLocation + ".download";
                    try
                    {
                        using var response = await App.HttpClient.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();
                        await using (var fileStream = new FileStream(temporaryDownload, FileMode.Create, FileAccess.Write))
                            await response.Content.CopyToAsync(fileStream);
                        await UpdateIntegrity.VerifyAsync(temporaryDownload, expectedHash);
                        File.Move(temporaryDownload, downloadLocation, overwrite: true);
                    }
                    finally
                    {
                        if (File.Exists(temporaryDownload))
                            File.Delete(temporaryDownload);
                    }
                }
#endif

                App.Logger.WriteLine(LOG_IDENT, $"Starting {version}...");

                ProcessStartInfo startInfo = new()
                {
                    FileName = downloadLocation,
                };

                startInfo.ArgumentList.Add("-upgrade");

                if (quiet)
                    startInfo.ArgumentList.Add("-quiet");

                foreach (string arg in launchArgs ?? App.LaunchSettings.Args)
                {
                    if (IsInternalLaunchArg(arg))
                        continue;

                    startInfo.ArgumentList.Add(arg);
                }

                if (launchMode == LaunchMode.Player && !startInfo.ArgumentList.Contains("-player"))
                    startInfo.ArgumentList.Add("-player");
                else if (launchMode == LaunchMode.Studio && !startInfo.ArgumentList.Contains("-studio"))
                    startInfo.ArgumentList.Add("-studio");

                App.Settings.Save();

                Process.Start(startInfo);

                return true;
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LOG_IDENT, "An exception occurred when running the auto-updater");
                App.Logger.WriteException(LOG_IDENT, ex);

                if (!quiet)
                {
                    Frontend.ShowMessageBox(
                        string.Format(Strings.Bootstrapper_AutoUpdateFailed, version),
                        MessageBoxImage.Information
                    );

                    Utilities.ShellExecute(App.ProjectDownloadLink);
                }
            }

            return false;
        }

        static bool IsInternalLaunchArg(string arg)
        {
            if (!arg.StartsWith('-'))
                return false;

            string identifier = arg[1..];

            return identifier.Equals("startupupdate", StringComparison.OrdinalIgnoreCase)
                || identifier.Equals("quiet", StringComparison.OrdinalIgnoreCase)
                || identifier.Equals("upgrade", StringComparison.OrdinalIgnoreCase);
        }

        public static void ConfigureHttpClient()
        {
            App.HttpClient.Timeout = TimeSpan.FromSeconds(30);

            if (App.HttpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                string userAgent = $"{App.ProjectName}/{App.Version}";

                if (App.IsActionBuild)
                {
                    if (App.IsProductionBuild)
                        userAgent += " (Production)";
                    else
                        userAgent += $" (Artifact {App.BuildMetadata.CommitHash}, {App.BuildMetadata.CommitRef})";
                }

                App.HttpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
            }
        }
    }
}
