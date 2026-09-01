using System.Text.Json.Serialization;
using Dapper;

namespace CCSwitch.Wpf.Services;

public class UsageSummaryDto
{
    [JsonPropertyName("totalRequests")] public ulong TotalRequests { get; set; }
    [JsonPropertyName("totalCost")] public string TotalCost { get; set; } = "0.00";
    [JsonPropertyName("totalInputTokens")] public ulong TotalInputTokens { get; set; }
    [JsonPropertyName("totalOutputTokens")] public ulong TotalOutputTokens { get; set; }
    [JsonPropertyName("totalCacheCreationTokens")] public ulong TotalCacheCreationTokens { get; set; }
    [JsonPropertyName("totalCacheReadTokens")] public ulong TotalCacheReadTokens { get; set; }
    [JsonPropertyName("successRate")] public double SuccessRate { get; set; } = 1.0;
    [JsonPropertyName("realTotalTokens")] public ulong RealTotalTokens { get; set; }
    [JsonPropertyName("cacheHitRate")] public double CacheHitRate { get; set; }
}

public class UsageSummaryByAppDto
{
    [JsonPropertyName("appType")] public string AppType { get; set; } = "";
    [JsonPropertyName("summary")] public UsageSummaryDto Summary { get; set; } = new();
}

public class DailyStatsDto
{
    [JsonPropertyName("date")] public string Date { get; set; } = "";
    [JsonPropertyName("requestCount")] public ulong RequestCount { get; set; }
    [JsonPropertyName("totalCost")] public string TotalCost { get; set; } = "0.00";
    [JsonPropertyName("totalTokens")] public ulong TotalTokens { get; set; }
    [JsonPropertyName("totalInputTokens")] public ulong TotalInputTokens { get; set; }
    [JsonPropertyName("totalOutputTokens")] public ulong TotalOutputTokens { get; set; }
    [JsonPropertyName("totalCacheCreationTokens")] public ulong TotalCacheCreationTokens { get; set; }
    [JsonPropertyName("totalCacheReadTokens")] public ulong TotalCacheReadTokens { get; set; }
}

public class ProviderStatsDto
{
    [JsonPropertyName("providerId")] public string ProviderId { get; set; } = "";
    [JsonPropertyName("providerName")] public string ProviderName { get; set; } = "";
    [JsonPropertyName("requestCount")] public ulong RequestCount { get; set; }
    [JsonPropertyName("totalTokens")] public ulong TotalTokens { get; set; }
    [JsonPropertyName("totalCost")] public string TotalCost { get; set; } = "0.00";
    [JsonPropertyName("successRate")] public double SuccessRate { get; set; } = 1.0;
    [JsonPropertyName("avgLatencyMs")] public ulong AvgLatencyMs { get; set; }
}

public class ModelStatsDto
{
    [JsonPropertyName("model")] public string Model { get; set; } = "";
    [JsonPropertyName("requestCount")] public ulong RequestCount { get; set; }
    [JsonPropertyName("totalTokens")] public ulong TotalTokens { get; set; }
    [JsonPropertyName("totalCost")] public string TotalCost { get; set; } = "0.00";
    [JsonPropertyName("avgCostPerRequest")] public string AvgCostPerRequest { get; set; } = "0.00";
}
public class RequestLogDto
{
    [JsonPropertyName("requestId")] public string RequestId { get; set; } = "";
    [JsonPropertyName("providerId")] public string ProviderId { get; set; } = "";
    [JsonPropertyName("providerName")] public string? ProviderName { get; set; }
    [JsonPropertyName("appType")] public string AppType { get; set; } = "";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
    [JsonPropertyName("requestModel")] public string? RequestModel { get; set; }
    [JsonPropertyName("pricingModel")] public string? PricingModel { get; set; }
    [JsonPropertyName("costMultiplier")] public string CostMultiplier { get; set; } = "1.0";
    [JsonPropertyName("inputTokens")] public ulong InputTokens { get; set; }
    [JsonPropertyName("outputTokens")] public ulong OutputTokens { get; set; }
    [JsonPropertyName("cacheReadTokens")] public ulong CacheReadTokens { get; set; }
    [JsonPropertyName("cacheCreationTokens")] public ulong CacheCreationTokens { get; set; }
    [JsonPropertyName("inputCostUsd")] public string InputCostUsd { get; set; } = "0.00";
    [JsonPropertyName("outputCostUsd")] public string OutputCostUsd { get; set; } = "0.00";
    [JsonPropertyName("cacheReadCostUsd")] public string CacheReadCostUsd { get; set; } = "0.00";
    [JsonPropertyName("cacheCreationCostUsd")] public string CacheCreationCostUsd { get; set; } = "0.00";
    [JsonPropertyName("totalCostUsd")] public string TotalCostUsd { get; set; } = "0.00";
    [JsonPropertyName("isStreaming")] public bool IsStreaming { get; set; }
    [JsonPropertyName("latencyMs")] public ulong LatencyMs { get; set; }
    [JsonPropertyName("firstTokenMs")] public ulong? FirstTokenMs { get; set; }
    [JsonPropertyName("durationMs")] public ulong? DurationMs { get; set; }
    [JsonPropertyName("statusCode")] public int StatusCode { get; set; } = 200;
    [JsonPropertyName("errorMessage")] public string? ErrorMessage { get; set; }
    [JsonPropertyName("createdAt")] public long CreatedAt { get; set; }
    [JsonPropertyName("dataSource")] public string? DataSource { get; set; }
}

public class LogFiltersDto
{
    [JsonPropertyName("appType")] public string? AppType { get; set; }
    [JsonPropertyName("providerName")] public string? ProviderName { get; set; }
    [JsonPropertyName("model")] public string? Model { get; set; }
    [JsonPropertyName("statusCode")] public int? StatusCode { get; set; }
    [JsonPropertyName("startDate")] public long? StartDate { get; set; }
    [JsonPropertyName("endDate")] public long? EndDate { get; set; }
}

public class PaginatedLogsDto
{
    [JsonPropertyName("data")] public List<RequestLogDto> Data { get; set; } = new();
    [JsonPropertyName("total")] public uint Total { get; set; }
    [JsonPropertyName("page")] public uint Page { get; set; }
    [JsonPropertyName("pageSize")] public uint PageSize { get; set; }
}

public class ModelPricingDto
{
    [JsonPropertyName("modelId")] public string ModelId { get; set; } = "";
    [JsonPropertyName("displayName")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("inputCostPerMillion")] public string InputCostPerMillion { get; set; } = "0";
    [JsonPropertyName("outputCostPerMillion")] public string OutputCostPerMillion { get; set; } = "0";
    [JsonPropertyName("cacheReadCostPerMillion")] public string CacheReadCostPerMillion { get; set; } = "0";
    [JsonPropertyName("cacheCreationCostPerMillion")] public string CacheCreationCostPerMillion { get; set; } = "0";
}

public class ModelsDevSyncConfigDto
{
    [JsonPropertyName("autoSyncEnabled")] public bool AutoSyncEnabled { get; set; }
    [JsonPropertyName("includeCommonModels")] public bool IncludeCommonModels { get; set; }
    [JsonPropertyName("selectedModelKeys")] public List<string> SelectedModelKeys { get; set; } = new();
    [JsonPropertyName("excludedCommonModelKeys")] public List<string> ExcludedCommonModelKeys { get; set; } = new();
    [JsonPropertyName("lastSyncAt")] public long? LastSyncAt { get; set; }
    [JsonPropertyName("lastSyncError")] public string? LastSyncError { get; set; }
}

public class ModelsDevSyncStateDto
{
    [JsonPropertyName("config")] public ModelsDevSyncConfigDto Config { get; set; } = new();
    [JsonPropertyName("configPath")] public string ConfigPath { get; set; } = "";
}

public class ProviderLimitStatusDto
{
    [JsonPropertyName("providerId")] public string ProviderId { get; set; } = "";
    [JsonPropertyName("dailyUsage")] public string DailyUsage { get; set; } = "0.00";
    [JsonPropertyName("dailyLimit")] public string? DailyLimit { get; set; }
    [JsonPropertyName("dailyExceeded")] public bool DailyExceeded { get; set; }
    [JsonPropertyName("monthlyUsage")] public string MonthlyUsage { get; set; } = "0.00";
    [JsonPropertyName("monthlyLimit")] public string? MonthlyLimit { get; set; }
    [JsonPropertyName("monthlyExceeded")] public bool MonthlyExceeded { get; set; }
}

public class SessionSyncResultDto
{
    [JsonPropertyName("imported")] public int Imported { get; set; }
    [JsonPropertyName("skipped")] public int Skipped { get; set; }
    [JsonPropertyName("filesScanned")] public int FilesScanned { get; set; }
    [JsonPropertyName("suspectedDuplicates")] public int SuspectedDuplicates { get; set; }
    [JsonPropertyName("deferredFiles")] public int DeferredFiles { get; set; }
    [JsonPropertyName("errors")] public List<string> Errors { get; set; } = new();
}

public class DataSourceSummaryDto
{
    [JsonPropertyName("dataSource")] public string DataSource { get; set; } = "";
    [JsonPropertyName("requestCount")] public int RequestCount { get; set; }
    [JsonPropertyName("totalCostUsd")] public string TotalCostUsd { get; set; } = "0.00";
}
public class UsageService
{
    private readonly DatabaseService _dbService;

    public UsageService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task<UsageSummaryDto> GetUsageSummaryAsync(long? startDate = null, long? endDate = null, string? appType = null, string? providerName = null, string? model = null)
    {
        using var conn = _dbService.CreateConnection();
        
        var sql = @"
            SELECT 
                COUNT(1) AS TotalRequests,
                COALESCE(SUM(input_tokens), 0) AS TotalInputTokens,
                COALESCE(SUM(output_tokens), 0) AS TotalOutputTokens,
                COALESCE(SUM(cache_creation_tokens), 0) AS TotalCacheCreationTokens,
                COALESCE(SUM(cache_read_tokens), 0) AS TotalCacheReadTokens,
                COALESCE(SUM(CAST(total_cost_usd AS REAL)), 0.0) AS TotalCostNum,
                COALESCE(SUM(CASE WHEN status_code >= 200 AND status_code < 400 THEN 1 ELSE 0 END), 0) AS SuccessRequests
            FROM proxy_request_logs
            WHERE 1=1
        ";

        if (startDate.HasValue) sql += " AND created_at >= @startDate";
        if (endDate.HasValue) sql += " AND created_at <= @endDate";
        if (!string.IsNullOrEmpty(appType)) sql += " AND app_type = @appType";
        if (!string.IsNullOrEmpty(providerName)) sql += " AND provider_id = @providerName";
        if (!string.IsNullOrEmpty(model)) sql += " AND (model = @model OR pricing_model = @model)";

        try
        {
            var row = await conn.QueryFirstOrDefaultAsync(sql, new { startDate, endDate, appType, providerName, model });
            if (row != null)
            {
                ulong totalReq = row.TotalRequests != null ? (ulong)(long)row.TotalRequests : 0;
                ulong inputTokens = row.TotalInputTokens != null ? (ulong)(long)row.TotalInputTokens : 0;
                ulong outputTokens = row.TotalOutputTokens != null ? (ulong)(long)row.TotalOutputTokens : 0;
                ulong cacheCreation = row.TotalCacheCreationTokens != null ? (ulong)(long)row.TotalCacheCreationTokens : 0;
                ulong cacheRead = row.TotalCacheReadTokens != null ? (ulong)(long)row.TotalCacheReadTokens : 0;
                double cost = row.TotalCostNum != null ? (double)row.TotalCostNum : 0.0;
                ulong successReq = row.SuccessRequests != null ? (ulong)(long)row.SuccessRequests : 0;

                ulong realTotal = inputTokens + outputTokens + cacheCreation + cacheRead;
                ulong cacheableInput = inputTokens + cacheCreation + cacheRead;
                double hitRate = cacheableInput > 0 ? (double)cacheRead / cacheableInput : 0.0;
                double successRate = totalReq > 0 ? (double)successReq / totalReq : 1.0;

                return new UsageSummaryDto
                {
                    TotalRequests = totalReq,
                    TotalCost = cost.ToString("F4"),
                    TotalInputTokens = inputTokens,
                    TotalOutputTokens = outputTokens,
                    TotalCacheCreationTokens = cacheCreation,
                    TotalCacheReadTokens = cacheRead,
                    SuccessRate = successRate,
                    RealTotalTokens = realTotal,
                    CacheHitRate = hitRate
                };
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UsageService] Query proxy_request_logs failed: {ex.Message}");
        }

        return new UsageSummaryDto();
    }

    public async Task<List<UsageSummaryByAppDto>> GetUsageSummaryByAppAsync(long? startDate = null, long? endDate = null, string? providerName = null, string? model = null)
    {
        var list = new List<UsageSummaryByAppDto>();
        var apps = new[] { "claude", "codex", "grokbuild", "opencode", "cursor", "antigravity", "cpa" };

        foreach (var app in apps)
        {
            var summary = await GetUsageSummaryAsync(startDate, endDate, app, providerName, model);
            list.Add(new UsageSummaryByAppDto
            {
                AppType = app,
                Summary = summary
            });
        }

        return list;
    }

    public async Task<List<DailyStatsDto>> GetUsageTrendsAsync(long? startDate = null, long? endDate = null, string? appType = null, string? providerName = null, string? model = null)
    {
        using var conn = _dbService.CreateConnection();
        var sql = @"
            SELECT 
                strftime('%Y-%m-%d', datetime(created_at, 'unixepoch', 'localtime')) AS DateStr,
                COUNT(1) AS RequestCount,
                COALESCE(SUM(CAST(total_cost_usd AS REAL)), 0.0) AS TotalCostNum,
                COALESCE(SUM(input_tokens + output_tokens + cache_creation_tokens + cache_read_tokens), 0) AS TotalTokens,
                COALESCE(SUM(input_tokens), 0) AS TotalInputTokens,
                COALESCE(SUM(output_tokens), 0) AS TotalOutputTokens,
                COALESCE(SUM(cache_creation_tokens), 0) AS TotalCacheCreationTokens,
                COALESCE(SUM(cache_read_tokens), 0) AS TotalCacheReadTokens
            FROM proxy_request_logs
            WHERE 1=1
        ";

        if (startDate.HasValue) sql += " AND created_at >= @startDate";
        if (endDate.HasValue) sql += " AND created_at <= @endDate";
        if (!string.IsNullOrEmpty(appType)) sql += " AND app_type = @appType";
        if (!string.IsNullOrEmpty(providerName)) sql += " AND provider_id = @providerName";
        if (!string.IsNullOrEmpty(model)) sql += " AND (model = @model OR pricing_model = @model)";

        sql += " GROUP BY DateStr ORDER BY DateStr ASC";

        var list = new List<DailyStatsDto>();
        try
        {
            var rows = await conn.QueryAsync(sql, new { startDate, endDate, appType, providerName, model });
            foreach (var r in rows)
            {
                list.Add(new DailyStatsDto
                {
                    Date = (string)(r.DateStr ?? ""),
                    RequestCount = r.RequestCount != null ? (ulong)(long)r.RequestCount : 0,
                    TotalCost = r.TotalCostNum != null ? ((double)r.TotalCostNum).ToString("F4") : "0.00",
                    TotalTokens = r.TotalTokens != null ? (ulong)(long)r.TotalTokens : 0,
                    TotalInputTokens = r.TotalInputTokens != null ? (ulong)(long)r.TotalInputTokens : 0,
                    TotalOutputTokens = r.TotalOutputTokens != null ? (ulong)(long)r.TotalOutputTokens : 0,
                    TotalCacheCreationTokens = r.TotalCacheCreationTokens != null ? (ulong)(long)r.TotalCacheCreationTokens : 0,
                    TotalCacheReadTokens = r.TotalCacheReadTokens != null ? (ulong)(long)r.TotalCacheReadTokens : 0
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UsageService] GetUsageTrends failed: {ex.Message}");
        }

        return list;
    }
    public async Task<List<ProviderStatsDto>> GetProviderStatsAsync(long? startDate = null, long? endDate = null, string? appType = null, string? providerName = null, string? model = null)
    {
        using var conn = _dbService.CreateConnection();
        var sql = @"
            SELECT 
                l.provider_id AS ProviderId,
                COALESCE(p.name, l.provider_id) AS ProviderName,
                COUNT(1) AS RequestCount,
                COALESCE(SUM(l.input_tokens + l.output_tokens + l.cache_creation_tokens + l.cache_read_tokens), 0) AS TotalTokens,
                COALESCE(SUM(CAST(l.total_cost_usd AS REAL)), 0.0) AS TotalCostNum,
                COALESCE(SUM(CASE WHEN l.status_code >= 200 AND l.status_code < 400 THEN 1 ELSE 0 END), 0) AS SuccessReq,
                COALESCE(AVG(l.latency_ms), 0) AS AvgLatency
            FROM proxy_request_logs l
            LEFT JOIN providers p ON l.provider_id = p.id AND l.app_type = p.app_type
            WHERE 1=1
        ";

        if (startDate.HasValue) sql += " AND l.created_at >= @startDate";
        if (endDate.HasValue) sql += " AND l.created_at <= @endDate";
        if (!string.IsNullOrEmpty(appType)) sql += " AND l.app_type = @appType";
        if (!string.IsNullOrEmpty(providerName)) sql += " AND (l.provider_id = @providerName OR p.name = @providerName)";
        if (!string.IsNullOrEmpty(model)) sql += " AND (l.model = @model OR l.pricing_model = @model)";

        sql += " GROUP BY l.provider_id, ProviderName ORDER BY RequestCount DESC";

        var list = new List<ProviderStatsDto>();
        try
        {
            var rows = await conn.QueryAsync(sql, new { startDate, endDate, appType, providerName, model });
            foreach (var r in rows)
            {
                ulong reqCount = r.RequestCount != null ? (ulong)(long)r.RequestCount : 0;
                ulong succCount = r.SuccessReq != null ? (ulong)(long)r.SuccessReq : 0;
                double cost = r.TotalCostNum != null ? (double)r.TotalCostNum : 0.0;
                double successRate = reqCount > 0 ? (double)succCount / reqCount : 1.0;

                list.Add(new ProviderStatsDto
                {
                    ProviderId = (string)r.ProviderId,
                    ProviderName = (string)r.ProviderName,
                    RequestCount = reqCount,
                    TotalTokens = r.TotalTokens != null ? (ulong)(long)r.TotalTokens : 0,
                    TotalCost = cost.ToString("F4"),
                    SuccessRate = successRate,
                    AvgLatencyMs = r.AvgLatency != null ? (ulong)(long)r.AvgLatency : 0
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UsageService] GetProviderStats failed: {ex.Message}");
        }

        return list;
    }

    public async Task<List<ModelStatsDto>> GetModelStatsAsync(long? startDate = null, long? endDate = null, string? appType = null, string? providerName = null, string? model = null)
    {
        using var conn = _dbService.CreateConnection();
        var sql = @"
            SELECT 
                COALESCE(pricing_model, model) AS EffectiveModel,
                COUNT(1) AS RequestCount,
                COALESCE(SUM(input_tokens + output_tokens + cache_creation_tokens + cache_read_tokens), 0) AS TotalTokens,
                COALESCE(SUM(CAST(total_cost_usd AS REAL)), 0.0) AS TotalCostNum
            FROM proxy_request_logs
            WHERE 1=1
        ";

        if (startDate.HasValue) sql += " AND created_at >= @startDate";
        if (endDate.HasValue) sql += " AND created_at <= @endDate";
        if (!string.IsNullOrEmpty(appType)) sql += " AND app_type = @appType";
        if (!string.IsNullOrEmpty(providerName)) sql += " AND provider_id = @providerName";
        if (!string.IsNullOrEmpty(model)) sql += " AND (model = @model OR pricing_model = @model)";

        sql += " GROUP BY EffectiveModel ORDER BY RequestCount DESC";

        var list = new List<ModelStatsDto>();
        try
        {
            var rows = await conn.QueryAsync(sql, new { startDate, endDate, appType, providerName, model });
            foreach (var r in rows)
            {
                ulong reqCount = r.RequestCount != null ? (ulong)(long)r.RequestCount : 0;
                double cost = r.TotalCostNum != null ? (double)r.TotalCostNum : 0.0;
                double avgCost = reqCount > 0 ? cost / reqCount : 0.0;

                list.Add(new ModelStatsDto
                {
                    Model = (string)(r.EffectiveModel ?? "unknown"),
                    RequestCount = reqCount,
                    TotalTokens = r.TotalTokens != null ? (ulong)(long)r.TotalTokens : 0,
                    TotalCost = cost.ToString("F4"),
                    AvgCostPerRequest = avgCost.ToString("F4")
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UsageService] GetModelStats failed: {ex.Message}");
        }

        return list;
    }

    public async Task<PaginatedLogsDto> GetRequestLogsAsync(LogFiltersDto? filters, uint page = 0, uint pageSize = 20)
    {
        using var conn = _dbService.CreateConnection();
        var whereClause = " WHERE 1=1";
        var param = new DynamicParameters();

        if (filters != null)
        {
            if (!string.IsNullOrEmpty(filters.AppType))
            {
                whereClause += " AND l.app_type = @AppType";
                param.Add("AppType", filters.AppType);
            }
            if (!string.IsNullOrEmpty(filters.ProviderName))
            {
                whereClause += " AND (l.provider_id = @ProviderName OR p.name = @ProviderName)";
                param.Add("ProviderName", filters.ProviderName);
            }
            if (!string.IsNullOrEmpty(filters.Model))
            {
                whereClause += " AND (l.model = @Model OR l.pricing_model = @Model)";
                param.Add("Model", filters.Model);
            }
            if (filters.StatusCode.HasValue)
            {
                whereClause += " AND l.status_code = @StatusCode";
                param.Add("StatusCode", filters.StatusCode.Value);
            }
            if (filters.StartDate.HasValue)
            {
                whereClause += " AND l.created_at >= @StartDate";
                param.Add("StartDate", filters.StartDate.Value);
            }
            if (filters.EndDate.HasValue)
            {
                whereClause += " AND l.created_at <= @EndDate";
                param.Add("EndDate", filters.EndDate.Value);
            }
        }

        try
        {
            var countSql = $"SELECT COUNT(1) FROM proxy_request_logs l LEFT JOIN providers p ON l.provider_id = p.id AND l.app_type = p.app_type {whereClause}";
            var total = await conn.ExecuteScalarAsync<uint>(countSql, param);

            var offset = (int)(page * pageSize);
            param.Add("Limit", (int)pageSize);
            param.Add("Offset", offset);

            var querySql = $@"
                SELECT 
                    l.request_id AS RequestId,
                    l.provider_id AS ProviderId,
                    p.name AS ProviderName,
                    l.app_type AS AppType,
                    l.model AS Model,
                    l.request_model AS RequestModel,
                    l.pricing_model AS PricingModel,
                    l.cost_multiplier AS CostMultiplier,
                    l.input_tokens AS InputTokens,
                    l.output_tokens AS OutputTokens,
                    l.cache_read_tokens AS CacheReadTokens,
                    l.cache_creation_tokens AS CacheCreationTokens,
                    l.input_cost_usd AS InputCostUsd,
                    l.output_cost_usd AS OutputCostUsd,
                    l.cache_read_cost_usd AS CacheReadCostUsd,
                    l.cache_creation_cost_usd AS CacheCreationCostUsd,
                    l.total_cost_usd AS TotalCostUsd,
                    l.is_streaming AS IsStreaming,
                    l.latency_ms AS LatencyMs,
                    l.first_token_ms AS FirstTokenMs,
                    l.duration_ms AS DurationMs,
                    l.status_code AS StatusCode,
                    l.error_message AS ErrorMessage,
                    l.created_at AS CreatedAt,
                    l.data_source AS DataSource
                FROM proxy_request_logs l
                LEFT JOIN providers p ON l.provider_id = p.id AND l.app_type = p.app_type
                {whereClause}
                ORDER BY l.created_at DESC
                LIMIT @Limit OFFSET @Offset
            ";

            var rows = await conn.QueryAsync<RequestLogDto>(querySql, param);
            return new PaginatedLogsDto
            {
                Data = rows.ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UsageService] GetRequestLogs failed: {ex.Message}");
            return new PaginatedLogsDto { Page = page, PageSize = pageSize };
        }
    }

    public async Task<RequestLogDto?> GetRequestDetailAsync(string requestId)
    {
        using var conn = _dbService.CreateConnection();
        var sql = @"
            SELECT 
                l.request_id AS RequestId,
                l.provider_id AS ProviderId,
                p.name AS ProviderName,
                l.app_type AS AppType,
                l.model AS Model,
                l.request_model AS RequestModel,
                l.pricing_model AS PricingModel,
                l.cost_multiplier AS CostMultiplier,
                l.input_tokens AS InputTokens,
                l.output_tokens AS OutputTokens,
                l.cache_read_tokens AS CacheReadTokens,
                l.cache_creation_tokens AS CacheCreationTokens,
                l.input_cost_usd AS InputCostUsd,
                l.output_cost_usd AS OutputCostUsd,
                l.cache_read_cost_usd AS CacheReadCostUsd,
                l.cache_creation_cost_usd AS CacheCreationCostUsd,
                l.total_cost_usd AS TotalCostUsd,
                l.is_streaming AS IsStreaming,
                l.latency_ms AS LatencyMs,
                l.first_token_ms AS FirstTokenMs,
                l.duration_ms AS DurationMs,
                l.status_code AS StatusCode,
                l.error_message AS ErrorMessage,
                l.created_at AS CreatedAt,
                l.data_source AS DataSource
            FROM proxy_request_logs l
            LEFT JOIN providers p ON l.provider_id = p.id AND l.app_type = p.app_type
            WHERE l.request_id = @requestId
        ";

        try
        {
            return await conn.QueryFirstOrDefaultAsync<RequestLogDto>(sql, new { requestId });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UsageService] GetRequestDetail failed: {ex.Message}");
            return null;
        }
    }

    public async Task<List<ModelPricingDto>> GetModelPricingAsync()
    {
        using var conn = _dbService.CreateConnection();
        try
        {
            var rows = await conn.QueryAsync(
                @"SELECT model_id, display_name, input_cost_per_million, output_cost_per_million,
                         cache_read_cost_per_million, cache_creation_cost_per_million
                  FROM model_pricing
                  ORDER BY display_name ASC"
            );

            var list = new List<ModelPricingDto>();
            foreach (var r in rows)
            {
                list.Add(new ModelPricingDto
                {
                    ModelId = (string)r.model_id,
                    DisplayName = (string)r.display_name,
                    InputCostPerMillion = (string)r.input_cost_per_million,
                    OutputCostPerMillion = (string)r.output_cost_per_million,
                    CacheReadCostPerMillion = (string)(r.cache_read_cost_per_million ?? "0"),
                    CacheCreationCostPerMillion = (string)(r.cache_creation_cost_per_million ?? "0")
                });
            }
            return list;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UsageService] GetModelPricing failed: {ex.Message}");
            return new List<ModelPricingDto>();
        }
    }

    public async Task<bool> UpdateModelPricingAsync(ModelPricingDto entry)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO model_pricing (
                model_id, display_name, input_cost_per_million, output_cost_per_million,
                cache_read_cost_per_million, cache_creation_cost_per_million
            ) VALUES (
                @ModelId, @DisplayName, @InputCostPerMillion, @OutputCostPerMillion,
                @CacheReadCostPerMillion, @CacheCreationCostPerMillion
            )
            ON CONFLICT(model_id) DO UPDATE SET
                display_name = @DisplayName,
                input_cost_per_million = @InputCostPerMillion,
                output_cost_per_million = @OutputCostPerMillion,
                cache_read_cost_per_million = @CacheReadCostPerMillion,
                cache_creation_cost_per_million = @CacheCreationCostPerMillion",
            entry
        );
        return true;
    }

    public async Task<int> UpdateModelPricingBatchAsync(List<ModelPricingDto> entries)
    {
        int count = 0;
        foreach (var entry in entries)
        {
            if (await UpdateModelPricingAsync(entry)) count++;
        }
        return count;
    }

    public async Task<bool> DeleteModelPricingAsync(string modelId)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM model_pricing WHERE model_id = @modelId", new { modelId });
        return true;
    }

    public Task<ModelsDevSyncStateDto> GetModelsDevSyncConfigAsync()
    {
        return Task.FromResult(new ModelsDevSyncStateDto
        {
            Config = new ModelsDevSyncConfigDto
            {
                AutoSyncEnabled = false,
                IncludeCommonModels = true
            },
            ConfigPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cc-switch", "model-pricing.json")
        });
    }

    public Task SaveModelsDevSyncConfigAsync(ModelsDevSyncConfigDto config)
    {
        return Task.CompletedTask;
    }

    public Task RecordModelsDevSyncResultAsync(long? syncedAt, string? error)
    {
        return Task.CompletedTask;
    }

    public async Task<ProviderLimitStatusDto> CheckProviderLimitsAsync(string providerId, string appType)
    {
        var summary = await GetUsageSummaryAsync(
            startDate: new DateTimeOffset(DateTime.UtcNow.Date).ToUnixTimeSeconds(),
            appType: appType,
            providerName: providerId
        );

        return new ProviderLimitStatusDto
        {
            ProviderId = providerId,
            DailyUsage = summary.TotalCost,
            DailyExceeded = false,
            MonthlyUsage = summary.TotalCost,
            MonthlyExceeded = false
        };
    }

    public Task<SessionSyncResultDto> SyncSessionUsageAsync()
    {
        return Task.FromResult(new SessionSyncResultDto
        {
            Imported = 0,
            FilesScanned = 0
        });
    }

    public Task<SessionSyncResultDto> RebuildCodexUsageAsync()
    {
        return Task.FromResult(new SessionSyncResultDto
        {
            Imported = 0,
            FilesScanned = 0
        });
    }

    public async Task<List<DataSourceSummaryDto>> GetDataSourceBreakdownAsync()
    {
        using var conn = _dbService.CreateConnection();
        var sql = @"
            SELECT 
                COALESCE(data_source, 'proxy') AS DataSource,
                COUNT(1) AS RequestCount,
                COALESCE(SUM(CAST(total_cost_usd AS REAL)), 0.0) AS TotalCostNum
            FROM proxy_request_logs
            GROUP BY DataSource
        ";

        var list = new List<DataSourceSummaryDto>();
        try
        {
            var rows = await conn.QueryAsync(sql);
            foreach (var r in rows)
            {
                double cost = r.TotalCostNum != null ? (double)r.TotalCostNum : 0.0;
                list.Add(new DataSourceSummaryDto
                {
                    DataSource = (string)r.DataSource,
                    RequestCount = r.RequestCount != null ? (int)(long)r.RequestCount : 0,
                    TotalCostUsd = cost.ToString("F4")
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UsageService] GetDataSourceBreakdown failed: {ex.Message}");
        }

        return list;
    }
}
