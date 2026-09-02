namespace CCSwitch.Wpf.Services.State;

public class McpStateService
{
    private readonly McpService _mcpService;

    public List<McpServerDto> Servers { get; private set; } = new();
    public bool IsLoading { get; private set; } = false;

    public event Action? OnMcpChanged;

    public McpStateService(McpService mcpService)
    {
        _mcpService = mcpService;
    }

    public async Task ReloadAsync()
    {
        IsLoading = true;
        OnMcpChanged?.Invoke();

        try
        {
            var dict = await _mcpService.GetAllServersAsync();
            Servers = dict.Values.ToList();
        }
        catch
        {
            Servers = new List<McpServerDto>();
        }
        finally
        {
            IsLoading = false;
            OnMcpChanged?.Invoke();
        }
    }

    public async Task<bool> ToggleAppAsync(string serverId, string app, bool enabled)
    {
        var res = await _mcpService.ToggleMcpAppAsync(serverId, app, enabled);
        if (res)
        {
            var server = Servers.FirstOrDefault(s => s.Id == serverId);
            if (server != null)
            {
                switch (app.ToLowerInvariant())
                {
                    case "claude": server.Apps.Claude = enabled; break;
                    case "codex": server.Apps.Codex = enabled; break;
                    case "opencode": server.Apps.Opencode = enabled; break;
                    case "grokbuild": server.Apps.Grokbuild = enabled; break;
                }
                OnMcpChanged?.Invoke();
            }
        }
        return res;
    }

    public async Task<bool> SaveServerAsync(McpServerDto server)
    {
        var res = await _mcpService.UpsertServerAsync(server);
        if (res)
        {
            await ReloadAsync();
        }
        return res;
    }

    public async Task<bool> DeleteServerAsync(string id)
    {
        var res = await _mcpService.DeleteServerAsync(id);
        if (res)
        {
            await ReloadAsync();
        }
        return res;
    }
}
