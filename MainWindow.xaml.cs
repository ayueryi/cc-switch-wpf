using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using CCSwitch.Wpf.Bridge;
using CCSwitch.Wpf.Services;

namespace CCSwitch.Wpf;

public partial class MainWindow : Window
{
    private IpcDispatcher? _dispatcher;
    private readonly DatabaseService _dbService;
    private readonly SettingsService _settingsService;
    private readonly LiveConfigService _liveConfigService;
    private readonly ProviderService _providerService;
    private readonly McpService _mcpService;
    private readonly PromptService _promptService;
    private readonly SkillService _skillService;
    private readonly AuthService _authService;
    private readonly SessionService _sessionService;
    private readonly UsageService _usageService;
    private readonly SystemIntegrationService _sysService;
    private readonly ProfileService _profileService;
    private readonly StreamCheckService _streamService;
    private readonly CloudSyncService _cloudSyncService;
    private readonly SkillMarketService _skillMarketService;
    private readonly LiveImportService _liveImportService;
    private TrayService? _trayService;
    private LocalStaticServer? _staticServer;
    private readonly LoggerService _logger = LoggerService.Instance;

    public MainWindow()
    {
        InitializeComponent();

        _logger.Info("MainWindow", "CCSwitch MainWindow initializing...");

        _dbService = new DatabaseService();
        _dbService.Initialize();
        _settingsService = new SettingsService(_dbService);
        _liveConfigService = new LiveConfigService();
        _providerService = new ProviderService(_dbService, _settingsService, _liveConfigService);
        _mcpService = new McpService(_dbService);
        _promptService = new PromptService(_dbService);
        _skillService = new SkillService(_dbService);
        _authService = new AuthService();
        _sessionService = new SessionService();
        _usageService = new UsageService(_dbService);
        _sysService = new SystemIntegrationService();
        _profileService = new ProfileService(_dbService);
        _streamService = new StreamCheckService(_dbService, _providerService);
        _cloudSyncService = new CloudSyncService(_dbService, _providerService, _mcpService);
        _skillMarketService = new SkillMarketService(_dbService);
        _liveImportService = new LiveImportService(_providerService, _mcpService);

        try
        {
            _trayService = new TrayService(this, _providerService);
        }
        catch (Exception tEx)
        {
            Log($"[TrayService] Init failed: {tEx.Message}");
        }

        SourceInitialized += MainWindow_SourceInitialized;
        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        WindowNativeHelper.EnableImmersiveDarkMode(this, 0x001B1818);
    }

    private void Log(string message)
    {
        _logger.Info("WebView2", message);
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        WindowNativeHelper.EnableImmersiveDarkMode(this, 0x001B1818);
        await InitializeWebViewAsync();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _trayService?.Dispose();
        _staticServer?.Dispose();
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            bool initialized = false;
            try
            {
                var userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CCSwitch",
                    "WebView2Profile"
                );
                Directory.CreateDirectory(userDataFolder);
                Log($"Attempting primary UserDataFolder: {userDataFolder}");

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await WebViewControl.EnsureCoreWebView2Async(env);
                initialized = true;
            }
            catch (Exception ex)
            {
                Log($"Primary WebView2 init failed ({ex.Message}), trying process-isolated temp profile...");
            }

            if (!initialized)
            {
                try
                {
                    var fallbackFolder = Path.Combine(
                        Path.GetTempPath(),
                        $"CCSwitch_WV2_{Environment.ProcessId}"
                    );
                    Directory.CreateDirectory(fallbackFolder);
                    Log($"Attempting fallback UserDataFolder: {fallbackFolder}");

                    var fallbackEnv = await CoreWebView2Environment.CreateAsync(null, fallbackFolder);
                    await WebViewControl.EnsureCoreWebView2Async(fallbackEnv);
                    initialized = true;
                }
                catch (Exception ex2)
                {
                    Log($"Fallback WebView2 init failed ({ex2.Message}), trying system default...");
                    await WebViewControl.EnsureCoreWebView2Async();
                }
            }

            var core = WebViewControl.CoreWebView2;
            Log($"WebView2 initialized, Edge version: {core.Environment.BrowserVersionString}");

            core.Settings.AreDefaultContextMenusEnabled = true;
            core.Settings.AreDevToolsEnabled = true;
            core.Settings.IsStatusBarEnabled = false;

            _dispatcher = new IpcDispatcher(core)
            {
                Logger = Log
            };
            RegisterAllIpcHandlers(_dispatcher);

            core.WebMessageReceived += async (s, e) =>
            {
                try
                {
                    string rawMsg;
                    try
                    {
                        rawMsg = e.TryGetWebMessageAsString();
                    }
                    catch
                    {
                        rawMsg = e.WebMessageAsJson;
                    }

                    if (string.IsNullOrEmpty(rawMsg))
                    {
                        rawMsg = e.WebMessageAsJson;
                    }

                    await _dispatcher.HandleMessageAsync(rawMsg);
                }
                catch (Exception wEx)
                {
                    Log($"[WebMessageReceived Error] {wEx}");
                }
            };

            core.NavigationCompleted += (s, e) =>
            {
                Log($"Navigation completed: isSuccess={e.IsSuccess}, httpStatus={e.HttpStatusCode}, error={e.WebErrorStatus}");
            };

            var shimScript = GetShimScript();
            await core.AddScriptToExecuteOnDocumentCreatedAsync(shimScript);

            var candidatePaths = new[]
            {
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\..\..\..\tooling\cc-switch-diy\dist")),
                @"C:\Users\yu\codes\tooling\cc-switch-diy\dist",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dist")
            };

            string? distPath = null;
            foreach (var p in candidatePaths)
            {
                if (Directory.Exists(p) && File.Exists(Path.Combine(p, "index.html")))
                {
                    distPath = p;
                    break;
                }
            }

            if (distPath != null)
            {
                Log($"Found dist directory at: {distPath}");
                try
                {
                    core.SetVirtualHostNameToFolderMapping(
                        "app.ccswitch.local",
                        distPath,
                        CoreWebView2HostResourceAccessKind.Allow
                    );
                    Log("Virtual host mapped: https://app.ccswitch.local/");
                    core.Navigate("https://app.ccswitch.local/index.html");
                }
                catch (Exception vEx)
                {
                    Log($"SetVirtualHostNameToFolderMapping failed ({vEx.Message}), falling back to LocalStaticServer...");
                    try
                    {
                        _staticServer = new LocalStaticServer(distPath, 18765);
                        _staticServer.Start();
                        Log($"Local static server listening on: {_staticServer.Url}");
                        core.Navigate(_staticServer.Url);
                    }
                    catch (Exception sEx)
                    {
                        Log($"LocalStaticServer failed ({sEx.Message}), falling back to file://");
                        core.Navigate($"file:///{Path.Combine(distPath, "index.html").Replace('\\', '/')}");
                    }
                }
            }
            else
            {
                Log("Frontend dist folder not found in any candidate path");
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to initialize WebView2: {ex}");
            System.Windows.MessageBox.Show($"Failed to initialize WebView2:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string GetShimScript()
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceName = "CCSwitch.Wpf.Bridge.TauriShim.js";

        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Bridge\TauriShim.js"),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Bridge\TauriShim.js")),
            @"C:\Users\yu\codes\tooling\cc-switch-wpf\Bridge\TauriShim.js"
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }

        return "// TauriShim not found";
    }
}