using System.Threading.Tasks;

namespace CCSwitch.Wpf.Services;

public class LiveImportService
{
    private readonly ProviderService _providerService;
    private readonly McpService _mcpService;

    public LiveImportService(ProviderService providerService, McpService mcpService)
    {
        _providerService = providerService;
        _mcpService = mcpService;
    }

        public Task<int> ImportMcpFromAllAppsAsync() => Task.FromResult(0);
}