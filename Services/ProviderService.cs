using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Microsoft.Data.Sqlite;

namespace CCSwitch.Wpf.Services;

public class ProviderDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("settingsConfig")]
    public JsonElement SettingsConfig { get; set; }

    [JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("createdAt")]
    public long? CreatedAt { get; set; }

    [JsonPropertyName("sortIndex")]
    public int? SortIndex { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("iconColor")]
    public string? IconColor { get; set; }

    [JsonPropertyName("meta")]
    public JsonElement? Meta { get; set; }

    [JsonPropertyName("inFailoverQueue")]
    public bool InFailoverQueue { get; set; }
}

public class ProviderSortUpdateDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("sortIndex")]
    public int SortIndex { get; set; }
}

public class ProviderService
{
    private readonly DatabaseService _dbService;
    private readonly SettingsService _settingsService;
    private readonly LiveConfigService _liveConfigService;

    public ProviderService(DatabaseService dbService, SettingsService settingsService, LiveConfigService liveConfigService)
    {
        _dbService = dbService;
        _settingsService = settingsService;
        _liveConfigService = liveConfigService;
    }

    public async Task<Dictionary<string, object>> GetProvidersAsync(string appType)
    {
        using var conn = _dbService.CreateConnection();
        var rows = await conn.QueryAsync(
            @"SELECT id, name, settings_config, website_url, category, created_at, 
                     sort_index, notes, icon, icon_color, meta, in_failover_queue 
              FROM providers 
              WHERE app_type = @appType 
              ORDER BY sort_index ASC, created_at ASC",
            new { appType }
        );

        var result = new Dictionary<string, object>();
        foreach (var row in rows)
        {
            try
            {
                var settingsJson = (string)row.settings_config;
                var metaJson = (string)(row.meta ?? "{}");

                var dto = new ProviderDto
                {
                    Id = (string)row.id,
                    Name = (string)row.name,
                    SettingsConfig = JsonDocument.Parse(string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson).RootElement.Clone(),
                    WebsiteUrl = (string?)row.website_url,
                    Category = (string?)row.category,
                    CreatedAt = row.created_at != null ? (long)row.created_at : null,
                    SortIndex = row.sort_index != null ? (int)row.sort_index : null,
                    Notes = (string?)row.notes,
                    Icon = (string?)row.icon,
                    IconColor = (string?)row.icon_color,
                    Meta = JsonDocument.Parse(string.IsNullOrWhiteSpace(metaJson) ? "{}" : metaJson).RootElement.Clone(),
                    InFailoverQueue = row.in_failover_queue != null && (long)row.in_failover_queue == 1
                };

                result[dto.Id] = dto;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProviderService] Failed to deserialize provider {row.id}: {ex.Message}");
            }
        }
        return result;
    }

    public async Task<ProviderDto?> GetProviderByIdAsync(string appType, string providerId)
    {
        using var conn = _dbService.CreateConnection();
        var row = await conn.QueryFirstOrDefaultAsync(
            @"SELECT id, name, settings_config, website_url, category, created_at, 
                     sort_index, notes, icon, icon_color, meta, in_failover_queue 
              FROM providers 
              WHERE app_type = @appType AND id = @providerId 
              LIMIT 1",
            new { appType, providerId }
        );

        if (row == null) return null;

        var settingsJson = (string)row.settings_config;
        var metaJson = (string)(row.meta ?? "{}");

        return new ProviderDto
        {
            Id = (string)row.id,
            Name = (string)row.name,
            SettingsConfig = JsonDocument.Parse(string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson).RootElement.Clone(),
            WebsiteUrl = (string?)row.website_url,
            Category = (string?)row.category,
            CreatedAt = row.created_at != null ? (long)row.created_at : null,
            SortIndex = row.sort_index != null ? (int)row.sort_index : null,
            Notes = (string?)row.notes,
            Icon = (string?)row.icon,
            IconColor = (string?)row.icon_color,
            Meta = JsonDocument.Parse(string.IsNullOrWhiteSpace(metaJson) ? "{}" : metaJson).RootElement.Clone(),
            InFailoverQueue = row.in_failover_queue != null && (long)row.in_failover_queue == 1
        };
    }

    public async Task<string> GetCurrentProviderAsync(string appType)
    {
        // 1. Check settings.json first
        var fromSettings = await _settingsService.GetCurrentProviderForAppAsync(appType);
        if (!string.IsNullOrEmpty(fromSettings))
        {
            using var conn = _dbService.CreateConnection();
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM providers WHERE app_type = @appType AND id = @id",
                new { appType, id = fromSettings }
            );

            if (count > 0)
            {
                return fromSettings;
            }
        }

        // 2. Fallback to SQLite DB is_current
        {
            using var conn = _dbService.CreateConnection();
            var currentId = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT id FROM providers WHERE app_type = @appType AND is_current = 1 LIMIT 1",
                new { appType }
            );

            if (!string.IsNullOrEmpty(currentId))
            {
                return currentId;
            }
        }

        return "default";
    }

    public async Task SwitchProviderAsync(string appType, string providerId)
    {
        // 1. Update settings.json
        await _settingsService.SetCurrentProviderForAppAsync(appType, providerId);

        // 2. Update DB is_current
        using var conn = _dbService.CreateConnection();
        using var tx = conn.BeginTransaction();

        await conn.ExecuteAsync(
            "UPDATE providers SET is_current = 0 WHERE app_type = @appType",
            new { appType },
            tx
        );

        await conn.ExecuteAsync(
            "UPDATE providers SET is_current = 1 WHERE app_type = @appType AND id = @providerId",
            new { appType, providerId },
            tx
        );

        tx.Commit();

        // 3. Live Sync to disk configuration file
        var provider = await GetProviderByIdAsync(appType, providerId);
        LoggerService.Instance.Info("ProviderService", $"Switched active provider for {appType} -> {providerId} ({provider?.Name})");
        if (provider != null)
        {
            await _liveConfigService.SyncProviderToLiveAsync(appType, provider);
        }
    }

    public async Task<bool> AddProviderAsync(string appType, ProviderDto provider, bool addToLive = false)
    {
        if (string.IsNullOrWhiteSpace(provider.Id))
        {
            provider.Id = Guid.NewGuid().ToString();
        }
        provider.CreatedAt ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var settingsJson = JsonSerializer.Serialize(provider.SettingsConfig);
        var metaJson = provider.Meta != null ? JsonSerializer.Serialize(provider.Meta) : "{}";

        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO providers (
                id, app_type, name, settings_config, website_url, category, 
                created_at, sort_index, notes, icon, icon_color, meta, 
                is_current, in_failover_queue
            ) VALUES (
                @Id, @appType, @Name, @settingsJson, @WebsiteUrl, @Category, 
                @CreatedAt, @SortIndex, @Notes, @Icon, @IconColor, @metaJson, 
                0, @InFailoverQueue
            )",
            new
            {
                provider.Id,
                appType,
                provider.Name,
                settingsJson,
                provider.WebsiteUrl,
                provider.Category,
                provider.CreatedAt,
                provider.SortIndex,
                provider.Notes,
                provider.Icon,
                provider.IconColor,
                metaJson,
                InFailoverQueue = provider.InFailoverQueue ? 1 : 0
            }
        );

        if (addToLive)
        {
            await SwitchProviderAsync(appType, provider.Id);
        }

        return true;
    }

    public async Task<bool> UpdateProviderAsync(string appType, ProviderDto provider, string? originalId = null)
    {
        var targetId = !string.IsNullOrEmpty(originalId) ? originalId : provider.Id;
        var settingsJson = JsonSerializer.Serialize(provider.SettingsConfig);
        var metaJson = provider.Meta != null ? JsonSerializer.Serialize(provider.Meta) : "{}";

        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(@"
            UPDATE providers SET
                id = @NewId,
                name = @Name,
                settings_config = @settingsJson,
                website_url = @WebsiteUrl,
                category = @Category,
                sort_index = @SortIndex,
                notes = @Notes,
                icon = @Icon,
                icon_color = @IconColor,
                meta = @metaJson,
                in_failover_queue = @InFailoverQueue
            WHERE app_type = @appType AND id = @targetId",
            new
            {
                NewId = provider.Id,
                provider.Name,
                settingsJson,
                provider.WebsiteUrl,
                provider.Category,
                provider.SortIndex,
                provider.Notes,
                provider.Icon,
                provider.IconColor,
                metaJson,
                InFailoverQueue = provider.InFailoverQueue ? 1 : 0,
                appType,
                targetId
            }
        );

        var current = await GetCurrentProviderAsync(appType);
        if (current == provider.Id || current == targetId)
        {
            await _liveConfigService.SyncProviderToLiveAsync(appType, provider);
        }

        return true;
    }

    public async Task<bool> DeleteProviderAsync(string appType, string providerId)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM providers WHERE app_type = @appType AND id = @providerId",
            new { appType, providerId }
        );

        var current = await GetCurrentProviderAsync(appType);
        if (current == providerId)
        {
            var fallback = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT id FROM providers WHERE app_type = @appType ORDER BY sort_index ASC, created_at ASC LIMIT 1",
                new { appType }
            );

            if (!string.IsNullOrEmpty(fallback))
            {
                await SwitchProviderAsync(appType, fallback);
            }
        }

        return true;
    }

    public async Task<bool> UpdateProvidersSortOrderAsync(string appType, List<ProviderSortUpdateDto> updates)
    {
        using var conn = _dbService.CreateConnection();
        using var tx = conn.BeginTransaction();

        foreach (var update in updates)
        {
            await conn.ExecuteAsync(
                "UPDATE providers SET sort_index = @SortIndex WHERE app_type = @appType AND id = @Id",
                new { update.SortIndex, appType, update.Id },
                tx
            );
        }

        tx.Commit();
        return true;
    }

        public Task<string> GetCurrentProviderIdAsync(string appType) => GetCurrentProviderAsync(appType);
    public async Task<bool> SetCurrentProviderAsync(string appType, string providerId) { await SwitchProviderAsync(appType, providerId); return true; }
    public async Task<bool> SaveProviderAsync(string appType, ProviderDto provider)
    {
        var existing = await GetProviderByIdAsync(appType, provider.Id);
        if (existing != null)
        {
            return await UpdateProviderAsync(appType, provider);
        }
        return await AddProviderAsync(appType, provider);
    }
        public async Task<bool> SetProviderSortOrderAsync(string appType, List<string> orderedIds)
    {
        var updates = new List<ProviderSortUpdateDto>();
        for (int i = 0; i < orderedIds.Count; i++)
        {
            updates.Add(new ProviderSortUpdateDto { Id = orderedIds[i], SortIndex = i });
        }
        return await UpdateProvidersSortOrderAsync(appType, updates);
    }
    public Task<bool> SetSortOrderAsync(string appType, List<ProviderSortUpdateDto> updates) => UpdateProvidersSortOrderAsync(appType, updates);
    public async Task SyncAllCurrentProvidersToLiveAsync()
    {
        var apps = new[] { "claude", "codex", "opencode" };
        foreach (var app in apps)
        {
            var curId = await GetCurrentProviderAsync(app);
            if (!string.IsNullOrEmpty(curId) && curId != "default")
            {
                var provider = await GetProviderByIdAsync(app, curId);
                if (provider != null)
                {
                    await _liveConfigService.SyncProviderToLiveAsync(app, provider);
                }
            }
        }
    }
}
