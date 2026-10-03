// Only desktop UI dependencies are stubbed. The profile view model, flag manager,
// persistence implementation, and enum types are the actual application files.
global using System.Diagnostics;
global using System.Text;
global using System.Text.Json;
global using Bloxstrap.Utility;
global using Bloxstrap.Models.Attributes;
global using Bloxstrap.Resources;
global using Bloxstrap.Models.Persistable;
namespace Bloxstrap
{
    public static class Paths
    {
        public static string Base { get; set; } = "";
        public static string Modifications => Path.Combine(Base, "Modifications");
    }
    public sealed class TestLogger
    {
        public void WriteLine(string source, string message) { }
        public void WriteException(string source, Exception error) { }
    }
    public static class App
    {
        public static TestLogger Logger { get; } = new();
        public static FastFlagManager FastFlags { get; set; } = new();
        public static JsonManager<Settings> Settings { get; set; } = new();
    }
    public static class Frontend
    {
        public static void ShowMessageBox(string message, System.Windows.MessageBoxImage image) { }
    }
}
namespace Bloxstrap.Models.Persistable
{
    public sealed class Settings { public bool UseFastFlagManager { get; set; } }
}
namespace System.Windows { public enum MessageBoxImage { Warning } }
namespace Bloxstrap.Resources
{
    public static class Strings
    {
        public const string JsonManager_SettingsLoadFailed = "Settings load failed";
        public const string JsonManager_FastFlagsLoadFailed = "Flags load failed";
        public const string Bootstrapper_JsonManagerSaveFailed = "Could not save {0}: {1}";
    }
}
