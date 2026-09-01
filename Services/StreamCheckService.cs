using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;

namespace CCSwitch.Wpf.Services;

public class StreamCheckResultDto
{
    [JsonPropertyName("providerId")] public string ProviderId { get; set; } = "";
    [JsonPropertyName("isOk")] public bool IsOk { get; set; }
    [JsonPropertyName("latencyMs")] public long LatencyMs { get; set; }
    [JsonPropertyName("statusCode")] public int StatusCode { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = "";
}

public class StreamCheckService
{
    private readonly DatabaseService _dbService;
    private readonly ProviderService _providerService;
    private readonly HttpClient _httpClient;

    public StreamCheckService(DatabaseService dbService, ProviderService providerService)
    {
        _dbService = dbService;
        _providerService = providerService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        InitializeTables();
    }

    private void InitializeTables()
    {
        using var conn = _dbService.CreateConnection();
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS stream_check_config (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
        ");
    }

    public async Task<StreamCheckResultDto> CheckProviderAsync(string appType, string providerId)
    {
        var providersDict = await _providerService.GetProvidersAsync(appType);
        var providers = providersDict.Values.OfType<ProviderDto>().ToList();
        var provider = providers.FirstOrDefault(p => p.Id == providerId);
        if (provider == null)
        {
            return new StreamCheckResultDto
            {
                ProviderId = providerId,
                IsOk = false,
                LatencyMs = 0,
                StatusCode = 404,
                Message = "Provider not found"
            };
        }

        string baseUrl = "";
        string apiKey = "";

        if (provider.SettingsConfig.TryGetProperty("env", out var envObj) && envObj.ValueKind == JsonValueKind.Object)
        {
            if (envObj.TryGetProperty("ANTHROPIC_BASE_URL", out var b1)) baseUrl = b1.GetString() ?? "";
            else if (envObj.TryGetProperty("OPENAI_BASE_URL", out var b2)) baseUrl = b2.GetString() ?? "";
            else if (envObj.TryGetProperty("BASE_URL", out var b3)) baseUrl = b3.GetString() ?? "";

            if (envObj.TryGetProperty("ANTHROPIC_API_KEY", out var k1)) apiKey = k1.GetString() ?? "";
            else if (envObj.TryGetProperty("OPENAI_API_KEY", out var k2)) apiKey = k2.GetString() ?? "";
            else if (envObj.TryGetProperty("API_KEY", out var k3)) apiKey = k3.GetString() ?? "";
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = appType.ToLowerInvariant() switch
            {
                "codex" => "https://api.openai.com/v1",
                _ => "https://api.anthropic.com/v1"
            };
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var targetUrl = baseUrl.TrimEnd('/') + "/models";
            using var req = new HttpRequestMessage(HttpMethod.Get, targetUrl);
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
                req.Headers.TryAddWithoutValidation("x-api-key", apiKey);
                req.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
            }

            var resp = await _httpClient.SendAsync(req);
            sw.Stop();

            return new StreamCheckResultDto
            {
                ProviderId = providerId,
                IsOk = resp.IsSuccessStatusCode,
                LatencyMs = sw.ElapsedMilliseconds,
                StatusCode = (int)resp.StatusCode,
                Message = resp.IsSuccessStatusCode ? "OK" : $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}"
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new StreamCheckResultDto
            {
                ProviderId = providerId,
                IsOk = false,
                LatencyMs = sw.ElapsedMilliseconds,
                StatusCode = 0,
                Message = ex.Message
            };
        }
    }

    public async Task<List<StreamCheckResultDto>> CheckAllProvidersAsync(string appType)
    {
        var providersDict = await _providerService.GetProvidersAsync(appType);
        var providers = providersDict.Values.OfType<ProviderDto>().ToList();
        var tasks = providers.Select(p => CheckProviderAsync(appType, p.Id));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    public async Task<object> GetStreamCheckConfigAsync()
    {
        using var conn = _dbService.CreateConnection();
        var val = await conn.QueryFirstOrDefaultAsync<string>("SELECT value FROM stream_check_config WHERE key = 'config'");
        if (!string.IsNullOrEmpty(val))
        {
            try { return JsonDocument.Parse(val).RootElement.Clone(); } catch { }
        }
        return new { auto_check = false, interval_minutes = 30, timeout_seconds = 10 };
    }

    public async Task<bool> SaveStreamCheckConfigAsync(JsonElement config)
    {
        using var conn = _dbService.CreateConnection();
        var val = JsonSerializer.Serialize(config);
        await conn.ExecuteAsync(
            "INSERT INTO stream_check_config (key, value) VALUES ('config', @val) ON CONFLICT(key) DO UPDATE SET value = @val",
            new { val }
        );
        return true;
    }

    public async Task<bool> GetAutoFailoverEnabledAsync()
    {
        using var conn = _dbService.CreateConnection();
        var val = await conn.QueryFirstOrDefaultAsync<string>("SELECT value FROM stream_check_config WHERE key = 'auto_failover'");
        return val == "1" || val?.ToLowerInvariant() == "true";
    }

    public async Task<bool> SetAutoFailoverEnabledAsync(bool enabled)
    {
        using var conn = _dbService.CreateConnection();
        var val = enabled ? "1" : "0";
        await conn.ExecuteAsync(
            "INSERT INTO stream_check_config (key, value) VALUES ('auto_failover', @val) ON CONFLICT(key) DO UPDATE SET value = @val",
            new { val }
        );
        return true;
    }

    public async Task<List<string>> GetFailoverQueueAsync(string appType)
    {
        using var conn = _dbService.CreateConnection();
        var rows = await conn.QueryAsync<string>(
            "SELECT provider_id FROM failover_queue WHERE app_type = @appType ORDER BY priority ASC",
            new { appType }
        );
        return rows.ToList();
    }

    public async Task<bool> AddToFailoverQueueAsync(string appType, string providerId)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(
            "INSERT INTO failover_queue (app_type, provider_id, priority) VALUES (@appType, @providerId, 0) ON CONFLICT DO NOTHING",
            new { appType, providerId }
        );
        return true;
    }

    public async Task<bool> RemoveFromFailoverQueueAsync(string appType, string providerId)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM failover_queue WHERE app_type = @appType AND provider_id = @providerId",
            new { appType, providerId }
        );
        return true;
    }

    public object GetCircuitBreakerConfig()
    {
        return new { enabled = true, failure_threshold = 3, reset_timeout_seconds = 60 };
    }

    public object GetCircuitBreakerStats()
    {
        return new { total_trips = 0, active_broken_providers = Array.Empty<string>() };
    }
}