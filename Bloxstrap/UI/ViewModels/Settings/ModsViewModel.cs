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

namespace Bloxstrap.UI.ViewModels.Settings
{
    public class ModsViewModel : NotifyPropertyChangedViewModel
    {
        public ModsViewModel()
        {
            ReloadSkyPacks();

            string selectedId = App.Settings.Prop.SelectedCustomSkyId;
            _selectedSkyPack = SkyPacks.FirstOrDefault(x => x.Id == selectedId) ?? SkyPacks.First();
            UpdateSkyPreviewPaths();
            OnPropertyChanged(nameof(SelectedSkyPack));
            OnPropertyChanged(nameof(SkyPreviewVisibility));
            OnPropertyChanged(nameof(DeleteCustomSkyVisibility));
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
                _selectedSkyPack = value;
                CustomSkyTask.NewState = value?.Id ?? "";
                App.Settings.Prop.SelectedCustomSkyId = CustomSkyTask.NewState;
                UpdateSkyPreviewPaths();
                OnPropertyChanged(nameof(SelectedSkyPack));
                OnPropertyChanged(nameof(SkyPreviewVisibility));
                OnPropertyChanged(nameof(DeleteCustomSkyVisibility));
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
            PreviewSkyBack = SelectedSkyPack?.GetFaceImagePath("bk");
            PreviewSkyDown = SelectedSkyPack?.GetFaceImagePath("dn");
            PreviewSkyFront = SelectedSkyPack?.GetFaceImagePath("ft");
            PreviewSkyLeft = SelectedSkyPack?.GetFaceImagePath("lf");
            PreviewSkyRight = SelectedSkyPack?.GetFaceImagePath("rt");
            PreviewSkyUp = SelectedSkyPack?.GetFaceImagePath("up");

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

        public EnumModPresetTask<Enums.CursorType> CursorTypeTask { get; } = new("CursorType", new()
        {
            {
                Enums.CursorType.From2006, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.From2006.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.From2006.ArrowFarCursor.png" }
                }
            },
            {
                Enums.CursorType.From2013, new()
                {
                    { @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png",    "Cursor.From2013.ArrowCursor.png"    },
                    { @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png", "Cursor.From2013.ArrowFarCursor.png" }
                }
            }
        });

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
