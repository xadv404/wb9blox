using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

using Microsoft.Win32;

using Windows.Win32;
using Windows.Win32.UI.Shell;
using Windows.Win32.Foundation;

using CommunityToolkit.Mvvm.Input;

using Bloxstrap.Models.Entities;
using Bloxstrap.Models.SettingTasks;
using Bloxstrap.AppData;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class ModsViewModel : NotifyPropertyChangedViewModel
    {
        public ModsViewModel()
        {
            ReloadSkyPacks();

            string selectedId = App.Settings.Prop.SelectedCustomSkyId;
            _selectedSkyPack = SkyPacks.FirstOrDefault(x => x.Id == selectedId) ?? SkyPacks.First();

            if (_selectedSkyPack is not null && !String.IsNullOrEmpty(_selectedSkyPack.Id))
                ApplySelectedSky(_selectedSkyPack);
            else
                UpdateSkyPreviewPaths();
            OnPropertyChanged(nameof(SelectedSkyPack));
            OnPropertyChanged(nameof(SkyPreviewVisibility));
            OnPropertyChanged(nameof(DeleteCustomSkyVisibility));

            OnPropertyChanged(nameof(SelectedCursorType));
            OnPropertyChanged(nameof(CustomCursorVisibility));
            OnPropertyChanged(nameof(ChooseCustomCursorNearVisibility));
            OnPropertyChanged(nameof(DeleteCustomCursorNearVisibility));
            OnPropertyChanged(nameof(ChooseCustomCursorFarVisibility));
            OnPropertyChanged(nameof(DeleteCustomCursorFarVisibility));
            OnPropertyChanged(nameof(PreviewCustomCursorNear));
            OnPropertyChanged(nameof(PreviewCustomCursorFar));
        }

        private void OpenModsFolder() => Process.Start("explorer.exe", Paths.Modifications);

        private readonly Dictionary<string, byte[]> FontHeaders = new()
        {
            { "ttf", new byte[4] { 0x00, 0x01, 0x00, 0x00 } },
            { "otf", new byte[4] { 0x4F, 0x54, 0x54, 0x4F } },
            { "ttc", new byte[4] { 0x74, 0x74, 0x63, 0x66 } } 
        };

        private void ManageCustomFont()
        {
            if (!String.IsNullOrEmpty(TextFontTask.NewState))
            {
                TextFontTask.NewState = "";
            }
            else
            {
                var dialog = new OpenFileDialog
                {
                    Filter = $"{Strings.Menu_FontFiles}|*.ttf;*.otf;*.ttc"
                };

                if (dialog.ShowDialog() != true)
                    return;

                string type = dialog.FileName.Substring(dialog.FileName.Length-3, 3).ToLowerInvariant();

                if (!FontHeaders.ContainsKey(type) 
                    || !FontHeaders.Any(x => File.ReadAllBytes(dialog.FileName).Take(4).SequenceEqual(x.Value)))
                {
                    Frontend.ShowMessageBox(Strings.Menu_Mods_Misc_CustomFont_Invalid, MessageBoxImage.Error);
                    return;
                }

                TextFontTask.NewState = dialog.FileName;
            }

            OnPropertyChanged(nameof(ChooseCustomFontVisibility));
            OnPropertyChanged(nameof(DeleteCustomFontVisibility));
        }

        private CustomSkyPack? _selectedSkyPack;

        public ObservableCollection<CustomSkyPack> SkyPacks { get; } = new();

        public CustomSkyPack? SelectedSkyPack
        {
            get => _selectedSkyPack;
            set
            {
                if (_selectedSkyPack?.Id == value?.Id)
                    return;

                _selectedSkyPack = value;
                CustomSkyTask.NewState = value?.Id ?? "";
                ApplySelectedSky(value);
                OnPropertyChanged(nameof(SelectedSkyPack));
                OnPropertyChanged(nameof(SkyPreviewVisibility));
                OnPropertyChanged(nameof(DeleteCustomSkyVisibility));
            }
        }

        void ApplySelectedSky(CustomSkyPack? pack)
        {
            try
            {
                if (pack is null || String.IsNullOrEmpty(pack.Id))
                {
                    RobloxSkybox.RemoveApplied();
                    App.Settings.Prop.SelectedCustomSkyId = "";
                }
                else
                {
                    RobloxSkybox.EnsurePreviewCache(pack);
                    RobloxSkybox.ApplyPack(pack);
                    App.Settings.Prop.SelectedCustomSkyId = pack.Id;
                }

                CustomSkyTask.NewState = App.Settings.Prop.SelectedCustomSkyId;
                CustomSkyTask.OriginalState = CustomSkyTask.NewState;
                App.Settings.Save();
                UpdateSkyPreviewPaths();
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("ModsViewModel::ApplySelectedSky", ex);
                Frontend.ShowMessageBox(ex.Message, MessageBoxImage.Error);
            }
        }

        public Visibility SkyPreviewVisibility => SelectedSkyPack is not null && !String.IsNullOrEmpty(SelectedSkyPack.Id) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility DeleteCustomSkyVisibility => SelectedSkyPack is not null && !String.IsNullOrEmpty(SelectedSkyPack.Id) ? Visibility.Visible : Visibility.Collapsed;

        public string? PreviewSkyBack { get; private set; }
        public string? PreviewSkyDown { get; private set; }
        public string? PreviewSkyFront { get; private set; }
        public string? PreviewSkyLeft { get; private set; }
        public string? PreviewSkyRight { get; private set; }
        public string? PreviewSkyUp { get; private set; }

        private void ReloadSkyPacks()
        {
            SkyPacks.Clear();
            SkyPacks.Add(new CustomSkyPack { Id = "", Name = Strings.Menu_Mods_Misc_CustomSky_None });

            foreach (var pack in RobloxSkybox.ListInstalledPacks())
                SkyPacks.Add(pack);
        }

        private void UpdateSkyPreviewPaths()
        {
            if (SelectedSkyPack is null || String.IsNullOrEmpty(SelectedSkyPack.Id))
            {
                PreviewSkyBack = null;
                PreviewSkyDown = null;
                PreviewSkyFront = null;
                PreviewSkyLeft = null;
                PreviewSkyRight = null;
                PreviewSkyUp = null;
            }
            else
            {
                PreviewSkyBack = RobloxSkybox.GetFacePreviewPath(SelectedSkyPack, "bk");
                PreviewSkyDown = RobloxSkybox.GetFacePreviewPath(SelectedSkyPack, "dn");
                PreviewSkyFront = RobloxSkybox.GetFacePreviewPath(SelectedSkyPack, "ft");
                PreviewSkyLeft = RobloxSkybox.GetFacePreviewPath(SelectedSkyPack, "lf");
                PreviewSkyRight = RobloxSkybox.GetFacePreviewPath(SelectedSkyPack, "rt");
                PreviewSkyUp = RobloxSkybox.GetFacePreviewPath(SelectedSkyPack, "up");
            }

            OnPropertyChanged(nameof(PreviewSkyBack));
            OnPropertyChanged(nameof(PreviewSkyDown));
            OnPropertyChanged(nameof(PreviewSkyFront));
            OnPropertyChanged(nameof(PreviewSkyLeft));
            OnPropertyChanged(nameof(PreviewSkyRight));
            OnPropertyChanged(nameof(PreviewSkyUp));
        }

        private void ImportCustomSky()
        {
            using var folderDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = Strings.Menu_Mods_Misc_CustomSky_ImportPrompt,
                UseDescriptionForTitle = true
            };

            if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            string name = Path.GetFileName(folderDialog.SelectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            if (String.IsNullOrWhiteSpace(name))
                name = "Custom Sky";

            if (!RobloxSkybox.TryGetImportIssues(folderDialog.SelectedPath, out string? blockingError, out string? confirmMessage))
            {
                Frontend.ShowMessageBox(blockingError ?? Strings.Menu_Mods_Misc_CustomSky_Invalid, MessageBoxImage.Error);
                return;
            }

            if (!String.IsNullOrEmpty(confirmMessage))
            {
                var confirmResult = Frontend.ShowMessageBox(
                    confirmMessage,
                    MessageBoxImage.Warning,
                    MessageBoxButton.YesNo,
                    MessageBoxResult.No);

                if (confirmResult != MessageBoxResult.Yes)
                    return;
            }

            if (!RobloxSkybox.TryImportSkyPack(folderDialog.SelectedPath, name, out CustomSkyPack? pack, out string? errorMessage))
            {
                Frontend.ShowMessageBox(errorMessage ?? Strings.Menu_Mods_Misc_CustomSky_Invalid, MessageBoxImage.Error);
                return;
            }

            ReloadSkyPacks();
            SelectedSkyPack = SkyPacks.FirstOrDefault(x => x.Id == pack!.Id);
        }

        private void RemoveSelectedCustomSky()
        {
            if (SelectedSkyPack is null || String.IsNullOrEmpty(SelectedSkyPack.Id))
                return;

            var result = Frontend.ShowMessageBox(
                String.Format(Strings.Menu_Mods_Misc_CustomSky_RemoveConfirm, SelectedSkyPack.Name),
                MessageBoxImage.Warning,
                MessageBoxButton.YesNo,
                MessageBoxResult.No
            );

            if (result != MessageBoxResult.Yes)
                return;

            string packId = SelectedSkyPack.Id;
            RobloxSkybox.DeletePack(packId);

            if (CustomSkyTask.OriginalState == packId)
                CustomSkyTask.NewState = "";

            ReloadSkyPacks();
            SelectedSkyPack = SkyPacks.First();
        }

        public ICommand OpenModsFolderCommand => new RelayCommand(OpenModsFolder);

        public Visibility ChooseCustomFontVisibility => !String.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility DeleteCustomFontVisibility => !String.IsNullOrEmpty(TextFontTask.NewState) ? Visibility.Visible : Visibility.Collapsed;

        public ICommand ManageCustomFontCommand => new RelayCommand(ManageCustomFont);

        public ICommand ImportCustomSkyCommand => new RelayCommand(ImportCustomSky);

        public ICommand RemoveSelectedCustomSkyCommand => new RelayCommand(RemoveSelectedCustomSky);

        public ICommand OpenCompatSettingsCommand => new RelayCommand(OpenCompatSettings);

        public ModPresetTask OldAvatarBackgroundTask { get; } = new("OldAvatarBackground", @"ExtraContent\places\Mobile.rbxl", "OldAvatarBackground.rbxl");

        public ModPresetTask OldCharacterSoundsTask { get; } = new("OldCharacterSounds", new()
        {
            { @"content\sounds\action_footsteps_plastic.mp3", "Sounds.OldWalk.mp3"  },
            { @"content\sounds\action_jump.mp3",              "Sounds.OldJump.mp3"  },
            { @"content\sounds\action_get_up.mp3",            "Sounds.OldGetUp.mp3" },
            { @"content\sounds\action_falling.mp3",           "Sounds.Empty.mp3"    },
            { @"content\sounds\action_jump_land.mp3",         "Sounds.Empty.mp3"    },
            { @"content\sounds\action_swim.mp3",              "Sounds.Empty.mp3"    },
            { @"content\sounds\impact_water.mp3",             "Sounds.Empty.mp3"    }
        });

        public EmojiModPresetTask EmojiFontTask { get; } = new();

        public CursorModTask CursorTypeTask { get; } = new();

        public Enums.CursorType SelectedCursorType
        {
            get => CursorTypeTask.NewState;
            set
            {
                CursorTypeTask.NewState = value;
                OnPropertyChanged(nameof(SelectedCursorType));
                OnPropertyChanged(nameof(CustomCursorVisibility));
            }
        }

        public Visibility CustomCursorVisibility =>
            CursorTypeTask.NewState.Equals(Enums.CursorType.Custom) ? Visibility.Visible : Visibility.Collapsed;

        public string? PreviewCustomCursorNear =>
            File.Exists(App.Settings.Prop.CustomCursorNearPath) ? App.Settings.Prop.CustomCursorNearPath : null;

        public string? PreviewCustomCursorFar =>
            File.Exists(App.Settings.Prop.CustomCursorFarPath) ? App.Settings.Prop.CustomCursorFarPath : null;

        public Visibility ChooseCustomCursorNearVisibility =>
            String.IsNullOrEmpty(App.Settings.Prop.CustomCursorNearPath) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility DeleteCustomCursorNearVisibility =>
            String.IsNullOrEmpty(App.Settings.Prop.CustomCursorNearPath) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility ChooseCustomCursorFarVisibility =>
            String.IsNullOrEmpty(App.Settings.Prop.CustomCursorFarPath) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility DeleteCustomCursorFarVisibility =>
            String.IsNullOrEmpty(App.Settings.Prop.CustomCursorFarPath) ? Visibility.Collapsed : Visibility.Visible;

        private bool ValidateCursorImage(string path)
        {
            if (!File.Exists(path))
                return false;

            byte[] header = File.ReadAllBytes(path).Take(8).ToArray();
            byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

            if (!header.SequenceEqual(pngHeader))
            {
                Frontend.ShowMessageBox(Strings.Menu_Mods_Presets_MouseCursor_Custom_Invalid, MessageBoxImage.Error);
                return false;
            }

            using var image = Image.Load<Rgba32>(path);

            if (image.Width > 64 || image.Height > 64)
                Frontend.ShowMessageBox(Strings.Menu_Mods_Presets_MouseCursor_Custom_SizeWarning, MessageBoxImage.Warning);

            return true;
        }

        private void ManageCustomCursorNear()
        {
            if (!String.IsNullOrEmpty(App.Settings.Prop.CustomCursorNearPath))
            {
                App.Settings.Prop.CustomCursorNearPath = "";
            }
            else
            {
                var dialog = new OpenFileDialog
                {
                    Filter = $"{Strings.Menu_ImageFiles}|*.png"
                };

                if (dialog.ShowDialog() != true || !ValidateCursorImage(dialog.FileName))
                    return;

                App.Settings.Prop.CustomCursorNearPath = dialog.FileName;
            }

            RefreshCustomCursorState();
        }

        private void ManageCustomCursorFar()
        {
            if (!String.IsNullOrEmpty(App.Settings.Prop.CustomCursorFarPath))
            {
                App.Settings.Prop.CustomCursorFarPath = "";
            }
            else
            {
                var dialog = new OpenFileDialog
                {
                    Filter = $"{Strings.Menu_ImageFiles}|*.png"
                };

                if (dialog.ShowDialog() != true || !ValidateCursorImage(dialog.FileName))
                    return;

                App.Settings.Prop.CustomCursorFarPath = dialog.FileName;
            }

            RefreshCustomCursorState();
        }

        private void RefreshCustomCursorState()
        {
            if (!String.IsNullOrEmpty(App.Settings.Prop.CustomCursorNearPath)
                && !String.IsNullOrEmpty(App.Settings.Prop.CustomCursorFarPath))
            {
                SelectedCursorType = Enums.CursorType.Custom;
            }
            else
            {
                OnPropertyChanged(nameof(SelectedCursorType));
                OnPropertyChanged(nameof(CustomCursorVisibility));
            }

            OnPropertyChanged(nameof(ChooseCustomCursorNearVisibility));
            OnPropertyChanged(nameof(DeleteCustomCursorNearVisibility));
            OnPropertyChanged(nameof(ChooseCustomCursorFarVisibility));
            OnPropertyChanged(nameof(DeleteCustomCursorFarVisibility));
            OnPropertyChanged(nameof(PreviewCustomCursorNear));
            OnPropertyChanged(nameof(PreviewCustomCursorFar));
            OnPropertyChanged(nameof(CustomCursorVisibility));
        }

        public ICommand ManageCustomCursorNearCommand => new RelayCommand(ManageCustomCursorNear);

        public ICommand ManageCustomCursorFarCommand => new RelayCommand(ManageCustomCursorFar);

        public FontModPresetTask TextFontTask { get; } = new();

        public CustomSkyModTask CustomSkyTask { get; } = new();

        private void OpenCompatSettings()
        {
            string path = new RobloxPlayerData().ExecutablePath;

            if (File.Exists(path))
                PInvoke.SHObjectProperties(HWND.Null, SHOP_TYPE.SHOP_FILEPATH, path, "Compatibility");
            else
                Frontend.ShowMessageBox(Strings.Common_RobloxNotInstalled, MessageBoxImage.Error);

        }
    }
}
