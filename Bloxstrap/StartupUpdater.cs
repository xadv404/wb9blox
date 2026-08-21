using Microsoft.Win32;

using Bloxstrap.Models.Persistable;
using Bloxstrap.Utility;

namespace Bloxstrap
{
    static class StartupUpdater
    {
        const string LOG_IDENT = "StartupUpdater";

        // Wait until after login so boot stays responsive; only one lightweight HTTP check runs.
        const int StartupDelayMs = 15000;

        public static int Run(string[] args)
        {
            try
            {
                string? installLocation = GetInstallLocation();

                if (installLocation is null)
                    return 0;

                Paths.Initialize(installLocation);

                var settings = LoadSettings();

                if (settings is null || !settings.CheckForUpdates)
                    return 0;

                using var mutex = new InterProcessLock("StartupUpdater");

                if (!mutex.IsAcquired)
                    return 0;

                if (Process.GetProcessesByName(App.ProjectName).Length > 1)
                    return 0;

                try
                {
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.BelowNormal;
                }
                catch
                {
                    // best effort only
                }

                Thread.Sleep(StartupDelayMs);

                App.Logger.Initialize();
                App.LaunchSettings = new LaunchSettings(args);
                AppUpdater.ConfigureHttpClient();
                App.Settings.Load(false);

                bool updateStarted = AppUpdater.CheckAndApplyUpdateAsync(quiet: true).GetAwaiter().GetResult();

                return updateStarted ? 0 : 0;
            }
            catch (Exception ex)
            {
                try
                {
                    if (App.Logger.Initialized)
                    {
                        App.Logger.WriteLine(LOG_IDENT, "Startup update check failed");
                        App.Logger.WriteException(LOG_IDENT, ex);
                    }
                }
                catch
                {
                    // ignore logging failures during startup
                }

                return 0;
            }
        }

        static string? GetInstallLocation()
        {
            using var key = Registry.CurrentUser.OpenSubKey(App.UninstallKey);

            if (key?.GetValue("InstallLocation") is not string location || !Directory.Exists(location))
                return null;

            return location;
        }

        static Settings? LoadSettings()
        {
            string settingsPath = Path.Combine(Paths.Base, "Settings.json");

            if (!File.Exists(settingsPath))
                return new Settings();

            try
            {
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsPath));
            }
            catch
            {
                return null;
            }
        }
    }
}
