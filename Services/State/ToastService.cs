using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCSwitch.Wpf.Services.State;

public enum ToastType
{
    Success,
    Error,
    Info,
    Warning
}

public class ToastItem
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; } = ToastType.Info;
    public int DurationMs { get; set; } = 3000;
    public DateTime CreatedAt { get; } = DateTime.Now;
    public bool IsClosing { get; set; } = false;
}

public class ToastService
{
    private readonly List<ToastItem> _toasts = new();
    private readonly object _lock = new();

    public event Action? OnToastsChanged;

    public IReadOnlyList<ToastItem> GetToasts()
    {
        lock (_lock)
        {
            return _toasts.ToList();
        }
    }

    public void Show(string title, string message = "", ToastType type = ToastType.Info, int durationMs = 3000)
    {
        var item = new ToastItem
        {
            Title = title,
            Message = message,
            Type = type,
            DurationMs = durationMs
        };

        lock (_lock)
        {
            _toasts.Add(item);
            if (_toasts.Count > 5)
            {
                _toasts.RemoveAt(0);
            }
        }

        NotifyChanged();

        if (durationMs > 0)
        {
            _ = ScheduleAutoDismissAsync(item.Id, durationMs);
        }
    }

    public void ShowSuccess(string title, string message = "", int durationMs = 3000)
        => Show(title, message, ToastType.Success, durationMs);

    public void ShowError(string title, string message = "", int durationMs = 4500)
        => Show(title, message, ToastType.Error, durationMs);

    public void ShowInfo(string title, string message = "", int durationMs = 3000)
        => Show(title, message, ToastType.Info, durationMs);

    public void ShowWarning(string title, string message = "", int durationMs = 3500)
        => Show(title, message, ToastType.Warning, durationMs);

    public async Task DismissAsync(string id)
    {
        ToastItem? item;
        lock (_lock)
        {
            item = _toasts.FirstOrDefault(t => t.Id == id);
            if (item == null || item.IsClosing) return;
            item.IsClosing = true;
        }

        NotifyChanged();

        await Task.Delay(200);

        lock (_lock)
        {
            _toasts.RemoveAll(t => t.Id == id);
        }

        NotifyChanged();
    }

    private async Task ScheduleAutoDismissAsync(string id, int delayMs)
    {
        try
        {
            await Task.Delay(delayMs);
            await DismissAsync(id);
        }
        catch
        {
            // Ignored
        }
    }

    private void NotifyChanged()
    {
        try
        {
            OnToastsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ToastService] Error: {ex.Message}");
        }
    }
}
