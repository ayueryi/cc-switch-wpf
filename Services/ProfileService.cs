using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;

namespace CCSwitch.Wpf.Services;

public class ProfileDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("payload")] public JsonElement Payload { get; set; }
    [JsonPropertyName("createdAt")] public long? CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")] public long? UpdatedAt { get; set; }
}

public class CurrentProfileIdsDto
{
    [JsonPropertyName("claude")] public string? Claude { get; set; }
    [JsonPropertyName("claudeDesktop")] public string? ClaudeDesktop { get; set; }
    [JsonPropertyName("codex")] public string? Codex { get; set; }
}

public class ProfilesResponseDto
{
    [JsonPropertyName("profiles")] public List<ProfileDto> Profiles { get; set; } = new();
    [JsonPropertyName("currentIds")] public CurrentProfileIdsDto CurrentIds { get; set; } = new();
}

public class ProfileService
{
    private readonly DatabaseService _dbService;

    public ProfileService(DatabaseService dbService)
    {
        _dbService = dbService;
        InitializeTable();
    }

    private void InitializeTable()
    {
        try
        {
            using var conn = _dbService.CreateConnection();
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS profiles (
                    id TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    payload TEXT NOT NULL DEFAULT '{}',
                    sort_order INTEGER,
                    created_at INTEGER,
                    updated_at INTEGER
                );
            ");
        }
        catch (Exception ex)
        {
            LoggerService.Instance.Error("ProfileService", $"InitializeTable failed: {ex.Message}", ex);
        }
    }

    public async Task<ProfilesResponseDto> ListProfilesAsync()
    {
        try
        {
            using var conn = _dbService.CreateConnection();
            var rows = await conn.QueryAsync(
                "SELECT id, name, payload, created_at, updated_at FROM profiles ORDER BY created_at ASC"
            );

            var list = new List<ProfileDto>();
            foreach (var r in rows)
            {
                JsonElement payloadElement;
                try
                {
                    var payloadStr = (string?)r.payload ?? "{}";
                    payloadElement = JsonDocument.Parse(payloadStr).RootElement.Clone();
                }
                catch
                {
                    payloadElement = JsonDocument.Parse("{}").RootElement.Clone();
                }

                list.Add(new ProfileDto
                {
                    Id = (string)r.id,
                    Name = (string)r.name,
                    Payload = payloadElement,
                    CreatedAt = r.created_at != null ? (long?)r.created_at : null,
                    UpdatedAt = r.updated_at != null ? (long?)r.updated_at : null
                });
            }

            return new ProfilesResponseDto
            {
                Profiles = list,
                CurrentIds = new CurrentProfileIdsDto()
            };
        }
        catch (Exception ex)
        {
            LoggerService.Instance.Error("ProfileService", $"ListProfilesAsync error: {ex.Message}", ex);
            return new ProfilesResponseDto();
        }
    }

    public async Task<ProfileDto> CreateProfileAsync(string name, string? scope)
    {
        var id = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var emptyPayload = "{\"providers\":{},\"mcp\":{},\"skills\":{},\"prompts\":{}}";

        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO profiles (id, name, payload, sort_order, created_at, updated_at)
            VALUES (@id, @name, @emptyPayload, 0, @now, @now)",
            new { id, name, emptyPayload, now }
        );

        return new ProfileDto
        {
            Id = id,
            Name = name,
            Payload = JsonDocument.Parse(emptyPayload).RootElement.Clone(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public async Task<List<string>> ApplyProfileAsync(string id)
    {
        return new List<string>();
    }

    public async Task<ProfileDto?> UpdateProfileAsync(string id, string? name)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        using var conn = _dbService.CreateConnection();
        if (!string.IsNullOrEmpty(name))
        {
            await conn.ExecuteAsync(
                "UPDATE profiles SET name = @name, updated_at = @now WHERE id = @id",
                new { id, name, now }
            );
        }
        var row = await conn.QueryFirstOrDefaultAsync("SELECT * FROM profiles WHERE id = @id", new { id });
        if (row == null) return null;

        JsonElement payloadElement;
        try
        {
            var payloadStr = (string?)row.payload ?? "{}";
            payloadElement = JsonDocument.Parse(payloadStr).RootElement.Clone();
        }
        catch
        {
            payloadElement = JsonDocument.Parse("{}").RootElement.Clone();
        }

        return new ProfileDto
        {
            Id = (string)row.id,
            Name = (string)row.name,
            Payload = payloadElement,
            CreatedAt = row.created_at != null ? (long?)row.created_at : null,
            UpdatedAt = row.updated_at != null ? (long?)row.updated_at : null
        };
    }

    public async Task<bool> DeleteProfileAsync(string id)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM profiles WHERE id = @id", new { id });
        return true;
    }

    public async Task<bool> ClearCurrentProfileAsync()
    {
        return true;
    }
}
