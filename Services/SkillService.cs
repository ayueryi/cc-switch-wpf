using System.Text.Json.Serialization;
using Dapper;

namespace CCSwitch.Wpf.Services;

public class SkillAppsDto
{
    [JsonPropertyName("claude")] public bool Claude { get; set; }
    [JsonPropertyName("codex")] public bool Codex { get; set; }
    [JsonPropertyName("grokbuild")] public bool Grokbuild { get; set; }
    [JsonPropertyName("opencode")] public bool Opencode { get; set; }
}

public class InstalledSkillDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("directory")] public string Directory { get; set; } = "";
    [JsonPropertyName("repoOwner")] public string? RepoOwner { get; set; }
    [JsonPropertyName("repoName")] public string? RepoName { get; set; }
    [JsonPropertyName("repoBranch")] public string? RepoBranch { get; set; }
    [JsonPropertyName("readmeUrl")] public string? ReadmeUrl { get; set; }
    [JsonPropertyName("apps")] public SkillAppsDto Apps { get; set; } = new();
    [JsonPropertyName("installedAt")] public long InstalledAt { get; set; }
    [JsonPropertyName("contentHash")] public string? ContentHash { get; set; }
    [JsonPropertyName("updatedAt")] public long UpdatedAt { get; set; }
}

public class SkillService
{
    private readonly DatabaseService _dbService;

    public SkillService(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    public async Task<List<InstalledSkillDto>> GetInstalledSkillsAsync()
    {
        using var conn = _dbService.CreateConnection();
        var rows = await conn.QueryAsync(
            @"SELECT id, name, description, directory, repo_owner, repo_name, repo_branch,
                     readme_url, enabled_claude, enabled_codex, enabled_grokbuild,
                     enabled_opencode, installed_at, content_hash, updated_at
              FROM skills 
              ORDER BY name ASC"
        );

        var list = new List<InstalledSkillDto>();
        foreach (var row in rows)
        {
            var dto = new InstalledSkillDto
            {
                Id = (string)row.id,
                Name = (string)row.name,
                Description = (string?)row.description,
                Directory = (string)row.directory,
                RepoOwner = (string?)row.repo_owner,
                RepoName = (string?)row.repo_name,
                RepoBranch = (string?)row.repo_branch,
                ReadmeUrl = (string?)row.readme_url,
                Apps = new SkillAppsDto
                {
                    Claude = row.enabled_claude != null && (long)row.enabled_claude == 1,
                    Codex = row.enabled_codex != null && (long)row.enabled_codex == 1,
                    Grokbuild = row.enabled_grokbuild != null && (long)row.enabled_grokbuild == 1,
                    Opencode = row.enabled_opencode != null && (long)row.enabled_opencode == 1,
                },
                InstalledAt = row.installed_at != null ? (long)row.installed_at : 0,
                ContentHash = (string?)row.content_hash,
                UpdatedAt = row.updated_at != null ? (long)row.updated_at : 0
            };
            list.Add(dto);
        }
        return list;
    }
}
