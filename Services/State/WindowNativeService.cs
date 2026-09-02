using System;
using System.Diagnostics;
using System.Windows;
using CCSwitch.Wpf.Bridge;
using WpfApp = System.Windows.Application;

namespace CCSwitch.Wpf.Services.State;

public class WindowNativeService
{
    private Window? MainWindow => WpfApp.Current?.MainWindow;

    public bool IsMaximized => MainWindow?.WindowState == WindowState.Maximized;

    public event Action? OnWindowStateChanged;

    public void NotifyStateChanged()
    {
        OnWindowStateChanged?.Invoke();
    }

    public void DragMove()
    {
        if (MainWindow != null)
        {
            WindowNativeHelper.StartDragging(MainWindow);
        }
    }

    public void Minimize()
    {
        MainWindow?.Dispatcher.Invoke(() =>
        {
            if (MainWindow != null)
            {
                MainWindow.WindowState = WindowState.Minimized;
                NotifyStateChanged();
            }
        });
    }

    public void ToggleMaximize()
    {
        MainWindow?.Dispatcher.Invoke(() =>
        {
            if (MainWindow != null)
            {
                MainWindow.WindowState = MainWindow.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
                NotifyStateChanged();
            }
        });
    }

    public void Close()
    {
        MainWindow?.Dispatcher.Invoke(() =>
        {
            MainWindow?.Close();
        });
    }

    public void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WindowNativeService] OpenUrl failed: {ex.Message}");
        }
    }
}
