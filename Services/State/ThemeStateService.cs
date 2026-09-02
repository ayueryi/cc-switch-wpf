namespace CCSwitch.Wpf.Services.State;

public class ThemeStateService
{
    public bool IsDark { get; private set; } = true;

    public event Action? OnThemeChanged;

    public void ToggleTheme()
    {
        IsDark = !IsDark;
        OnThemeChanged?.Invoke();
    }

    public void SetTheme(bool isDark)
    {
        if (IsDark != isDark)
        {
            IsDark = isDark;
            OnThemeChanged?.Invoke();
        }
    }
}
