using System.IO;
using System.Text;
using System.Text.Json;

namespace CCSwitch.Wpf.Services;

public class LiveConfigService
{
    private static readonly string HomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static readonly string AppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

    public static string GetClaudeDir() => Path.Combine(HomeDir, ".claude");
    public static string GetCodexDir() => Path.Combine(HomeDir, ".codex");
    public static string GetGrokBuildDir() => Path.Combine(HomeDir, ".grok");
    public static string GetOpenCodeDir() => Path.Combine(HomeDir, ".config", "opencode");

    public async Task<bool> SyncProviderToLiveAsync(string appType, ProviderDto provider)
    {
        try
        {
            var normalizedApp = appType.Replace("-", "").ToLowerInvariant();
            switch (normalizedApp)
            {
                case "claude":
                    await WriteClaudeLiveConfigAsync(provider);
                    break;
                case "codex":
                    await WriteCodexLiveConfigAsync(provider);
                    break;
                case "grokbuild":
                    await WriteGrokBuildLiveConfigAsync(provider);
                    break;
                case "opencode":
                    await WriteOpenCodeLiveConfigAsync(provider);
                    break;
                default:
                    LoggerService.Instance.Warn("LiveConfig", $"Unsupported appType for live write: {appType}");
                    break;
            }
            return true;
        }
        catch (Exception ex)
        {
            LoggerService.Instance.Error("LiveConfig", $"Failed to sync live config for {appType}: {ex.Message}", ex);
            return false;
        }
    }

    private static async Task WriteClaudeLiveConfigAsync(ProviderDto provider)
    {
        var claudeDir = GetClaudeDir();
        Directory.CreateDirectory(claudeDir);

        var settingsPath = Path.Combine(claudeDir, "settings.json");
        var json = JsonSerializer.Serialize(provider.SettingsConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsPath, json, Encoding.UTF8);
        LoggerService.Instance.Info("LiveConfig", $"Synced Claude live config -> {settingsPath}");

        // Also write ~/.claude.json if it existed previously
        var legacyPath = Path.Combine(HomeDir, ".claude.json");
        if (File.Exists(legacyPath))
        {
            await File.WriteAllTextAsync(legacyPath, json, Encoding.UTF8);
        }
    }

    private static async Task WriteCodexLiveConfigAsync(ProviderDto provider)
    {
        var codexDir = GetCodexDir();
        Directory.CreateDirectory(codexDir);

        var cfg = provider.SettingsConfig;

        // 1. Write auth.json if present
        if (cfg.TryGetProperty("auth", out var authElement))
        {
            var authPath = Path.Combine(codexDir, "auth.json");
            var authJson = JsonSerializer.Serialize(authElement, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(authPath, authJson, Encoding.UTF8);
        }

        // 2. Write config.toml if present
        if (cfg.TryGetProperty("config", out var configElement))
        {
            var configPath = Path.Combine(codexDir, "config.toml");
            var configText = configElement.ValueKind == JsonValueKind.String
                ? configElement.GetString() ?? ""
                : configElement.ToString();

            await File.WriteAllTextAsync(configPath, configText, Encoding.UTF8);
        LoggerService.Instance.Info("LiveConfig", $"Synced Codex live config -> {configPath}");
        }
    }

    private static async Task WriteGrokBuildLiveConfigAsync(ProviderDto provider)
    {
        var grokDir = GetGrokBuildDir();
        Directory.CreateDirectory(grokDir);

        var cfg = provider.SettingsConfig;
        if (cfg.TryGetProperty("config", out var configElement))
        {
            var configPath = Path.Combine(grokDir, "config.toml");
            var configText = configElement.ValueKind == JsonValueKind.String
                ? configElement.GetString() ?? ""
                : configElement.ToString();

            await File.WriteAllTextAsync(configPath, configText, Encoding.UTF8);
            LoggerService.Instance.Info("LiveConfig", $"Synced GrokBuild live config -> {configPath}");
        }
    }

    private static async Task WriteOpenCodeLiveConfigAsync(ProviderDto provider)
    {
        var opencodeDir = GetOpenCodeDir();
        Directory.CreateDirectory(opencodeDir);

        var configPath = Path.Combine(opencodeDir, "config.json");
        var json = JsonSerializer.Serialize(provider.SettingsConfig, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configPath, json, Encoding.UTF8);
        LoggerService.Instance.Info("LiveConfig", $"Synced OpenCode live config -> {configPath}");
    }
}
