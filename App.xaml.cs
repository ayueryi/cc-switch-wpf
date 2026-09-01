using System.Windows;
using System.Windows.Threading;
using CCSwitch.Wpf.Services;

namespace CCSwitch.Wpf;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Hook UI thread unhandled exceptions
        DispatcherUnhandledException += App_DispatcherUnhandledException;

        // 2. Hook non-UI thread unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        // 3. Hook unobserved task exceptions
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        LoggerService.Instance.Info("App", $"=== CC Switch Application Starting (PID: {Environment.ProcessId}, OS: {Environment.OSVersion}, .NET: {Environment.Version}) ===");
        LoggerService.Instance.Info("App", $"Log destination: {LoggerService.Instance.GetCurrentLogFilePath()}");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LoggerService.Instance.Info("App", $"=== CC Switch Application Exiting (ExitCode: {e.ApplicationExitCode}) ===");
        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LoggerService.Instance.Error("App:Dispatcher", $"Unhandled UI Exception: {e.Exception.Message}", e.Exception);
        // Prevent crash if recoverable, but ensure logged
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            LoggerService.Instance.Error("App:AppDomain", $"Fatal Unhandled Domain Exception (IsTerminating: {e.IsTerminating}): {ex.Message}", ex);
        }
        else
        {
            LoggerService.Instance.Error("App:AppDomain", $"Fatal Unhandled Domain Exception (IsTerminating: {e.IsTerminating}): {e.ExceptionObject}");
        }
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LoggerService.Instance.Error("App:TaskScheduler", $"Unobserved Task Exception: {e.Exception.Message}", e.Exception);
        e.SetObserved();
    }
}
