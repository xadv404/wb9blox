using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Bloxstrap.UI.Converters
{
    public class PathToImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string path || String.IsNullOrEmpty(path) || !File.Exists(path))
                return null;

            var image = new BitmapImage();

            image.BeginInit();
            image.UriSource = new Uri(path, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
