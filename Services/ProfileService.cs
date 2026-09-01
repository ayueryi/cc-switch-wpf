using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;

namespace CCSwitch.Wpf.Services;

public class ProfileDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("is_active")] public bool IsActive { get; set; }
    [JsonPropertyName("created_at")] public long CreatedAt { get; set; }
}

public class ProfilesResponseDto
{
    [JsonPropertyName("profiles")] public List<ProfileDto> Profiles { get; set; } = new();
    [JsonPropertyName("active_id")] public string? ActiveId { get; set; }
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
        using var conn = _dbService.CreateConnection();
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS profiles (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                description TEXT,
                is_active INTEGER DEFAULT 0,
                created_at INTEGER NOT NULL,
                data_json TEXT NOT NULL
            );
        ");
    }

    public async Task<ProfilesResponseDto> ListProfilesAsync()
    {
        using var conn = _dbService.CreateConnection();
        var rows = await conn.QueryAsync(
            "SELECT id, name, description, is_active, created_at FROM profiles ORDER BY created_at ASC"
        );

        var list = new List<ProfileDto>();
        string? activeId = null;

        foreach (var r in rows)
        {
            var active = r.is_active != null && (long)r.is_active == 1;
            var id = (string)r.id;
            if (active) activeId = id;

            list.Add(new ProfileDto
            {
                Id = id,
                Name = (string)r.name,
                Description = (string?)r.description,
                IsActive = active,
                CreatedAt = (long)r.created_at
            });
        }

        return new ProfilesResponseDto
        {
            Profiles = list,
            ActiveId = activeId
        };
    }

    public async Task<ProfileDto> CreateProfileAsync(string name, string? description)
    {
        var id = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        using var conn = _dbService.CreateConnection();
        var providers = await conn.QueryAsync("SELECT * FROM providers");
        var dataJson = JsonSerializer.Serialize(providers);

        await conn.ExecuteAsync(@"
            INSERT INTO profiles (id, name, description, is_active, created_at, data_json)
            VALUES (@id, @name, @description, 0, @now, @dataJson)",
            new { id, name, description, now, dataJson }
        );

        return new ProfileDto
        {
            Id = id,
            Name = name,
            Description = description,
            IsActive = false,
            CreatedAt = now
        };
    }

    public async Task<bool> ApplyProfileAsync(string id)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync("UPDATE profiles SET is_active = CASE WHEN id = @id THEN 1 ELSE 0 END", new { id });
        return true;
    }

    public async Task<bool> UpdateProfileAsync(string id, string name, string? description)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE profiles SET name = @name, description = @description WHERE id = @id",
            new { id, name, description }
        );
        return true;
    }

    public async Task<bool> DeleteProfileAsync(string id)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM profiles WHERE id = @id", new { id });
        return true;
    }

    public async Task<bool> ClearCurrentProfileAsync()
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync("UPDATE profiles SET is_active = 0");
        return true;
    }
}