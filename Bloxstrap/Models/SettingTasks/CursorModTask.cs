using System.Windows;

using Bloxstrap.Models.SettingTasks.Base;

namespace Bloxstrap.Models.SettingTasks
{
    public class CursorModTask : EnumModPresetTask<Enums.CursorType>
    {
        const string NearRelativePath = @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png";
        const string FarRelativePath = @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png";

        string _originalNearPath = "";
        string _originalFarPath = "";
        string _pendingNearPath = "";
        string _pendingFarPath = "";

        public CursorModTask() : base("CursorType", CreateMap())
        {
            OriginalState = App.Settings.Prop.SelectedCursorType;
            _originalNearPath = App.Settings.Prop.CustomCursorNearPath;
            _originalFarPath = App.Settings.Prop.CustomCursorFarPath;
            _pendingNearPath = _originalNearPath;
            _pendingFarPath = _originalFarPath;
        }

        public string PendingNearPath
        {
            get => _pendingNearPath;
            set
            {
                _pendingNearPath = value ?? "";
                UpdatePendingState();
            }
        }

        public string PendingFarPath
        {
            get => _pendingFarPath;
            set
            {
                _pendingFarPath = value ?? "";
                UpdatePendingState();
            }
        }

        public override bool Changed => base.Changed || CustomPathsChanged;

        bool CustomPathsChanged =>
            !String.Equals(_pendingNearPath, _originalNearPath, StringComparison.Ordinal)
            || !String.Equals(_pendingFarPath, _originalFarPath, StringComparison.Ordinal);

        static Dictionary<Enums.CursorType, Dictionary<string, string>> CreateMap() => new()
        {
            {
                Enums.CursorType.From2006, new()
                {
                    { NearRelativePath, "Cursor.From2006.ArrowCursor.png" },
                    { FarRelativePath, "Cursor.From2006.ArrowFarCursor.png" }
                }
            },
            {
                Enums.CursorType.From2013, new()
                {
                    { NearRelativePath, "Cursor.From2013.ArrowCursor.png" },
                    { FarRelativePath, "Cursor.From2013.ArrowFarCursor.png" }
                }
            }
        };

        public override void Execute()
        {
            if (NewState.Equals(Enums.CursorType.Custom))
            {
                if (String.IsNullOrEmpty(_pendingNearPath) || String.IsNullOrEmpty(_pendingFarPath))
                {
                    Frontend.ShowMessageBox(Strings.Menu_Mods_Presets_MouseCursor_Custom_Missing, MessageBoxImage.Error);
                    return;
                }

                CopyCursorFile(_pendingNearPath, NearRelativePath);
                CopyCursorFile(_pendingFarPath, FarRelativePath);

                App.Settings.Prop.CustomCursorNearPath = _pendingNearPath;
                App.Settings.Prop.CustomCursorFarPath = _pendingFarPath;
                App.Settings.Prop.SelectedCursorType = Enums.CursorType.Custom;

                _originalNearPath = _pendingNearPath;
                _originalFarPath = _pendingFarPath;
                OriginalState = NewState;
                return;
            }

            base.Execute();
            App.Settings.Prop.SelectedCursorType = NewState;
            OriginalState = NewState;
        }

        void UpdatePendingState()
        {
            if (Changed)
                App.PendingSettingTasks[Name] = this;
            else
                App.PendingSettingTasks.Remove(Name);
        }

        static void CopyCursorFile(string sourcePath, string relativePath)
        {
            string destinationPath = Path.Combine(Paths.Modifications, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

            Filesystem.AssertReadOnly(destinationPath);
            File.Copy(sourcePath, destinationPath, true);
        }
    }
}
