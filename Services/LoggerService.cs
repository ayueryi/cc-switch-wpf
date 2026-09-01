using System.IO;
using System.Text;

namespace CCSwitch.Wpf.Services;

public enum LogLevel
{
    Debug,
    Info,
    Warn,
    Error
}

public class LoggerService
{
    private static readonly Lazy<LoggerService> _lazyInstance = new(() => new LoggerService());
    public static LoggerService Instance => _lazyInstance.Value;

    private readonly object _lock = new();
    private readonly string _logDir;

    public LoggerService()
    {
        _logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CCSwitch",
            "logs"
        );

        try
        {
            Directory.CreateDirectory(_logDir);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoggerService] Failed to create log directory: {ex.Message}");
        }
    }

    public string LogDirectory => _logDir;

    public string GetCurrentLogFilePath()
    {
        var fileName = $"ccswitch_{DateTime.Now:yyyyMMdd}.log";
        return Path.Combine(_logDir, fileName);
    }

    public void Log(LogLevel level, string source, string message, Exception? ex = null)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpperInvariant();
        var logLine = ex == null
            ? $"[{timestamp}] [{levelStr}] [{source}] {message}"
            : $"[{timestamp}] [{levelStr}] [{source}] {message} Exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";

        System.Diagnostics.Debug.WriteLine(logLine);

        lock (_lock)
        {
            try
            {
                var filePath = GetCurrentLogFilePath();
                File.AppendAllText(filePath, logLine + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception writeEx)
            {
                System.Diagnostics.Debug.WriteLine($"[LoggerService] Failed to write to log file: {writeEx.Message}");
            }
        }
    }

    public void Debug(string source, string message) => Log(LogLevel.Debug, source, message);
    public void Info(string source, string message) => Log(LogLevel.Info, source, message);
    public void Warn(string source, string message, Exception? ex = null) => Log(LogLevel.Warn, source, message, ex);
    public void Error(string source, string message, Exception? ex = null) => Log(LogLevel.Error, source, message, ex);
}
