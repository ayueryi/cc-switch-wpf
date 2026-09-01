using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using CCSwitch.Wpf.Services;

namespace CCSwitch.Wpf.Bridge;

public class IpcDispatcher
{
    private readonly CoreWebView2 _webView;
    private readonly Dictionary<string, Func<JsonElement, Task<object?>>> _handlers = new(StringComparer.OrdinalIgnoreCase);

    public IpcDispatcher(CoreWebView2 webView)
    {
        _webView = webView;
    }

    public Action<string>? Logger { get; set; }

    public void Register(string cmd, Func<JsonElement, Task<object?>> handler)
    {
        _handlers[cmd] = handler;
    }

    public void Register(string cmd, Func<JsonElement, object?> handler)
    {
        _handlers[cmd] = args => Task.FromResult(handler(args));
    }

    public async Task HandleMessageAsync(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Handle potential double-serialized JSON string (e.g. from JSON.stringify)
            if (root.ValueKind == JsonValueKind.String)
            {
                var innerJson = root.GetString();
                if (!string.IsNullOrEmpty(innerJson))
                {
                    await HandleMessageAsync(innerJson);
                    return;
                }
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                Logger?.Invoke($"[WPF Bridge Warning] Unexpected message ValueKind: {root.ValueKind}");
                return;
            }

            if (root.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "console_log")
            {
                var level = root.TryGetProperty("level", out var l) ? l.GetString() : "log";
                var msg = root.TryGetProperty("message", out var m) ? (m.GetString() ?? "") : "";
                switch (level?.ToLowerInvariant())
                {
                    case "error":
                        LoggerService.Instance.Error("Frontend", msg);
                        break;
                    case "warn":
                        LoggerService.Instance.Warn("Frontend", msg);
                        break;
                    default:
                        LoggerService.Instance.Info("Frontend", msg);
                        break;
                }
                return;
            }

            var request = JsonSerializer.Deserialize<IpcRequest>(root.GetRawText());
            if (request == null || request.Type != "invoke") return;

            LoggerService.Instance.Debug("IPC", $"Invoke cmd={request.Cmd} id={request.Id}");

            IpcResponse response;
            if (_handlers.TryGetValue(request.Cmd, out var handler))
            {
                try
                {
                    var result = await handler(request.Args);
                    response = new IpcResponse
                    {
                        Id = request.Id,
                        Result = result,
                        Error = null
                    };
                }
                catch (Exception ex)
                {
                    LoggerService.Instance.Error("IPC", $"Invoke cmd={request.Cmd} failed: {ex.Message}", ex);
                    response = new IpcResponse
                    {
                        Id = request.Id,
                        Result = null,
                        Error = ex.Message
                    };
                }
            }
            else
            {
                // Fallback for unhandled/stubbed commands to avoid freezing the UI
                Logger?.Invoke($"[IPC Stub] cmd={request.Cmd}");
                System.Diagnostics.Debug.WriteLine($"[WPF Bridge] Unhandled command: {request.Cmd}");
                response = new IpcResponse
                {
                    Id = request.Id,
                    Result = GetDefaultStubResult(request.Cmd),
                    Error = null
                };
            }

            var responseJson = JsonSerializer.Serialize(response);
            _webView.PostWebMessageAsJson(responseJson);
        }
        catch (Exception ex)
        {
            Logger?.Invoke($"[WPF Bridge Error] Error processing message: {ex}");
            System.Diagnostics.Debug.WriteLine($"[WPF Bridge] Error processing message: {ex}");
        }
    }

    public void EmitEvent(string eventName, object? payload)
    {
        var ev = new IpcEvent
        {
            Event = eventName,
            Payload = payload
        };
        var json = JsonSerializer.Serialize(ev);
        _webView.PostWebMessageAsJson(json);
    }

    private static object? GetDefaultStubResult(string cmd)
    {
        return cmd switch
        {
            "get_init_error" => null,
            "get_current_provider" => "default",
            "get_providers" => new Dictionary<string, object>(),
            "get_proxy_status" => new { is_running = false, active_targets = Array.Empty<object>() },
            "get_proxy_takeover_status" => new Dictionary<string, bool>(),
            "get_auto_failover_enabled" => false,
            "get_failover_queue" => Array.Empty<object>(),
            "auth_get_antigravity_status" => new { accounts = Array.Empty<object>(), default_account_id = (string?)null, is_authenticated = false },
            "auth_get_status" => new { provider = "antigravity_oauth", authenticated = false, default_account_id = (string?)null, accounts = Array.Empty<object>() },
            "get_settings" => new { visibleApps = new Dictionary<string, bool> { ["claude"] = true, ["antigravity"] = true, ["codex"] = true, ["grokbuild"] = true, ["opencode"] = true }, theme = "dark" },
            "get_unmanaged_skills" => Array.Empty<object>(),
            "scan_unmanaged_skills" => Array.Empty<object>(),
            "get_models_dev_sync_config" => new { enabled = false, auto_sync = false, skipped = true },
            "check_all_env_conflicts" => new Dictionary<string, object>(),
            "check_env_conflicts" => Array.Empty<object>(),
            "get_env_conflicts" => Array.Empty<object>(),
            "get_profiles" => new { profiles = Array.Empty<object>(), active_id = (string?)null },
            "list_profiles" => new { profiles = Array.Empty<object>(), active_id = (string?)null },
            "get_migration_result" => new { migrated = false },
            "get_skills_migration_result" => new { migrated = false },
            "get_mcp_servers" => new Dictionary<string, object>(),
            "get_prompts" => new Dictionary<string, object>(),
            "get_skills" => Array.Empty<object>(),
            "get_installed_skills" => Array.Empty<object>(),
            "get_sessions" => Array.Empty<object>(),
            "get_usage_summary" => new { total_cost = 0.0, total_tokens = 0 },
            "get_coding_plan_quota" => new { total = 0, used = 0 },
            "get_subscription_quota" => new { total = 0, used = 0 },
            "get_balance" => new { balance = "0.00", currency = "USD" },
            "get_circuit_breaker_config" => new { enabled = false },
            "get_circuit_breaker_stats" => new { total_trips = 0 },
            "get_stream_check_config" => new { auto_check = false },
            "plugin:app|version" => "3.20.3",
            "plugin:updater|check" => null,
            "plugin:log|log" => null,
            "plugin:window|set_decorations" => null,
            "set_window_theme" => null,
            _ => new Dictionary<string, object>()
        };
    }
}