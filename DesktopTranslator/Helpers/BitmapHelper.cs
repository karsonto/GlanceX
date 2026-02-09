using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace DesktopTranslator.Helpers;

public static class BitmapHelper
{
    /// <summary>
    /// Convert System.Drawing.Bitmap to Windows.Graphics.Imaging.SoftwareBitmap for OCR.
    /// </summary>
    public static async Task<SoftwareBitmap> ConvertToSoftwareBitmapAsync(Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Bmp);
        memoryStream.Position = 0;

        using var randomAccessStream = new InMemoryRandomAccessStream();
        using (var outputStream = randomAccessStream.GetOutputStreamAt(0))
        {
            using var writer = new DataWriter(outputStream);
            writer.WriteBytes(memoryStream.ToArray());
            await writer.StoreAsync();
            await writer.FlushAsync();
        }

        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied);

        return softwareBitmap;
    }
}
