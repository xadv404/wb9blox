using System.Windows;
using System.Windows.Interop;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Mvvm.Contracts;
using Wpf.Ui.Mvvm.Services;

namespace Bloxstrap.UI.Elements.Base
{
    public abstract class WpfUiWindow : UiWindow
    {
        private readonly IThemeService _themeService = new ThemeService();

        public WpfUiWindow()
        {
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            const int customThemeIndex = 2; // index for CustomTheme merged dictionary

            _themeService.SetTheme(App.Settings.Prop.Theme.GetFinal() == Enums.Theme.Dark ? ThemeType.Dark : ThemeType.Light);
            _themeService.SetSystemAccent();

            // there doesn't seem to be a way to query the name for merged dictionaries
            var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/UI/Style/{Enum.GetName(App.Settings.Prop.Theme.GetFinal())}.xaml") };
            Application.Current.Resources.MergedDictionaries[customThemeIndex] = dict;

#if QA_BUILD
            this.BorderBrush = System.Windows.Media.Brushes.Red;
            this.BorderThickness = new Thickness(4);
#endif
        }

        /// <summary>
        /// Semi-transparent menu background (~75% opacity) with acrylic blur.
        /// </summary>
        protected void ApplyMenuTransparency(double opacity = 0.75)
        {
            const double minOpacity = 0.70;
            const double maxOpacity = 0.80;
            opacity = Math.Clamp(opacity, minOpacity, maxOpacity);

            WindowBackdropType = Wpf.Ui.Appearance.BackgroundType.Acrylic;

            void UpdateBackground()
            {
                bool isDark = App.Settings.Prop.Theme.GetFinal() == Enums.Theme.Dark;
                var baseColor = isDark
                    ? System.Windows.Media.Color.FromRgb(32, 32, 32)
                    : System.Windows.Media.Color.FromRgb(243, 243, 243);

                byte alpha = (byte)(255 * opacity);
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));
            }

            UpdateBackground();
            Loaded += (_, _) => UpdateBackground();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            if (App.Settings.Prop.WPFSoftwareRender || App.LaunchSettings.NoGPUFlag.Active)
            {
                if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                    hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;
            }

            base.OnSourceInitialized(e);
        }
    }
}
