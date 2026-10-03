using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Enums.FlagPresets;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class FastFlagsViewModel : NotifyPropertyChangedViewModel
    {
        private Dictionary<string, object>? _preResetFlags;
        public sealed record ProfileChoice(string Name, string Description);
        public IEnumerable<ProfileChoice> ProfileChoices { get; } = new[]
        {
            new ProfileChoice("Roblox par défaut", "Laisse Roblox choisir ses réglages graphiques."),
            new ProfileChoice("Performances", "Textures légères et anticrénelage minimal."),
            new ProfileChoice("Équilibré", "Un compromis entre détails et charge graphique."),
            new ProfileChoice("Qualité visuelle", "Textures détaillées et anticrénelage renforcé.")
        };
        private string _selectedProfile = "Équilibré";
        public FastFlagsViewModel()
        {
            var keys = FastFlagProfiles.Profiles.Values.SelectMany(profile => profile.Keys).Distinct().ToArray();
            var matching = FastFlagProfiles.Profiles.FirstOrDefault(profile => keys.All(key =>
                profile.Value.TryGetValue(key, out string? expected)
                    ? App.FastFlags.GetValue(key) == expected
                    : App.FastFlags.GetValue(key) is null));
            if (matching.Key is not null)
                _selectedProfile = matching.Key;
        }
        public string SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (string.IsNullOrEmpty(value) || !FastFlagProfiles.Profiles.ContainsKey(value))
                    return;
                _selectedProfile = value;
                OnPropertyChanged(nameof(SelectedProfile));
                ProfileFeedback = "";
                OnPropertyChanged(nameof(ProfileFeedback));
            }
        }
        public string ProfileFeedback { get; private set; } = "";
        public event EventHandler? RequestSaveSettingsEvent;
        public ICommand ApplyProfileCommand => new RelayCommand(ApplyProfile);

        private void ApplyProfile()
        {
            try
            {
                FastFlagProfiles.Apply(App.FastFlags.Prop, SelectedProfile);
                App.Settings.Prop.UseFastFlagManager = true;
                _preResetFlags = null;
                RequestSaveSettingsEvent?.Invoke(this, EventArgs.Empty);
                bool saved = App.FastFlags.HasCurrentConfigurationOnDisk() && App.Settings.HasCurrentConfigurationOnDisk();
                ProfileFeedback = saved
                    ? $"Profil « {SelectedProfile} » enregistré. Les flags sont visibles dans l’éditeur ; redémarre Roblox via Angestrap pour les utiliser."
                    : "Les flags ont été ajoutés, mais leur enregistrement a échoué. Vérifie les droits du dossier puis réessaie.";
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("FastFlags::ApplyProfile", ex);
                ProfileFeedback = "Impossible d’appliquer et d’enregistrer ce profil. Consulte les logs et réessaie.";
            }
            RefreshControls();
        }

        private void RefreshControls()
        {
            OnPropertyChanged(nameof(UseFastFlagManager));
            OnPropertyChanged(nameof(SelectedMSAALevel));
            OnPropertyChanged(nameof(SelectedTextureQuality));
            OnPropertyChanged(nameof(FixDisplayScaling));
            OnPropertyChanged(nameof(ResetConfiguration));
            OnPropertyChanged(nameof(ProfileFeedback));
        }

        public event EventHandler? OpenFlagEditorEvent;

        private void OpenFastFlagEditor() => OpenFlagEditorEvent?.Invoke(this, EventArgs.Empty);

        public ICommand OpenFastFlagEditorCommand => new RelayCommand(OpenFastFlagEditor);

        public bool UseFastFlagManager
        {
            get => App.Settings.Prop.UseFastFlagManager;
            set => App.Settings.Prop.UseFastFlagManager = value;
        }

        public IReadOnlyDictionary<MSAAMode, string?> MSAALevels => FastFlagManager.MSAAModes;

        public MSAAMode SelectedMSAALevel
        {
            get => MSAALevels.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.MSAA")).Key;
            set => App.FastFlags.SetPreset("Rendering.MSAA", MSAALevels[value]);
        }

        public bool FixDisplayScaling
        {
            get => App.FastFlags.GetPreset("Rendering.DisableScaling") == "True";
            set => App.FastFlags.SetPreset("Rendering.DisableScaling", value ? "True" : null);
        }

        public IReadOnlyDictionary<TextureQuality, string?> TextureQualities => FastFlagManager.TextureQualityLevels;

        public TextureQuality SelectedTextureQuality
        {
            get => TextureQualities.Where(x => x.Value == App.FastFlags.GetPreset("Rendering.TextureQuality.Level")).FirstOrDefault().Key;
            set
            {
                if (value == TextureQuality.Default)
                {
                    App.FastFlags.SetPreset("Rendering.TextureQuality", null);
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.TextureQuality.OverrideEnabled", "True");
                    App.FastFlags.SetPreset("Rendering.TextureQuality.Level", TextureQualities[value]);
                }
            }
        }
        public bool ResetConfiguration
        {
            get => _preResetFlags is not null;

            set
            {
                if (value)
                {
                    _preResetFlags = new(App.FastFlags.Prop);
                    App.FastFlags.Prop.Clear();
                }
                else if (_preResetFlags is not null)
                {
                    App.FastFlags.Prop = _preResetFlags;
                    _preResetFlags = null;
                }

                ProfileFeedback = "";
                RefreshControls();
            }
        }
    }
}
