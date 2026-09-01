using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using WpfApplication = System.Windows.Application;

namespace CCSwitch.Wpf.Services;

public class TrayService : IDisposable
{
    private readonly Window _mainWindow;
    private readonly ProviderService _providerService;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;

    public TrayService(Window mainWindow, ProviderService providerService)
    {
        _mainWindow = mainWindow;
        _providerService = providerService;
        InitializeTray();
    }

    private void InitializeTray()
    {
        _contextMenu = new ContextMenuStrip();

        Icon? icon = null;
        var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
        if (File.Exists(iconPath))
        {
            icon = new Icon(iconPath);
        }
        else
        {
            var altPath = @"C:\Users\yu\codes\tooling\cc-switch-wpf\icon.ico";
            if (File.Exists(altPath))
            {
                icon = new Icon(altPath);
            }
            else
            {
                icon = SystemIcons.Application;
            }
        }

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Text = "CC Switch",
            Visible = true,
            ContextMenuStrip = _contextMenu
        };

        _notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

        _ = RefreshMenuAsync();
        LoggerService.Instance.Info("Tray", "System tray icon initialized successfully");
    }

    public void ShowMainWindow()
    {
        _mainWindow.Dispatcher.Invoke(() =>
        {
            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
            _mainWindow.Activate();
            LoggerService.Instance.Info("Tray", "Restored and activated main window");
        });
    }

    public void HideMainWindow()
    {
        _mainWindow.Dispatcher.Invoke(() =>
        {
            _mainWindow.Hide();
            LoggerService.Instance.Info("Tray", "Minimized main window to system tray");
        });
    }

    public async Task RefreshMenuAsync()
    {
        if (_contextMenu == null) return;

        try
        {
            var apps = new[] { "claude", "codex", "grokbuild", "opencode", "antigravity" };
            var menuItems = new List<ToolStripItem>();

            // 1. App Header
            var header = new ToolStripMenuItem("CC Switch (v3.20.4)") { Enabled = false };
            menuItems.Add(header);
            menuItems.Add(new ToolStripSeparator());

            // 2. Providers for each app
            foreach (var app in apps)
            {
                var providersDict = await _providerService.GetProvidersAsync(app);
                var providers = providersDict.Values.OfType<ProviderDto>().ToList();
                var currentId = await _providerService.GetCurrentProviderIdAsync(app);

                var appName = app switch
                {
                    "claude" => "Claude",
                    "codex" => "Codex",
                    "grokbuild" => "Grok Build",
                    "opencode" => "OpenCode",
                    "antigravity" => "Antigravity",
                    _ => char.ToUpper(app[0]) + app[1..]
                };

                var appMenu = new ToolStripMenuItem($"{appName} 供应商");

                if (providers.Count == 0)
                {
                    appMenu.DropDownItems.Add(new ToolStripMenuItem("暂无可用供应商") { Enabled = false });
                }
                else
                {
                    foreach (var p in providers)
                    {
                        var isCurrent = p.Id == currentId;
                        var pItem = new ToolStripMenuItem(p.Name, null, async (s, e) =>
                        {
                            LoggerService.Instance.Info("Tray", $"User switched {app} provider via tray context menu: {p.Name} ({p.Id})");
                            await _providerService.SetCurrentProviderAsync(app, p.Id);
                            await RefreshMenuAsync();
                        })
                        {
                            Checked = isCurrent
                        };
                        appMenu.DropDownItems.Add(pItem);
                    }
                }
                menuItems.Add(appMenu);
            }

            menuItems.Add(new ToolStripSeparator());

            // 3. Show / Hide window
            var showItem = new ToolStripMenuItem("显示主窗口", null, (s, e) => ShowMainWindow());
            menuItems.Add(showItem);

            // 4. Exit
            var exitItem = new ToolStripMenuItem("退出 CC Switch", null, (s, e) =>
            {
                _notifyIcon!.Visible = false;
                LoggerService.Instance.Info("Tray", "User requested application shutdown via tray context menu");
                WpfApplication.Current.Dispatcher.Invoke(() => WpfApplication.Current.Shutdown());
            });
            menuItems.Add(exitItem);

            _contextMenu.Invoke(() =>
            {
                _contextMenu.Items.Clear();
                _contextMenu.Items.AddRange(menuItems.ToArray());
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TrayService] RefreshMenuAsync error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
        _contextMenu?.Dispose();
    }
}
