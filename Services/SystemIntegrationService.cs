using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using Microsoft.Win32;
using WpfApplication = System.Windows.Application;

namespace CCSwitch.Wpf.Services;

public class EnvConflictDto
{
    [JsonPropertyName("key")] public string Key { get; set; } = "";
    [JsonPropertyName("value")] public string Value { get; set; } = "";
    [JsonPropertyName("source")] public string Source { get; set; } = "User"; // "User", "Machine", "Process"
    [JsonPropertyName("appType")] public string AppType { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
}

public class SystemIntegrationService
{
    private const string AutoRunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "CCSwitch";

    public static string GetConfigDir()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cc-switch");
        Directory.CreateDirectory(dir);
        return dir;
    }

    public string? OpenFileDialog(string? filter = null, string? title = null, string? initialDirectory = null)
    {
        return WpfApplication.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = filter ?? "All Files (*.*)|*.*",
                Title = title ?? "Select File",
                InitialDirectory = initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        });
    }

    public string? SaveFileDialog(string? filter = null, string? title = null, string? defaultName = null, string? initialDirectory = null)
    {
        return WpfApplication.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter ?? "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = title ?? "Save File",
                FileName = defaultName ?? "backup.json",
                InitialDirectory = initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        });
    }

    public string? PickDirectory(string? title = null, string? initialDirectory = null)
    {
        return WpfApplication.Current.Dispatcher.Invoke(() =>
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = title ?? "Select Folder",
                SelectedPath = initialDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                UseDescriptionForTitle = true
            };
            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
        });
    }

    public bool CopyTextToClipboard(string text)
    {
        try
        {
            WpfApplication.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.Clipboard.SetText(text ?? "");
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool OpenExternal(string url)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool OpenDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool OpenAppConfigFolder(string appType)
    {
        var path = appType.ToLowerInvariant() switch
        {
            "claude" => LiveConfigService.GetClaudeDir(),
            "codex" => LiveConfigService.GetCodexDir(),
            "opencode" => LiveConfigService.GetOpenCodeDir(),
            _ => GetConfigDir()
        };
        return OpenDirectory(path);
    }

    public bool GetAutoLaunchStatus()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AutoRunRegistryKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public bool SetAutoLaunch(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(AutoRunRegistryKey, true);
            if (key == null) return false;

            if (enable)
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\" --minimized");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void RestartApp()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
            }
            WpfApplication.Current.Dispatcher.Invoke(() => WpfApplication.Current.Shutdown());
        }
        catch { }
    }

    public Dictionary<string, List<EnvConflictDto>> CheckAllEnvConflicts()
    {
        var result = new Dictionary<string, List<EnvConflictDto>>(StringComparer.OrdinalIgnoreCase);

        var monitoredVars = new (string Key, string AppType, string Desc)[]
        {
            ("ANTHROPIC_API_KEY", "claude", "May override active Claude provider API key"),
            ("ANTHROPIC_BASE_URL", "claude", "May override active Claude provider endpoint URL"),
            ("ANTHROPIC_AUTH_TOKEN", "claude", "May override Claude authentication token"),
            ("OPENAI_API_KEY", "codex", "May override active Codex provider API key"),
            ("OPENAI_BASE_URL", "codex", "May override active Codex provider endpoint URL"),
            ("OPENCODE_API_KEY", "opencode", "May override active OpenCode provider API key")
        };

        var targets = new[] { EnvironmentVariableTarget.Process, EnvironmentVariableTarget.User, EnvironmentVariableTarget.Machine };

        foreach (var item in monitoredVars)
        {
            foreach (var target in targets)
            {
                var val = Environment.GetEnvironmentVariable(item.Key, target);
                if (!string.IsNullOrWhiteSpace(val))
                {
                    if (!result.ContainsKey(item.AppType))
                    {
                        result[item.AppType] = new List<EnvConflictDto>();
                    }

                    // Mask key for safety
                    var masked = val.Length > 8 ? $"{val[..4]}...{val[^4..]}" : "********";
                    result[item.AppType].Add(new EnvConflictDto
                    {
                        Key = item.Key,
                        Value = masked,
                        Source = target.ToString(),
                        AppType = item.AppType,
                        Description = item.Desc
                    });
                    break;
                }
            }
        }

        return result;
    }

    public List<EnvConflictDto> CheckEnvConflicts(string appType)
    {
        var all = CheckAllEnvConflicts();
        return all.TryGetValue(appType, out var list) ? list : new List<EnvConflictDto>();
    }

    public Dictionary<string, object> ProbeToolInstallations()
    {
        var tools = new[] { "claude", "codex", "opencode" };
        var result = new Dictionary<string, object>();

        foreach (var tool in tools)
        {
            var (installed, version) = CheckBinaryInPath(tool);
            result[tool] = new { installed, version };
        }

        return result;
    }

    private static (bool Installed, string Version) CheckBinaryInPath(string binaryName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = binaryName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                p.WaitForExit(1000);
                if (p.ExitCode == 0)
                {
                    return (true, "installed");
                }
            }
        }
        catch { }
        return (false, "not_installed");
    }
}