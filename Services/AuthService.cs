using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CCSwitch.Wpf.Services;

public class ManagedAuthAccountDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("provider")] public string Provider { get; set; } = "antigravity_oauth";
    [JsonPropertyName("login")] public string Login { get; set; } = "";
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("avatar_url")] public string? AvatarUrl { get; set; }
    [JsonPropertyName("authenticated_at")] public long AuthenticatedAt { get; set; }
    [JsonPropertyName("is_default")] public bool IsDefault { get; set; }
    [JsonPropertyName("github_domain")] public string GithubDomain { get; set; } = "";
    [JsonPropertyName("requires_reauth")] public bool RequiresReauth { get; set; }
}

public class ManagedAuthStatusDto
{
    [JsonPropertyName("provider")] public string Provider { get; set; } = "antigravity_oauth";
    [JsonPropertyName("authenticated")] public bool Authenticated { get; set; }
    [JsonPropertyName("default_account_id")] public string? DefaultAccountId { get; set; }
    [JsonPropertyName("accounts")] public List<ManagedAuthAccountDto> Accounts { get; set; } = new();
}

public class AuthService
{
    private readonly string _antigravityAuthPath;

    public AuthService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cc-switch"
        );
        _antigravityAuthPath = Path.Combine(appData, "antigravity_oauth_auth.json");
    }

    public async Task<ManagedAuthStatusDto> GetAntigravityAuthStatusAsync()
    {
        var status = new ManagedAuthStatusDto
        {
            Provider = "antigravity_oauth",
            Authenticated = false,
            DefaultAccountId = null,
            Accounts = new List<ManagedAuthAccountDto>()
        };

        if (File.Exists(_antigravityAuthPath))
        {
            try
            {
                var text = await File.ReadAllTextAsync(_antigravityAuthPath);
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;

                string? defaultAccountId = root.TryGetProperty("default_account_id", out var def) ? def.GetString() : null;
                status.DefaultAccountId = defaultAccountId;

                if (root.TryGetProperty("accounts", out var accs) && accs.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in accs.EnumerateObject())
                    {
                        var accObj = prop.Value;
                        var id = prop.Name;
                        var login = accObj.TryGetProperty("login", out var l) ? l.GetString() ?? "" : "";
                        var name = accObj.TryGetProperty("name", out var n) ? n.GetString() : null;
                        var avatarUrl = accObj.TryGetProperty("avatar_url", out var a) ? a.GetString() : null;
                        var authAt = accObj.TryGetProperty("authenticated_at", out var at) ? at.GetInt64() : 0;
                        var reqReauth = accObj.TryGetProperty("requires_reauth", out var r) && r.GetBoolean();

                        status.Accounts.Add(new ManagedAuthAccountDto
                        {
                            Id = id,
                            Provider = "antigravity_oauth",
                            Login = login,
                            Name = name,
                            AvatarUrl = avatarUrl,
                            AuthenticatedAt = authAt,
                            IsDefault = id == defaultAccountId,
                            RequiresReauth = reqReauth
                        });
                    }
                }

                status.Authenticated = status.Accounts.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthService] Parse antigravity auth failed: {ex.Message}");
            }
        }

        return status;
    }
}
