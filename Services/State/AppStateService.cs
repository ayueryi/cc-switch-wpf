using System;

namespace CCSwitch.Wpf.Services.State;

public class AppStateService
{
    public string CurrentApp { get; private set; } = "claude";
    public string CurrentView { get; private set; } = "providers";
    public string SearchQuery { get; private set; } = string.Empty;
    public string? CurrentProfileId { get; private set; }
    public string? CurrentProfileName { get; private set; }

    public event Action? OnStateChanged;
    public event Action? OnOpenAddProviderModalRequested;

    public void SetCurrentApp(string app)
    {
        if (CurrentApp != app)
        {
            CurrentApp = app;
            OnStateChanged?.Invoke();
        }
    }

    public void SetCurrentView(string view)
    {
        if (CurrentView != view)
        {
            CurrentView = view;
            OnStateChanged?.Invoke();
        }
    }

    public void SetSearchQuery(string query)
    {
        if (SearchQuery != query)
        {
            SearchQuery = query;
            OnStateChanged?.Invoke();
        }
    }

    public void SetCurrentProfile(string? id, string? name)
    {
        CurrentProfileId = id;
        CurrentProfileName = name;
        OnStateChanged?.Invoke();
    }

    public void RequestOpenAddProviderModal()
    {
        OnOpenAddProviderModalRequested?.Invoke();
    }
}
