using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;

namespace CCSwitch.Wpf.Services;

public class McpAppsDto
{
    [JsonPropertyName("claude")] public bool Claude { get; set; }
    [JsonPropertyName("codex")] public bool Codex { get; set; }
    [JsonPropertyName("grokbuild")] public bool Grokbuild { get; set; }
    [JsonPropertyName("opencode")] public bool Opencode { get; set; }
}

public class McpServerDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("server")] public JsonElement Server { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("homepage")] public string? Homepage { get; set; }
    [JsonPropertyName("docs")] public string? Docs { get; set; }
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
    [JsonPropertyName("apps")] public McpAppsDto Apps { get; set; } = new();
}

public class McpService
{
    private readonly DatabaseService _dbService;

    public McpService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task<Dictionary<string, McpServerDto>> GetAllServersAsync()
    {
        using var conn = _dbService.CreateConnection();
        var rows = await conn.QueryAsync(
            @"SELECT id, name, server_config, description, homepage, docs, tags,
                     enabled_claude, enabled_codex, enabled_grokbuild,
                     enabled_opencode
              FROM mcp_servers"
        );

        var result = new Dictionary<string, McpServerDto>();
        foreach (var row in rows)
        {
            try
            {
                var serverJson = (string)row.server_config;
                var tagsJson = (string)(row.tags ?? "[]");

                var dto = new McpServerDto
                {
                    Id = (string)row.id,
                    Name = (string)row.name,
                    Server = JsonDocument.Parse(string.IsNullOrWhiteSpace(serverJson) ? "{}" : serverJson).RootElement.Clone(),
                    Description = (string?)row.description,
                    Homepage = (string?)row.homepage,
                    Docs = (string?)row.docs,
                    Tags = JsonSerializer.Deserialize<List<string>>(tagsJson) ?? new(),
                    Apps = new McpAppsDto
                    {
                        Claude = row.enabled_claude != null && (long)row.enabled_claude == 1,
                        Codex = row.enabled_codex != null && (long)row.enabled_codex == 1,
                        Grokbuild = row.enabled_grokbuild != null && (long)row.enabled_grokbuild == 1,
                        Opencode = row.enabled_opencode != null && (long)row.enabled_opencode == 1,
                    }
                };

                result[dto.Id] = dto;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[McpService] Failed to parse MCP server {row.id}: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<bool> UpsertServerAsync(McpServerDto server)
    {
        var serverJson = JsonSerializer.Serialize(server.Server);
        var tagsJson = JsonSerializer.Serialize(server.Tags);

        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO mcp_servers (
                id, name, server_config, description, homepage, docs, tags,
                enabled_claude, enabled_codex, enabled_grokbuild, enabled_opencode
            ) VALUES (
                @Id, @Name, @serverJson, @Description, @Homepage, @Docs, @tagsJson,
                @Claude, @Codex, @Grokbuild, @Opencode
            )
            ON CONFLICT(id) DO UPDATE SET
                name = @Name,
                server_config = @serverJson,
                description = @Description,
                homepage = @Homepage,
                docs = @Docs,
                tags = @tagsJson,
                enabled_claude = @Claude,
                enabled_codex = @Codex,
                enabled_grokbuild = @Grokbuild,
                enabled_opencode = @Opencode",
            new
            {
                server.Id,
                server.Name,
                serverJson,
                server.Description,
                server.Homepage,
                server.Docs,
                tagsJson,
                Claude = server.Apps.Claude ? 1 : 0,
                Codex = server.Apps.Codex ? 1 : 0,
                Grokbuild = server.Apps.Grokbuild ? 1 : 0,
                Opencode = server.Apps.Opencode ? 1 : 0,
            }
        );

        return true;
    }

    public async Task<bool> DeleteServerAsync(string id)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM mcp_servers WHERE id = @id", new { id });
        return true;
    }

        public Task<Dictionary<string, McpServerDto>> GetMcpServersAsync() => GetAllServersAsync();
    public Task<bool> UpsertMcpServerAsync(McpServerDto server) => UpsertServerAsync(server);
    public Task<bool> DeleteMcpServerAsync(string id) => DeleteServerAsync(id);
    public async Task<bool> ToggleMcpAppAsync(string serverId, string app, bool enabled)
    {
        var column = app.ToLowerInvariant() switch
        {
            "claude" => "enabled_claude",
            "codex" => "enabled_codex",
            "grokbuild" => "enabled_grokbuild",
            "opencode" => "enabled_opencode",
            _ => null
        };

        if (column == null) return false;

        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(
            $"UPDATE mcp_servers SET {column} = @val WHERE id = @serverId",
            new { val = enabled ? 1 : 0, serverId }
        );

        return true;
    }
}
