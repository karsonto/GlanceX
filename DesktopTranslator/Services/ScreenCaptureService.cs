using System.Drawing;
using DesktopTranslator.Helpers;

namespace DesktopTranslator.Services;

public class ScreenCaptureService
{
    /// <summary>
    /// Capture a region of the screen in physical pixels.
    /// The region coordinates should already be in physical (device) pixels.
    /// </summary>
    public Bitmap CaptureRegion(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Capture region must have positive dimensions.");

        var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    /// <summary>
    /// Capture the entire virtual screen (all monitors).
    /// </summary>
    public Bitmap CaptureFullScreen()
    {
        int left = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        int top = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

        return CaptureRegion(left, top, width, height);
    }
}
