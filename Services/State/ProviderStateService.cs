using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CCSwitch.Wpf.Services.State;

public class ProviderStateService
{
    private readonly ProviderService _providerService;
    private readonly AppStateService _appStateService;

    public List<ProviderDto> Providers { get; private set; } = new();
    public string CurrentProviderId { get; private set; } = "";
    public bool IsLoading { get; private set; } = false;

    public event Action? OnProvidersChanged;

    public ProviderStateService(ProviderService providerService, AppStateService appStateService)
    {
        _providerService = providerService;
        _appStateService = appStateService;
        _appStateService.OnStateChanged += async () => await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        IsLoading = true;
        OnProvidersChanged?.Invoke();

        try
        {
            var app = _appStateService.CurrentApp;
            var dict = await _providerService.GetProvidersAsync(app);
            Providers = dict.Values.OfType<ProviderDto>().ToList();
            CurrentProviderId = await _providerService.GetCurrentProviderAsync(app) ?? "";
        }
        catch
        {
            Providers = new List<ProviderDto>();
            CurrentProviderId = "";
        }
        finally
        {
            IsLoading = false;
            OnProvidersChanged?.Invoke();
        }
    }

    public async Task<bool> SwitchProviderAsync(string id)
    {
        var app = _appStateService.CurrentApp;
        try
        {
            await _providerService.SwitchProviderAsync(app, id);
            CurrentProviderId = id;
            OnProvidersChanged?.Invoke();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AddProviderAsync(string name, string category, JsonElement config, string notes, string website)
    {
        var app = _appStateService.CurrentApp;
        var dto = new ProviderDto
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Category = category,
            SettingsConfig = config,
            Notes = notes,
            WebsiteUrl = website,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        var ok = await _providerService.AddProviderAsync(app, dto);
        if (ok)
        {
            await ReloadAsync();
        }
        return ok;
    }

    public async Task<bool> UpdateProviderAsync(string id, string name, string category, JsonElement config, string notes, string website)
    {
        var app = _appStateService.CurrentApp;
        var dto = new ProviderDto
        {
            Id = id,
            Name = name,
            Category = category,
            SettingsConfig = config,
            Notes = notes,
            WebsiteUrl = website
        };

        var ok = await _providerService.UpdateProviderAsync(app, dto);
        if (ok)
        {
            await ReloadAsync();
        }
        return ok;
    }

    public async Task<bool> DeleteProviderAsync(string id)
    {
        var app = _appStateService.CurrentApp;
        try
        {
            var ok = await _providerService.DeleteProviderAsync(app, id);
            if (ok)
            {
                await ReloadAsync();
            }
            return ok;
        }
        catch
        {
            return false;
        }
    }
}
