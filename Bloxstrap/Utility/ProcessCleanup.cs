namespace Bloxstrap.Utility
{
    static class ProcessCleanup
    {
        public static bool TryCleanupRunningInstances()
        {
            const string LOG_IDENT = "ProcessCleanup::TryCleanupRunningInstances";

            if (!ShouldRun())
                return true;

            var catstrapProcesses = GetOtherCatstrapProcesses();

            if (catstrapProcesses.Count > 0)
                CloseProcesses(catstrapProcesses, LOG_IDENT);

            return true;
        }

        static bool ShouldRun()
        {
            if (App.LaunchSettings.QuietFlag.Active)
                return false;

            if (App.LaunchSettings.UpgradeFlag.Active)
                return false;

            if (App.LaunchSettings.BackgroundUpdaterFlag.Active)
                return false;

            if (App.LaunchSettings.WatcherFlag.Active)
                return false;

            if (App.LaunchSettings.UninstallFlag.Active)
                return false;

            if (App.LaunchSettings.RobloxLaunchMode != LaunchMode.None)
                return false;

            return true;
        }

        static List<Process> GetOtherCatstrapProcesses() =>
            Process.GetProcessesByName(App.ProjectName)
                .Where(process => process.Id != Environment.ProcessId)
                .ToList();

        static void CloseProcesses(IEnumerable<Process> processes, string logIdent)
        {
            foreach (var process in processes)
            {
                try
                {
                    if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                        process.CloseMainWindow();

                    if (!process.WaitForExit(1500))
                        process.Kill(true);

                    App.Logger.WriteLine(logIdent, $"Closed process {process.ProcessName} (PID {process.Id})");
                }
                catch (Exception ex)
                {
                    App.Logger.WriteLine(logIdent, $"Failed to close process {process.ProcessName} (PID {process.Id})");
                    App.Logger.WriteException(logIdent, ex);
                }
                finally
                {
                    process.Dispose();
                }
            }
        }
    }
}
