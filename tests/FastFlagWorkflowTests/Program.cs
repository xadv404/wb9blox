using Bloxstrap;
using Bloxstrap.UI.ViewModels.Settings;
using Bloxstrap.Enums.FlagPresets;

Paths.Base = Path.Combine(Path.GetTempPath(), "angestrap-profile-workflow-" + Guid.NewGuid());
int passed = 0;
void Check(bool condition, string reason) { if (!condition) throw new Exception(reason); passed++; }
try
{
    App.FastFlags.Prop["CustomFlag"] = "preserve";
    var model = new FastFlagsViewModel();
    int saves = 0;
    EventHandler save = (_, _) => { saves++; App.FastFlags.Save(); App.Settings.Save(); };
    model.RequestSaveSettingsEvent += save;
    var notifications = new List<string?>();
    model.PropertyChanged += (_, change) => notifications.Add(change.PropertyName);

    foreach (string profile in FastFlagProfiles.Profiles.Keys)
    {
        model.SelectedProfile = profile;
        model.ApplyProfileCommand.Execute(null);
        Check(App.FastFlags.HasCurrentConfigurationOnDisk(), "Applied profile was not persisted");
        Check(App.Settings.HasCurrentConfigurationOnDisk() && App.Settings.Prop.UseFastFlagManager, "Flag manager was not enabled and saved");
        var reloaded = new FastFlagManager();
        reloaded.Load();
        foreach (var entry in FastFlagProfiles.Profiles[profile])
            Check(reloaded.GetValue(entry.Key) == entry.Value, "Reload lost a profile flag");
        Check(reloaded.GetValue("CustomFlag") == "preserve", "Profile erased unrelated custom flags");
        Check(model.ProfileFeedback.Contains("enregistré"), "Successful save not reported");
        Check(new FastFlagsViewModel().SelectedProfile == profile, "Reopening the page lost the applied profile selection");
    }
    Check(saves == 4, "Apply did not invoke save exactly once per profile");
    Check(notifications.Contains(nameof(model.SelectedMSAALevel)) && notifications.Contains(nameof(model.SelectedTextureQuality)), "Profile controls were not refreshed");
    model.SelectedProfile = "Performances";
    model.ApplyProfileCommand.Execute(null);
    Check(model.SelectedMSAALevel == MSAAMode.x1 && model.SelectedTextureQuality == TextureQuality.Level0, "Selected settings do not reflect the profile");
    var original = new Dictionary<string, object>(App.FastFlags.Prop);
    model.ResetConfiguration = true;
    Check(App.FastFlags.Prop.Count == 0 && model.ResetConfiguration, "Reset did not keep a reversible backup");
    model.ResetConfiguration = false;
    Check(original.Count == App.FastFlags.Prop.Count && original.All(pair => App.FastFlags.GetValue(pair.Key) == pair.Value.ToString()), "Reset lost the original profile");

    model.RequestSaveSettingsEvent -= save;
    model.SelectedProfile = "Qualité visuelle";
    model.ApplyProfileCommand.Execute(null);
    Check(!App.FastFlags.HasCurrentConfigurationOnDisk() && model.ProfileFeedback.Contains("échoué"), "An unsaved profile was reported as saved");
    model.RequestSaveSettingsEvent += (_, _) => throw new IOException("fixture save failure");
    model.ApplyProfileCommand.Execute(null);
    Check(model.ProfileFeedback.StartsWith("Impossible"), "Save exception escaped or was reported as success");

    Console.WriteLine($"{passed} profile workflow checks passed");
}
finally
{
    if (Directory.Exists(Paths.Base)) Directory.Delete(Paths.Base, true);
}
