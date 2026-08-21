using Microsoft.Win32;

namespace Bloxstrap.Utility
{
    static class WindowsStartup
    {
        const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public static string StartupArguments => "-startupupdate -quiet";

        static string ValueName => App.ProjectName;

        public static void Sync()
        {
            if (!Paths.Initialized)
                return;

            if (App.Settings.Prop.CheckForUpdates && File.Exists(Paths.Application))
                Register();
            else
                Unregister();
        }

        public static void Register()
        {
            if (!File.Exists(Paths.Application))
                return;

            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
                key?.SetValue(ValueName, $"\"{Paths.Application}\" {StartupArguments}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("WindowsStartup::Register", "Failed to register startup entry");
                App.Logger.WriteException("WindowsStartup::Register", ex);
            }
        }

        public static void Unregister()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);

                if (key?.GetValue(ValueName) is not null)
                    key.DeleteValue(ValueName, false);
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("WindowsStartup::Unregister", "Failed to remove startup entry");
                App.Logger.WriteException("WindowsStartup::Unregister", ex);
            }
        }
    }
}
