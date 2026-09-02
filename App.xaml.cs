using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using CCSwitch.Wpf.Services;
using CCSwitch.Wpf.Services.State;

namespace CCSwitch.Wpf;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        LoggerService.Instance.Info("App", $"=== Prism Starting (Blazor Hybrid, PID: {Environment.ProcessId}) ===");

        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();

        // 1. Initialize & Register Native Backend Services
        var dbService = new DatabaseService();
        dbService.Initialize();
        var settingsService = new SettingsService(dbService);
        var liveConfigService = new LiveConfigService();
        var providerService = new ProviderService(dbService, settingsService, liveConfigService);
        var mcpService = new McpService(dbService);
        var authService = new AuthService();
        var sessionService = new SessionService();
        var usageService = new UsageService(dbService);
        var sysService = new SystemIntegrationService();
        var profileService = new ProfileService(dbService);
        var streamService = new StreamCheckService(dbService, providerService);
        var cloudSyncService = new CloudSyncService(dbService, providerService, mcpService);
        var liveImportService = new LiveImportService(providerService, mcpService);

        services.AddSingleton(dbService);
        services.AddSingleton(settingsService);
        services.AddSingleton(liveConfigService);
        services.AddSingleton(providerService);
        services.AddSingleton(mcpService);
        services.AddSingleton(authService);
        services.AddSingleton(sessionService);
        services.AddSingleton(usageService);
        services.AddSingleton(sysService);
        services.AddSingleton(profileService);
        services.AddSingleton(streamService);
        services.AddSingleton(cloudSyncService);
        services.AddSingleton(liveImportService);

        // 2. Register Blazor State & Window Services
        services.AddSingleton<AppStateService>();
        services.AddSingleton<ThemeStateService>();
        services.AddSingleton<WindowNativeService>();
        services.AddSingleton<ProviderStateService>();
        services.AddSingleton<McpStateService>();

        Services = services.BuildServiceProvider();
        Resources.Add("services", Services);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var ex = e.Exception;
        var fullMsg = ex.InnerException != null 
            ? $"{ex.Message} -> Inner: {ex.InnerException.Message}\nStack: {ex.InnerException.StackTrace}" 
            : $"{ex.Message}\nStack: {ex.StackTrace}";
        
        LoggerService.Instance.Error("App:Dispatcher", $"Unhandled UI Exception: {fullMsg}", ex);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            var fullMsg = ex.InnerException != null 
                ? $"{ex.Message} -> Inner: {ex.InnerException.Message}\nStack: {ex.InnerException.StackTrace}" 
                : $"{ex.Message}\nStack: {ex.StackTrace}";
            LoggerService.Instance.Error("App:AppDomain", $"Fatal Domain Exception (IsTerminating: {e.IsTerminating}): {fullMsg}", ex);
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LoggerService.Instance.Error("App:TaskScheduler", $"Unobserved Task Exception: {e.Exception.Message}", e.Exception);
        e.SetObserved();
    }
}
