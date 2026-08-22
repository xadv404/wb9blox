using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Bloxstrap.Extensions
{
    public static class IconEx
    {
        public static Icon GetSized(this Icon icon, int width, int height) => new(icon, new Size(width, height));

        public static ImageSource GetImageSource(this Icon icon, bool handleException = true)
            => GetImageSource(icon, 0, handleException);

        public static ImageSource GetImageSource(this Icon icon, int pixelSize, bool handleException = true)
        {
            if (icon is null)
                throw new ArgumentNullException(nameof(icon));

            Icon? sizedIcon = null;

            try
            {
                Icon iconToSave = icon;

                if (pixelSize > 0)
                {
                    sizedIcon = new Icon(icon, pixelSize, pixelSize);
                    iconToSave = sizedIcon;
                }

                using MemoryStream stream = new();
                iconToSave.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
            catch (Exception ex) when (handleException)
            {
                App.Logger.WriteException("IconEx::GetImageSource", ex);
                Frontend.ShowMessageBox(string.Format(Strings.Dialog_IconLoadFailed, ex.Message));
                return BootstrapperIcon.IconCatstrap.GetIcon().GetImageSource(pixelSize, false);
            }
            finally
            {
                sizedIcon?.Dispose();
            }
        }
    }
}
