using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CCSwitch.Wpf.Bridge;

public static class WindowNativeHelper
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;

    // DWM_WINDOW_CORNER_PREFERENCE
    private const int DWMWCP_DEFAULT = 0;
    private const int DWMWCP_DONOTROUND = 1;
    private const int DWMWCP_ROUND = 2;
    private const int DWMWCP_ROUNDSMALL = 3;

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    public static void UpdateTheme(Window window, bool isDark)
    {
        var hwnd = new WindowInteropHelper(window).EnsureHandle();
        if (hwnd == IntPtr.Zero) return;

        try
        {
            int useDarkMode = isDark ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
            }

            // Windows 11 Native Rounded Corners
            int cornerPref = DWMWCP_ROUND;
            DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

            // Caption Color: Dark (BGR 0x001B1818), Light (BGR 0x00FFFFFF)
            int captionColor = isDark ? 0x001B1818 : 0x00FFFFFF;
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

            // Text Color: Dark (White), Light (Black)
            int textColor = isDark ? 0x00FFFFFF : 0x00000000;
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));

            // 1px Hardware Native Border Color: Dark subtle zinc (BGR 0x003C3A3A), Light gray (BGR 0x00E5E7EB)
            int borderColor = isDark ? 0x003C3A3A : 0x00E5E7EB;
            DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WindowNativeHelper] Failed to update DWM theme: {ex.Message}");
        }
    }

    public static void StartDragging(Window? window)
    {
        if (window == null) return;

        window.Dispatcher.Invoke(() =>
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    ReleaseCapture();
                    SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WindowNativeHelper] Dragging error: {ex.Message}");
            }
        });
    }
}
