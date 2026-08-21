using Bloxstrap.Models.SettingTasks.Base;

namespace Bloxstrap.Models.SettingTasks
{
    public class CursorModTask : EnumModPresetTask<Enums.CursorType>
    {
        const string NearRelativePath = @"content\textures\Cursors\KeyboardMouse\ArrowCursor.png";
        const string FarRelativePath = @"content\textures\Cursors\KeyboardMouse\ArrowFarCursor.png";

        public CursorModTask() : base("CursorType", CreateMap())
        {
            OriginalState = App.Settings.Prop.SelectedCursorType;
        }

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
                ApplyCustomCursor();
                App.Settings.Prop.SelectedCursorType = Enums.CursorType.Custom;
                OriginalState = NewState;
                return;
            }

            base.Execute();
            App.Settings.Prop.SelectedCursorType = NewState;
        }

        static void ApplyCustomCursor()
        {
            string nearPath = App.Settings.Prop.CustomCursorNearPath;
            string farPath = App.Settings.Prop.CustomCursorFarPath;

            if (String.IsNullOrEmpty(nearPath) || String.IsNullOrEmpty(farPath))
            {
                Frontend.ShowMessageBox(Strings.Menu_Mods_Presets_MouseCursor_Custom_Missing, MessageBoxImage.Error);
                return;
            }

            CopyCursorFile(nearPath, NearRelativePath);
            CopyCursorFile(farPath, FarRelativePath);
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
