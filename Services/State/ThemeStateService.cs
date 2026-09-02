using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CCSwitch.Wpf.Services.State;

public class ThemeStateService
{
    private readonly SettingsService? _settingsService;
    public bool IsDark { get; private set; } = true;

    public event Action? OnThemeChanged;

    public ThemeStateService(SettingsService? settingsService = null)
    {
        _settingsService = settingsService;
        InitializeThemeFromSettings();
    }

    private void InitializeThemeFromSettings()
    {
        if (_settingsService == null) return;
        try
        {
            var task = _settingsService.GetSettingsNodeAsync();
            var node = task.GetAwaiter().GetResult();
            if (node is JsonObject obj && obj.TryGetPropertyValue("theme", out var val) && val != null)
            {
                var themeStr = val.GetValue<string?>();
                if (!string.IsNullOrEmpty(themeStr))
                {
                    IsDark = !string.Equals(themeStr, "light", StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ThemeStateService] InitializeTheme failed: {ex.Message}");
        }
    }

    public void ToggleTheme()
    {
        SetTheme(!IsDark);
    }

    public void SetTheme(bool isDark)
    {
        if (IsDark != isDark)
        {
            IsDark = isDark;
            PersistTheme(isDark);
            OnThemeChanged?.Invoke();
        }
    }

    private void PersistTheme(bool isDark)
    {
        if (_settingsService == null) return;
        Task.Run(async () =>
        {
            try
            {
                var node = await _settingsService.GetSettingsNodeAsync();
                if (node is JsonObject obj)
                {
                    obj["theme"] = isDark ? "dark" : "light";
                    await _settingsService.SaveSettingsNodeAsync(obj);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeStateService] PersistTheme failed: {ex.Message}");
            }
        });
    }
}
