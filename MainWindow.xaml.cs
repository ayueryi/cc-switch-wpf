using System;
using System.IO;
using System.Windows;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using CCSwitch.Wpf.Bridge;
using CCSwitch.Wpf.Services;
using CCSwitch.Wpf.Services.State;

namespace CCSwitch.Wpf;

public partial class MainWindow : Window
{
    private TrayService? _trayService;
    private WindowNativeService? _windowNativeService;
    private ThemeStateService? _themeStateService;

    public MainWindow()
    {
        InitializeComponent();

        BlazorWebViewControl.BlazorWebViewInitializing += (s, e) =>
        {
            var profilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CCSwitch",
                "WebView2Profile"
            );
            Directory.CreateDirectory(profilePath);
            e.UserDataFolder = profilePath;
            LoggerService.Instance.Info("WebView2", $"BlazorWebView UserDataFolder configured: {profilePath}");
        };

        BlazorWebViewControl.BlazorWebViewInitialized += (s, e) =>
        {
            LoggerService.Instance.Info("WebView2", "BlazorWebView initialized successfully!");
            if (e.WebView.CoreWebView2 != null)
            {
                e.WebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                e.WebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            }
        };

        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
        StateChanged += MainWindow_StateChanged;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _themeStateService = App.Services.GetService(typeof(ThemeStateService)) as ThemeStateService;
        bool isDark = _themeStateService?.IsDark ?? true;
        WindowNativeHelper.UpdateTheme(this, isDark);
        Background = isDark
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x18, 0x18, 0x1B))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        _windowNativeService?.NotifyStateChanged();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _windowNativeService = App.Services.GetService(typeof(WindowNativeService)) as WindowNativeService;

            if (_themeStateService == null)
            {
                _themeStateService = App.Services.GetService(typeof(ThemeStateService)) as ThemeStateService;
            }

            if (_themeStateService != null)
            {
                _themeStateService.OnThemeChanged += HandleThemeChanged;
                HandleThemeChanged();
            }

            var providerService = App.Services.GetService(typeof(ProviderService)) as ProviderService;
            if (providerService != null)
            {
                _trayService = new TrayService(this, providerService);
            }
        }
        catch (Exception ex)
        {
            LoggerService.Instance.Error("MainWindow", $"Failed to initialize TrayService or Theme listener: {ex.Message}", ex);
        }
    }

    private void HandleThemeChanged()
    {
        Dispatcher.Invoke(() =>
        {
            if (_themeStateService == null) return;

            bool isDark = _themeStateService.IsDark;
            WindowNativeHelper.UpdateTheme(this, isDark);

            Background = isDark
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x18, 0x18, 0x1B))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
        });
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_themeStateService != null)
        {
            _themeStateService.OnThemeChanged -= HandleThemeChanged;
        }
        _trayService?.Dispose();
    }
}
