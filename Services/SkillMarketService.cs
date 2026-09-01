using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;

namespace CCSwitch.Wpf.Services;

public class SkillMarketItemDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("repoOwner")] public string RepoOwner { get; set; } = "";
    [JsonPropertyName("repoName")] public string RepoName { get; set; } = "";
    [JsonPropertyName("readmeUrl")] public string? ReadmeUrl { get; set; }
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
    [JsonPropertyName("stars")] public int Stars { get; set; }
}

public class SkillMarketService
{
    private readonly DatabaseService _dbService;

    public SkillMarketService(DatabaseService dbService)
    {
        _dbService = dbService;
        InitializeTable();
    }

    private void InitializeTable()
    {
        using var conn = _dbService.CreateConnection();
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS skills (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                description TEXT,
                directory TEXT NOT NULL,
                repo_owner TEXT,
                repo_name TEXT,
                repo_branch TEXT,
                readme_url TEXT,
                enabled_claude INTEGER DEFAULT 1,
                enabled_codex INTEGER DEFAULT 1,
                enabled_grokbuild INTEGER DEFAULT 1,
                enabled_opencode INTEGER DEFAULT 1,
                installed_at INTEGER NOT NULL,
                content_hash TEXT,
                updated_at INTEGER NOT NULL
            );
        ");
    }

    public List<SkillMarketItemDto> DiscoverAvailableSkills()
    {
        return new List<SkillMarketItemDto>
        {
            new SkillMarketItemDto
            {
                Id = "agentic-git",
                Name = "Agentic Git Workflow",
                Description = "Advanced Git commit and workflow utilities for coding assistants",
                RepoOwner = "anthropics",
                RepoName = "claude-code-skills",
                Tags = new List<string> { "git", "productivity", "devops" },
                Stars = 480
            },
            new SkillMarketItemDto
            {
                Id = "frontend-ui-craft",
                Name = "Frontend UI Crafter",
                Description = "High quality Tailwind CSS, React, and Lucide component generators",
                RepoOwner = "shadcn",
                RepoName = "ui-skills",
                Tags = new List<string> { "react", "tailwind", "ui" },
                Stars = 1250
            },
            new SkillMarketItemDto
            {
                Id = "sql-db-inspector",
                Name = "SQL Database Inspector",
                Description = "Automated SQLite and PostgreSQL schema inspection and query tuning",
                RepoOwner = "sqlite",
                RepoName = "db-inspector",
                Tags = new List<string> { "sql", "database", "sqlite" },
                Stars = 320
            }
        };
    }

    public async Task<bool> InstallSkillAsync(string skillId, string name, string? description, string? repoOwner, string? repoName)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cc-switch", "skills", skillId);
        Directory.CreateDirectory(dir);

        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync(@"
            INSERT INTO skills (
                id, name, description, directory, repo_owner, repo_name, repo_branch,
                readme_url, enabled_claude, enabled_codex, enabled_grokbuild,
                enabled_opencode, installed_at, content_hash, updated_at
            ) VALUES (
                @skillId, @name, @description, @dir, @repoOwner, @repoName, 'main',
                '', 1, 1, 1, 1, @now, '', @now
            )
            ON CONFLICT(id) DO UPDATE SET
                name = @name,
                description = @description,
                updated_at = @now",
            new { skillId, name, description, dir, repoOwner, repoName, now }
        );

        return true;
    }

    public async Task<bool> UninstallSkillAsync(string skillId)
    {
        using var conn = _dbService.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM skills WHERE id = @skillId", new { skillId });
        return true;
    }

    public async Task<bool> ToggleSkillAppAsync(string skillId, string appType, bool enabled)
    {
        var column = appType.ToLowerInvariant() switch
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
            $"UPDATE skills SET {column} = @val, updated_at = @now WHERE id = @skillId",
            new { val = enabled ? 1 : 0, now = DateTimeOffset.UtcNow.ToUnixTimeSeconds(), skillId }
        );

        return true;
    }
}