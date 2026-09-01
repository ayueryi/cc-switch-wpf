using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CCSwitch.Wpf.Services;

public class SessionMetaDto
{
    [JsonPropertyName("providerId")] public string ProviderId { get; set; } = "";
    [JsonPropertyName("sessionId")] public string SessionId { get; set; } = "";
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("summary")] public string? Summary { get; set; }
    [JsonPropertyName("projectDir")] public string? ProjectDir { get; set; }
    [JsonPropertyName("createdAt")] public long? CreatedAt { get; set; }
    [JsonPropertyName("lastActiveAt")] public long? LastActiveAt { get; set; }
    [JsonPropertyName("sourcePath")] public string? SourcePath { get; set; }
    [JsonPropertyName("resumeCommand")] public string? ResumeCommand { get; set; }
}

public class DeleteSessionRequestDto
{
    [JsonPropertyName("providerId")] public string ProviderId { get; set; } = "";
    [JsonPropertyName("sessionId")] public string SessionId { get; set; } = "";
    [JsonPropertyName("sourcePath")] public string SourcePath { get; set; } = "";
}

public class DeleteSessionOutcomeDto
{
    [JsonPropertyName("providerId")] public string ProviderId { get; set; } = "";
    [JsonPropertyName("sessionId")] public string SessionId { get; set; } = "";
    [JsonPropertyName("sourcePath")] public string SourcePath { get; set; } = "";
    [JsonPropertyName("success")] public bool Success { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
}

public class SessionMessageDto
{
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("content")] public string Content { get; set; } = "";
    [JsonPropertyName("ts")] public long? Ts { get; set; }
}

public class SessionService
{
    private static readonly string HomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public Task<List<SessionMetaDto>> ScanSessionsAsync()
    {
        var list = new List<SessionMetaDto>();

        try
        {
            ScanClaudeSessions(list);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SessionService] Error scanning Claude sessions: {ex.Message}");
        }

        try
        {
            ScanCodexSessions(list);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SessionService] Error scanning Codex sessions: {ex.Message}");
        }

        // Sort by last active / created descending
        list.Sort((a, b) => (b.LastActiveAt ?? b.CreatedAt ?? 0).CompareTo(a.LastActiveAt ?? a.CreatedAt ?? 0));
        return Task.FromResult(list);
    }

    private void ScanClaudeSessions(List<SessionMetaDto> list)
    {
        var projectsDir = Path.Combine(HomeDir, ".claude", "projects");
        if (!Directory.Exists(projectsDir)) return;

        var files = Directory.GetFiles(projectsDir, "*.jsonl", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            try
            {
                var fi = new FileInfo(file);
                var sessionId = Path.GetFileNameWithoutExtension(file);
                var projectDir = fi.Directory?.Name ?? "";

                string? title = null;
                string? summary = null;
                long? createdAt = fi.CreationTimeUtc.Ticks > 0 ? new DateTimeOffset(fi.CreationTimeUtc).ToUnixTimeMilliseconds() : null;
                long? lastActiveAt = fi.LastWriteTimeUtc.Ticks > 0 ? new DateTimeOffset(fi.LastWriteTimeUtc).ToUnixTimeMilliseconds() : null;

                // Read first non-meta line for title preview
                using var reader = new StreamReader(file);
                string? line;
                int readCount = 0;
                while ((line = reader.ReadLine()) != null && readCount < 10)
                {
                    readCount++;
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("isMeta", out var meta) && meta.GetBoolean()) continue;
                        if (root.TryGetProperty("message", out var msg))
                        {
                            if (msg.TryGetProperty("content", out var contentProp))
                            {
                                var text = contentProp.ValueKind == JsonValueKind.String ? contentProp.GetString() : contentProp.ToString();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    title ??= text.Length > 80 ? text.Substring(0, 77) + "..." : text;
                                    summary = text;
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                }

                list.Add(new SessionMetaDto
                {
                    ProviderId = "claude",
                    SessionId = sessionId,
                    Title = title ?? $"Claude Session {sessionId.Substring(0, Math.Min(8, sessionId.Length))}",
                    Summary = summary,
                    ProjectDir = projectDir,
                    CreatedAt = createdAt,
                    LastActiveAt = lastActiveAt,
                    SourcePath = file,
                    ResumeCommand = $"claude resume {sessionId}"
                });
            }
            catch { }
        }
    }

    private void ScanCodexSessions(List<SessionMetaDto> list)
    {
        var codexDir = Path.Combine(HomeDir, ".codex");
        if (!Directory.Exists(codexDir)) return;

        var searchDirs = new[]
        {
            Path.Combine(codexDir, "sessions"),
            Path.Combine(codexDir, "archived_sessions"),
            codexDir
        };

        foreach (var dir in searchDirs)
        {
            if (!Directory.Exists(dir)) continue;

            var files = Directory.GetFiles(dir, "*.jsonl", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                try
                {
                    var fi = new FileInfo(file);
                    var sessionId = Path.GetFileNameWithoutExtension(file);

                    string? title = null;
                    long? createdAt = fi.CreationTimeUtc.Ticks > 0 ? new DateTimeOffset(fi.CreationTimeUtc).ToUnixTimeMilliseconds() : null;
                    long? lastActiveAt = fi.LastWriteTimeUtc.Ticks > 0 ? new DateTimeOffset(fi.LastWriteTimeUtc).ToUnixTimeMilliseconds() : null;

                    using var reader = new StreamReader(file);
                    string? line;
                    int count = 0;
                    while ((line = reader.ReadLine()) != null && count < 10)
                    {
                        count++;
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try
                        {
                            using var doc = JsonDocument.Parse(line);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("content", out var content))
                            {
                                var text = content.ValueKind == JsonValueKind.String ? content.GetString() : content.ToString();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    title = text.Length > 80 ? text.Substring(0, 77) + "..." : text;
                                    break;
                                }
                            }
                        }
                        catch { }
                    }

                    list.Add(new SessionMetaDto
                    {
                        ProviderId = "codex",
                        SessionId = sessionId,
                        Title = title ?? $"Codex Session {sessionId.Substring(0, Math.Min(8, sessionId.Length))}",
                        CreatedAt = createdAt,
                        LastActiveAt = lastActiveAt,
                        SourcePath = file,
                        ResumeCommand = $"codex resume {sessionId}"
                    });
                }
                catch { }
            }
        }
    }

    public Task<List<SessionMessageDto>> GetSessionMessagesAsync(string providerId, string sourcePath)
    {
        return GetSessionMessagesAsync(sourcePath);
    }

    public async Task<List<SessionMessageDto>> GetSessionMessagesAsync(string sourcePath)
    {
        var messages = new List<SessionMessageDto>();
        if (!File.Exists(sourcePath)) return messages;

        using var reader = new StreamReader(sourcePath);
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;

                if (root.TryGetProperty("isMeta", out var meta) && meta.GetBoolean()) continue;

                string role = "user";
                string content = "";
                long? ts = null;

                if (root.TryGetProperty("timestamp", out var tProp))
                {
                    ts = tProp.GetInt64();
                }

                if (root.TryGetProperty("message", out var msg))
                {
                    if (msg.TryGetProperty("role", out var r)) role = r.GetString() ?? "user";
                    if (msg.TryGetProperty("content", out var c))
                    {
                        content = c.ValueKind == JsonValueKind.String ? c.GetString() ?? "" : c.ToString();
                    }
                }
                else if (root.TryGetProperty("role", out var r2))
                {
                    role = r2.GetString() ?? "user";
                    if (root.TryGetProperty("content", out var c2))
                    {
                        content = c2.ValueKind == JsonValueKind.String ? c2.GetString() ?? "" : c2.ToString();
                    }
                }

                if (!string.IsNullOrEmpty(content))
                {
                    messages.Add(new SessionMessageDto
                    {
                        Role = role,
                        Content = content,
                        Ts = ts
                    });
                }
            }
            catch { }
        }

        return messages;
    }

    public bool DeleteSession(string providerId, string sessionId, string sourcePath)
    {
        return DeleteSession(sourcePath);
    }

    public List<DeleteSessionOutcomeDto> DeleteSessions(List<DeleteSessionRequestDto> items)
    {
        var results = new List<DeleteSessionOutcomeDto>();
        foreach (var item in items)
        {
            try
            {
                var ok = DeleteSession(item.ProviderId, item.SessionId, item.SourcePath);
                results.Add(new DeleteSessionOutcomeDto
                {
                    ProviderId = item.ProviderId,
                    SessionId = item.SessionId,
                    SourcePath = item.SourcePath,
                    Success = ok,
                    Error = ok ? null : "File not found or delete failed"
                });
            }
            catch (Exception ex)
            {
                results.Add(new DeleteSessionOutcomeDto
                {
                    ProviderId = item.ProviderId,
                    SessionId = item.SessionId,
                    SourcePath = item.SourcePath,
                    Success = false,
                    Error = ex.Message
                });
            }
        }
        return results;
    }

    public bool DeleteSession(string sourcePath)
    {
        try
        {
            if (File.Exists(sourcePath))
            {
                File.Delete(sourcePath);
                return true;
            }
        }
        catch { }
        return false;
    }

    public bool LaunchSessionTerminal(string command, string? cwd, string? customConfig = null)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -Command \"{command}\"",
                WorkingDirectory = !string.IsNullOrEmpty(cwd) && Directory.Exists(cwd) ? cwd : HomeDir,
                UseShellExecute = true
            };
            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SessionService] Failed to launch terminal: {ex.Message}");
            return false;
        }
    }
}
