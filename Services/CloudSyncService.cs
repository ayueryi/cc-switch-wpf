using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;

namespace CCSwitch.Wpf.Services;

public class DbBackupDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("createdAt")] public long CreatedAt { get; set; }
    [JsonPropertyName("note")] public string? Note { get; set; }
}

public class CloudSyncService
{
    private readonly DatabaseService _dbService;
    private readonly ProviderService _providerService;
    private readonly McpService _mcpService;
    private readonly HttpClient _httpClient;
    private readonly string _backupsDir;
    private readonly string _dbPath;

    public CloudSyncService(DatabaseService dbService, ProviderService providerService, McpService mcpService)
    {
        _dbService = dbService;
        _providerService = providerService;
        _mcpService = mcpService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cc-switch");
        _dbPath = Path.Combine(appData, "cc-switch.db");
        _backupsDir = Path.Combine(appData, "backups");
        Directory.CreateDirectory(_backupsDir);
    }

    // 1. WebDAV Sync
    public async Task<bool> TestWebDavConnectionAsync(string url, string username, string password)
    {
        try
        {
            using var req = new HttpRequestMessage(new HttpMethod("PROPFIND"), url);
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            req.Headers.Add("Depth", "0");

            var resp = await _httpClient.SendAsync(req);
            return resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.MultiStatus;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> WebDavUploadAsync(string url, string username, string password, string content)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var resp = await _httpClient.SendAsync(req);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> WebDavDownloadAsync(string url, string username, string password)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);

            var resp = await _httpClient.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                return await resp.Content.ReadAsStringAsync();
            }
        }
        catch { }
        return null;
    }

    // 2. Database Local Backups
    public List<DbBackupDto> ListBackups()
    {
        var list = new List<DbBackupDto>();
        if (!Directory.Exists(_backupsDir)) return list;

        var files = Directory.GetFiles(_backupsDir, "*.db");
        foreach (var file in files)
        {
            var fi = new FileInfo(file);
            var id = Path.GetFileNameWithoutExtension(file);
            list.Add(new DbBackupDto
            {
                Id = id,
                Name = id,
                Size = fi.Length,
                CreatedAt = new DateTimeOffset(fi.CreationTimeUtc).ToUnixTimeSeconds(),
                Note = "Manual Backup"
            });
        }
        return list.OrderByDescending(b => b.CreatedAt).ToList();
    }

    public DbBackupDto CreateBackup(string? name, string? note)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupName = string.IsNullOrWhiteSpace(name) ? $"backup_{timestamp}" : name;
        var targetFile = Path.Combine(_backupsDir, $"{backupName}.db");

        if (File.Exists(_dbPath))
        {
            File.Copy(_dbPath, targetFile, true);
        }

        var fi = new FileInfo(targetFile);
        return new DbBackupDto
        {
            Id = backupName,
            Name = backupName,
            Size = fi.Exists ? fi.Length : 0,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Note = note
        };
    }

    public bool RestoreBackup(string backupId)
    {
        var srcFile = Path.Combine(_backupsDir, $"{backupId}.db");
        if (!File.Exists(srcFile)) return false;

        try
        {
            File.Copy(srcFile, _dbPath, true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool DeleteBackup(string backupId)
    {
        var target = Path.Combine(_backupsDir, $"{backupId}.db");
        if (File.Exists(target))
        {
            File.Delete(target);
            return true;
        }
        return false;
    }

    public bool RenameBackup(string backupId, string newName)
    {
        var src = Path.Combine(_backupsDir, $"{backupId}.db");
        var dst = Path.Combine(_backupsDir, $"{newName}.db");
        if (File.Exists(src) && !File.Exists(dst))
        {
            File.Move(src, dst);
            return true;
        }
        return false;
    }

    // 3. DeepLink Parser
    public object ParseDeepLink(string uri)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(uri)) return new { valid = false, error = "Empty URI" };

            // e.g. ccswitch://import?data=base64...
            var u = new Uri(uri);
            var query = u.Query.TrimStart('?');
            var parts = query.Split('&');
            foreach (var part in parts)
            {
                var kv = part.Split('=');
                if (kv.Length == 2 && kv[0].ToLowerInvariant() == "data")
                {
                    var raw = Uri.UnescapeDataString(kv[1]);
                    var jsonBytes = Convert.FromBase64String(raw);
                    var json = Encoding.UTF8.GetString(jsonBytes);
                    var doc = JsonDocument.Parse(json);
                    return new { valid = true, data = doc.RootElement.Clone() };
                }
            }
        }
        catch (Exception ex)
        {
            return new { valid = false, error = ex.Message };
        }
        return new { valid = false, error = "Unsupported scheme or missing data param" };
    }

    public async Task<bool> ImportFromDeepLinkAsync(JsonElement payload)
    {
        try
        {
            if (payload.TryGetProperty("providers", out var providersArr) && providersArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in providersArr.EnumerateArray())
                {
                    var app = p.TryGetProperty("appType", out var at) ? at.GetString() ?? "claude" : "claude";
                    var dto = JsonSerializer.Deserialize<ProviderDto>(p.GetRawText());
                    if (dto != null)
                    {
                        await _providerService.SaveProviderAsync(app, dto);
                    }
                }
            }

            if (payload.TryGetProperty("mcpServers", out var mcpArr) && mcpArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var m in mcpArr.EnumerateArray())
                {
                    var dto = JsonSerializer.Deserialize<McpServerDto>(m.GetRawText());
                    if (dto != null)
                    {
                        await _mcpService.UpsertServerAsync(dto);
                    }
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}