using System.Runtime.InteropServices;

namespace DesktopTranslator.Helpers;

public static class NativeMethods
{
    // ==================== Hotkey Registration ====================
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const int WM_HOTKEY = 0x0312;

    // Modifier keys
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_NOREPEAT = 0x4000;

    // Virtual keys
    public const uint VK_S = 0x53;
    public const uint VK_T = 0x54;

    // ==================== DPI ====================
    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

    [DllImport("gdi32.dll")]
    public static extern int GetDeviceCaps(IntPtr hDc, int nIndex);

    public const int LOGPIXELSX = 88;
    public const int LOGPIXELSY = 90;

    // ==================== Screen Metrics ====================
    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    public const int SM_XVIRTUALSCREEN = 76;
    public const int SM_YVIRTUALSCREEN = 77;
    public const int SM_CXVIRTUALSCREEN = 78;
    public const int SM_CYVIRTUALSCREEN = 79;

    // ==================== DPI Awareness ====================
    [DllImport("user32.dll")]
    public static extern bool SetProcessDPIAware();

    public static double GetDpiScale()
    {
        var hDc = GetDC(IntPtr.Zero);
        try
        {
            var dpiX = GetDeviceCaps(hDc, LOGPIXELSX);
            return dpiX / 96.0;
        }
        finally
        {
            ReleaseDC(IntPtr.Zero, hDc);
        }
    }
}
