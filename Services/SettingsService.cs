using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dapper;

namespace CCSwitch.Wpf.Services;

public class SettingsService
{
    private readonly DatabaseService _dbService;
    private readonly string _settingsFilePath;
    private readonly object _lock = new();

    public SettingsService(DatabaseService dbService)
    {
        _dbService = dbService;
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cc-switch"
        );
        Directory.CreateDirectory(appData);
        _settingsFilePath = Path.Combine(appData, "settings.json");
    }

    public static string GetCurrentProviderKey(string appType)
    {
        var normalized = appType.Replace("-", "").ToLowerInvariant();
        return normalized switch
        {
            "claude" => "currentProviderClaude",
            "codex" => "currentProviderCodex",
            "grokbuild" => "currentProviderGrokbuild",
            "opencode" => "currentProviderOpencode",
            "antigravity" => "currentProviderAntigravity",
            _ => $"currentProvider{char.ToUpperInvariant(appType[0])}{appType[1..]}"
        };
    }

    public async Task<JsonNode> GetSettingsNodeAsync()
    {
        lock (_lock)
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    var text = File.ReadAllText(_settingsFilePath);
                    var node = JsonNode.Parse(text);
                    if (node != null)
                    {
                        return node;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsService] Read settings.json failed: {ex.Message}");
                }
            }
        }

        // Fallback default
        var defaultNode = new JsonObject
        {
            ["showInTray"] = true,
            ["minimizeToTrayOnClose"] = true,
            ["useAppWindowControls"] = false,
            ["enableClaudePluginIntegration"] = true,
            ["skipClaudeOnboarding"] = true,
            ["launchOnStartup"] = true,
            ["silentStartup"] = true,
            ["enableLocalProxy"] = true,
            ["proxyConfirmed"] = true,
            ["usageConfirmed"] = true,
            ["usageDashboardRefreshIntervalMs"] = 5000,
            ["enableFailoverToggle"] = false,
            ["showProfileSwitcher"] = true,
            ["firstRunNoticeConfirmed"] = true,
            ["commonConfigConfirmed"] = true,
            ["language"] = "zh",
            ["visibleApps"] = new JsonObject
            {
                ["claude"] = true,
                ["codex"] = true,
                ["grokbuild"] = true,
                ["opencode"] = true,
                ["antigravity"] = true
            }
        };

        await SaveSettingsNodeAsync(defaultNode);
        return defaultNode;
    }

    public async Task<object> GetSettingsAsync()
    {
        var node = await GetSettingsNodeAsync();
        return node;
    }

    public async Task<bool> SaveSettingsAsync(JsonElement settingsElement)
    {
        try
        {
            var rawText = JsonSerializer.Serialize(settingsElement, new JsonSerializerOptions { WriteIndented = true });
            var node = JsonNode.Parse(rawText);
            if (node != null)
            {
                await SaveSettingsNodeAsync(node);
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] SaveSettingsAsync failed: {ex.Message}");
        }

        return false;
    }

    public Task SaveSettingsNodeAsync(JsonNode node)
    {
        lock (_lock)
        {
            var json = node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);

            try
            {
                using var conn = _dbService.CreateConnection();
                conn.Execute(
                    @"INSERT INTO settings (key, value) VALUES ('app_settings', @json)
                      ON CONFLICT(key) DO UPDATE SET value = @json",
                    new { json }
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService] Backup to DB failed: {ex.Message}");
            }
        }

        return Task.CompletedTask;
    }

    public async Task<string?> GetCurrentProviderForAppAsync(string appType)
    {
        var node = await GetSettingsNodeAsync();
        var key = GetCurrentProviderKey(appType);
        if (node is JsonObject obj && obj.TryGetPropertyValue(key, out var val) && val != null)
        {
            return val.GetValue<string?>();
        }
        return null;
    }

    public async Task SetCurrentProviderForAppAsync(string appType, string providerId)
    {
        var node = await GetSettingsNodeAsync();
        var key = GetCurrentProviderKey(appType);
        if (node is JsonObject obj)
        {
            obj[key] = providerId;
            await SaveSettingsNodeAsync(obj);
        }
    }
}
