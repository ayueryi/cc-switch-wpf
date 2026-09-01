using System.Text.Json.Serialization;
using Dapper;

namespace CCSwitch.Wpf.Services;

public class PromptDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("appType")] public string AppType { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("content")] public string Content { get; set; } = "";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("createdAt")] public long? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")] public long? UpdatedAt { get; set; }
}

public class PromptService
{
    private readonly DatabaseService _dbService;

    public PromptService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task<Dictionary<string, PromptDto>> GetPromptsAsync(string appType)
    {
        using var conn = _dbService.CreateConnection();
        var rows = await conn.QueryAsync(
            @"SELECT id, app_type, name, content, description, enabled, created_at, updated_at
              FROM prompts 
              WHERE app_type = @appType 
              ORDER BY created_at ASC, id ASC",
            new { appType }
        );

        var result = new Dictionary<string, PromptDto>();
        foreach (var row in rows)
        {
            var dto = new PromptDto
            {
                Id = (string)row.id,
                AppType = (string)row.app_type,
                Name = (string)row.name,
                Content = (string)row.content,
                Description = (string?)row.description,
                Enabled = row.enabled == null || (long)row.enabled == 1,
                CreatedAt = row.created_at != null ? (long)row.created_at : null,
                UpdatedAt = row.updated_at != null ? (long)row.updated_at : null
            };
            result[dto.Id] = dto;
        }
        return result;
    }

    public async Task<bool> UpsertPromptAsync(string appType, string id, PromptDto prompt)
    {
        var targetId = !string.IsNullOrEmpty(id) ? id : prompt.Id;
        if (string.IsNullOrEmpty(targetId))
        {
            targetId = Guid.NewGuid().ToString();
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var createdAt = prompt.CreatedAt ?? now;
        var updatedAt = now;

        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO prompts (
                id, app_type, name, content, description, enabled, created_at, updated_at
            ) VALUES (
                @targetId, @appType, @Name, @Content, @Description, @Enabled, @createdAt, @updatedAt
            )
            ON CONFLICT(id, app_type) DO UPDATE SET
                name = @Name,
                content = @Content,
                description = @Description,
                enabled = @Enabled,
                updated_at = @updatedAt",
            new
            {
                targetId,
                appType,
                prompt.Name,
                prompt.Content,
                prompt.Description,
                Enabled = prompt.Enabled ? 1 : 0,
                createdAt,
                updatedAt
            }
        );

        return true;
    }

        public Task<bool> UpsertPromptAsync(string appType, PromptDto prompt) => UpsertPromptAsync(appType, prompt.Id, prompt);
    public async Task<bool> DeletePromptAsync(string appType, string id)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM prompts WHERE app_type = @appType AND id = @id",
            new { appType, id }
        );
        return true;
    }

    public async Task<bool> EnablePromptAsync(string appType, string id, bool enabled = true)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE prompts SET enabled = @val, updated_at = @now WHERE app_type = @appType AND id = @id",
            new { val = enabled ? 1 : 0, now = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), appType, id }
        );
        return true;
    }
}
