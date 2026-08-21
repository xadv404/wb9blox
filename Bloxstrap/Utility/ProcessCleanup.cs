using System.Windows;

namespace Bloxstrap.Utility
{
    static class ProcessCleanup
    {
        static readonly string[] RobloxProcessNames =
        {
            App.RobloxPlayerAppName,
            App.RobloxStudioAppName,
            "RobloxCrashHandler",
        };

        public static bool TryCleanupRunningInstances()
        {
            const string LOG_IDENT = "ProcessCleanup::TryCleanupRunningInstances";

            if (!ShouldRun())
                return true;

            var angestrapProcesses = GetOtherAngestrapProcesses();
            var robloxProcesses = GetRobloxProcesses();

            if (angestrapProcesses.Count == 0 && robloxProcesses.Count == 0)
                return true;

            if (!ConfirmCleanup(angestrapProcesses.Count, robloxProcesses.Count))
            {
                App.Logger.WriteLine(LOG_IDENT, "User cancelled process cleanup");
                return false;
            }

            CloseProcesses(angestrapProcesses, LOG_IDENT);
            CloseProcesses(robloxProcesses, LOG_IDENT);

            Thread.Sleep(500);
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

            return true;
        }

        static List<Process> GetOtherAngestrapProcesses() =>
            Process.GetProcessesByName(App.ProjectName)
                .Where(process => process.Id != Environment.ProcessId)
                .ToList();

        static List<Process> GetRobloxProcesses()
        {
            var processes = new List<Process>();

            foreach (string processName in RobloxProcessNames)
                processes.AddRange(Process.GetProcessesByName(processName));

            return processes;
        }

        static bool ConfirmCleanup(int angestrapCount, int robloxCount)
        {
            var details = new List<string>();

            if (angestrapCount > 0)
                details.Add(String.Format(Strings.Dialog_ProcessCleanup_Angestrap, angestrapCount));

            if (robloxCount > 0)
                details.Add(String.Format(Strings.Dialog_ProcessCleanup_Roblox, robloxCount));

            string message = String.Format(
                Strings.Dialog_ProcessCleanup_Message,
                String.Join("\n", details));

            var result = Frontend.ShowMessageBox(
                message,
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo,
                MessageBoxResult.No);

            return result == MessageBoxResult.Yes;
        }

        static void CloseProcesses(IEnumerable<Process> processes, string logIdent)
        {
            foreach (var process in processes)
            {
                try
                {
                    if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                        process.CloseMainWindow();

                    if (!process.WaitForExit(3000))
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
