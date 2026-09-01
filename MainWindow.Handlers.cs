using System.IO;
using System.Text.Json;
using System.Windows;
using CCSwitch.Wpf.Bridge;
using CCSwitch.Wpf.Services;
using WpfApplication = System.Windows.Application;

namespace CCSwitch.Wpf;

public partial class MainWindow
{
    private void RegisterAllIpcHandlers(IpcDispatcher dispatcher)
    {
        RegisterCoreHandlers(dispatcher);
        RegisterProfileAndStreamHandlers(dispatcher);
        RegisterCloudAndSkillHandlers(dispatcher);
        RegisterSystemAndWindowHandlers(dispatcher);
    }

    private void RegisterCoreHandlers(IpcDispatcher dispatcher)
    {
        // 1. Settings
        dispatcher.Register("get_settings", async args => await _settingsService.GetSettingsAsync());
        dispatcher.Register("save_settings", async args => await _settingsService.SaveSettingsAsync(args.GetProperty("settings")));

        // 2. Providers
        dispatcher.Register("get_providers", async args =>
        {
            var app = args.TryGetProperty("app", out var a) ? a.GetString() ?? "claude" : "claude";
            return await _providerService.GetProvidersAsync(app);
        });
        dispatcher.Register("get_current_provider", async args =>
        {
            var app = args.TryGetProperty("app", out var a) ? a.GetString() ?? "claude" : "claude";
            return await _providerService.GetCurrentProviderIdAsync(app);
        });
        dispatcher.Register("get_current_provider_id", async args =>
        {
            var app = args.TryGetProperty("app", out var a) ? a.GetString() ?? "claude" : "claude";
            return await _providerService.GetCurrentProviderIdAsync(app);
        });
        dispatcher.Register("set_current_provider", async args =>
        {
            var app = args.TryGetProperty("app", out var a) ? a.GetString() ?? "claude" : "claude";
            var id = args.GetProperty("id").GetString() ?? "";
            var res = await _providerService.SetCurrentProviderAsync(app, id);
            _ = _trayService?.RefreshMenuAsync();
            return res;
        });
        dispatcher.Register("save_provider", async args =>
        {
            var app = args.TryGetProperty("app", out var a) ? a.GetString() ?? "claude" : "claude";
            var json = args.GetProperty("provider").GetRawText();
            var provider = JsonSerializer.Deserialize<ProviderDto>(json);
            if (provider != null)
            {
                var res = await _providerService.SaveProviderAsync(app, provider);
                _ = _trayService?.RefreshMenuAsync();
                return res;
            }
            return false;
        });
        dispatcher.Register("delete_provider", async args =>
        {
            var app = args.TryGetProperty("app", out var a) ? a.GetString() ?? "claude" : "claude";
            var id = args.GetProperty("id").GetString() ?? "";
            var res = await _providerService.DeleteProviderAsync(app, id);
            _ = _trayService?.RefreshMenuAsync();
            return res;
        });
        dispatcher.Register("set_provider_sort_order", async args =>
        {
            var app = args.TryGetProperty("app", out var a) ? a.GetString() ?? "claude" : "claude";
            var ids = JsonSerializer.Deserialize<List<string>>(args.GetProperty("ids").GetRawText()) ?? new();
            return await _providerService.SetProviderSortOrderAsync(app, ids);
        });
        dispatcher.Register("update_sort_order", async args =>
        {
            var app = args.TryGetProperty("app", out var a) ? a.GetString() ?? "claude" : "claude";
            var ids = JsonSerializer.Deserialize<List<string>>(args.GetProperty("order").GetRawText()) ?? new();
            return await _providerService.SetProviderSortOrderAsync(app, ids);
        });

        // 3. MCP Servers
        dispatcher.Register("get_mcp_servers", async args => await _mcpService.GetAllServersAsync());
        dispatcher.Register("upsert_mcp_server", async args =>
        {
            var json = args.GetProperty("server").GetRawText();
            var server = JsonSerializer.Deserialize<McpServerDto>(json);
            if (server != null) return await _mcpService.UpsertServerAsync(server);
            return false;
        });
        dispatcher.Register("delete_mcp_server", async args =>
        {
            var id = args.GetProperty("id").GetString() ?? "";
            return await _mcpService.DeleteServerAsync(id);
        });
        dispatcher.Register("toggle_mcp_app", async args =>
        {
            var id = args.GetProperty("id").GetString() ?? "";
            var app = args.GetProperty("app").GetString() ?? "claude";
            var enabled = args.GetProperty("enabled").GetBoolean();
            return await _mcpService.ToggleMcpAppAsync(id, app, enabled);
        });

        // 4. Prompts
        dispatcher.Register("get_prompts", async args =>
        {
            var app = args.TryGetProperty("app", out var a) ? a.GetString() ?? "claude" : "claude";
            return await _promptService.GetPromptsAsync(app);
        });
        dispatcher.Register("upsert_prompt", async args =>
        {
            var app = args.TryGetProperty("app", out var a) ? a.GetString() ?? "claude" : "claude";
            var json = args.GetProperty("prompt").GetRawText();
            var prompt = JsonSerializer.Deserialize<PromptDto>(json);
            if (prompt != null) return await _promptService.UpsertPromptAsync(app, prompt);
            return false;
        });
        dispatcher.Register("delete_prompt", async args =>
        {
            var app = args.TryGetProperty("app", out var a) ? a.GetString() ?? "claude" : "claude";
            var id = args.GetProperty("id").GetString() ?? "";
            return await _promptService.DeletePromptAsync(app, id);
        });
        dispatcher.Register("enable_prompt", async args =>
        {
            var app = args.TryGetProperty("app", out var a) ? a.GetString() ?? "claude" : "claude";
            var id = args.GetProperty("id").GetString() ?? "";
            var enabled = args.TryGetProperty("enabled", out var e) && e.GetBoolean();
            return await _promptService.EnablePromptAsync(app, id, enabled);
        });
    }

    private void RegisterProfileAndStreamHandlers(IpcDispatcher dispatcher)
    {
        // Profiles
        dispatcher.Register("list_profiles", async args => await _profileService.ListProfilesAsync());
        dispatcher.Register("get_profiles", async args => await _profileService.ListProfilesAsync());
        dispatcher.Register("create_profile", async args =>
        {
            var name = args.GetProperty("name").GetString() ?? "New Profile";
            var desc = args.TryGetProperty("description", out var d) ? d.GetString() : null;
            return await _profileService.CreateProfileAsync(name, desc);
        });
        dispatcher.Register("apply_profile", async args =>
        {
            var id = args.GetProperty("id").GetString() ?? "";
            return await _profileService.ApplyProfileAsync(id);
        });
        dispatcher.Register("update_profile", async args =>
        {
            var id = args.GetProperty("id").GetString() ?? "";
            var name = args.GetProperty("name").GetString() ?? "";
            var desc = args.TryGetProperty("description", out var d) ? d.GetString() : null;
            return await _profileService.UpdateProfileAsync(id, name, desc);
        });
        dispatcher.Register("delete_profile", async args =>
        {
            var id = args.GetProperty("id").GetString() ?? "";
            return await _profileService.DeleteProfileAsync(id);
        });
        dispatcher.Register("clear_current_profile", async args => await _profileService.ClearCurrentProfileAsync());

        // Stream Check & Failover
        dispatcher.Register("stream_check_provider", async args =>
        {
            var app = args.TryGetProperty("appType", out var a) ? a.GetString() ?? "claude" : "claude";
            var pId = args.GetProperty("providerId").GetString() ?? "";
            return await _streamService.CheckProviderAsync(app, pId);
        });
        dispatcher.Register("stream_check_all_providers", async args =>
        {
            var app = args.TryGetProperty("appType", out var a) ? a.GetString() ?? "claude" : "claude";
            return await _streamService.CheckAllProvidersAsync(app);
        });
        dispatcher.Register("get_stream_check_config", async args => await _streamService.GetStreamCheckConfigAsync());
        dispatcher.Register("save_stream_check_config", async args => await _streamService.SaveStreamCheckConfigAsync(args.GetProperty("config")));
        dispatcher.Register("get_auto_failover_enabled", async args => await _streamService.GetAutoFailoverEnabledAsync());
        dispatcher.Register("set_auto_failover_enabled", async args => await _streamService.SetAutoFailoverEnabledAsync(args.GetProperty("enabled").GetBoolean()));
        dispatcher.Register("get_failover_queue", async args =>
        {
            var app = args.TryGetProperty("appType", out var a) ? a.GetString() ?? "claude" : "claude";
            return await _streamService.GetFailoverQueueAsync(app);
        });
        dispatcher.Register("add_to_failover_queue", async args =>
        {
            var app = args.TryGetProperty("appType", out var a) ? a.GetString() ?? "claude" : "claude";
            var pId = args.GetProperty("providerId").GetString() ?? "";
            return await _streamService.AddToFailoverQueueAsync(app, pId);
        });
        dispatcher.Register("remove_from_failover_queue", async args =>
        {
            var app = args.TryGetProperty("appType", out var a) ? a.GetString() ?? "claude" : "claude";
            var pId = args.GetProperty("providerId").GetString() ?? "";
            return await _streamService.RemoveFromFailoverQueueAsync(app, pId);
        });
        dispatcher.Register("get_circuit_breaker_config", args => _streamService.GetCircuitBreakerConfig());
        dispatcher.Register("get_circuit_breaker_stats", args => _streamService.GetCircuitBreakerStats());
        dispatcher.Register("reset_circuit_breaker", args => true);
        dispatcher.Register("update_circuit_breaker_config", args => true);
    }

    private void RegisterCloudAndSkillHandlers(IpcDispatcher dispatcher)
    {
        // Skills
        dispatcher.Register("get_skills", async args => await _skillService.GetInstalledSkillsAsync());
        dispatcher.Register("get_installed_skills", async args => await _skillService.GetInstalledSkillsAsync());
        dispatcher.Register("discover_available_skills", args => _skillMarketService.DiscoverAvailableSkills());
        dispatcher.Register("search_skills_sh", args => _skillMarketService.DiscoverAvailableSkills());
        dispatcher.Register("install_skill", async args =>
        {
            var id = args.GetProperty("id").GetString() ?? "";
            var name = args.TryGetProperty("name", out var n) ? n.GetString() ?? id : id;
            var desc = args.TryGetProperty("description", out var d) ? d.GetString() : null;
            var ro = args.TryGetProperty("repoOwner", out var o) ? o.GetString() : null;
            var rn = args.TryGetProperty("repoName", out var r) ? r.GetString() : null;
            return await _skillMarketService.InstallSkillAsync(id, name, desc, ro, rn);
        });
        dispatcher.Register("uninstall_skill", async args =>
        {
            var id = args.GetProperty("id").GetString() ?? "";
            return await _skillMarketService.UninstallSkillAsync(id);
        });
        dispatcher.Register("toggle_skill_app", async args =>
        {
            var id = args.GetProperty("id").GetString() ?? "";
            var app = args.GetProperty("app").GetString() ?? "claude";
            var en = args.GetProperty("enabled").GetBoolean();
            return await _skillMarketService.ToggleSkillAppAsync(id, app, en);
        });

        // Cloud & Backup
        dispatcher.Register("webdav_test_connection", async args =>
        {
            var url = args.GetProperty("url").GetString() ?? "";
            var u = args.GetProperty("username").GetString() ?? "";
            var p = args.GetProperty("password").GetString() ?? "";
            return await _cloudSyncService.TestWebDavConnectionAsync(url, u, p);
        });
        dispatcher.Register("webdav_sync_upload", async args =>
        {
            var url = args.GetProperty("url").GetString() ?? "";
            var u = args.GetProperty("username").GetString() ?? "";
            var p = args.GetProperty("password").GetString() ?? "";
            var c = args.TryGetProperty("content", out var cp) ? cp.GetString() ?? "" : "";
            return await _cloudSyncService.WebDavUploadAsync(url, u, p, c);
        });
        dispatcher.Register("webdav_sync_download", async args =>
        {
            var url = args.GetProperty("url").GetString() ?? "";
            var u = args.GetProperty("username").GetString() ?? "";
            var p = args.GetProperty("password").GetString() ?? "";
            return await _cloudSyncService.WebDavDownloadAsync(url, u, p);
        });
        dispatcher.Register("list_db_backups", args => _cloudSyncService.ListBackups());
        dispatcher.Register("create_db_backup", args =>
        {
            var name = args.TryGetProperty("name", out var n) ? n.GetString() : null;
            var note = args.TryGetProperty("note", out var nt) ? nt.GetString() : null;
            return _cloudSyncService.CreateBackup(name, note);
        });
        dispatcher.Register("restore_db_backup", args => _cloudSyncService.RestoreBackup(args.GetProperty("backupId").GetString() ?? ""));
        dispatcher.Register("delete_db_backup", args => _cloudSyncService.DeleteBackup(args.GetProperty("backupId").GetString() ?? ""));
        dispatcher.Register("rename_db_backup", args => _cloudSyncService.RenameBackup(args.GetProperty("backupId").GetString() ?? "", args.GetProperty("newName").GetString() ?? ""));
        dispatcher.Register("parse_deeplink", args => _cloudSyncService.ParseDeepLink(args.GetProperty("uri").GetString() ?? ""));
        dispatcher.Register("import_from_deeplink_unified", async args => await _cloudSyncService.ImportFromDeepLinkAsync(args.GetProperty("payload")));

        // Live Import
        dispatcher.Register("import_mcp_from_apps", async args => await _liveImportService.ImportMcpFromAllAppsAsync());
    }

    private void RegisterSystemAndWindowHandlers(IpcDispatcher dispatcher)
    {
        // System Integration
        dispatcher.Register("open_file_dialog", args =>
        {
            var filter = args.TryGetProperty("filter", out var f) ? f.GetString() : null;
            var title = args.TryGetProperty("title", out var t) ? t.GetString() : null;
            return _sysService.OpenFileDialog(filter, title);
        });
        dispatcher.Register("save_file_dialog", args =>
        {
            var filter = args.TryGetProperty("filter", out var f) ? f.GetString() : null;
            var title = args.TryGetProperty("title", out var t) ? t.GetString() : null;
            var defName = args.TryGetProperty("defaultName", out var dn) ? dn.GetString() : null;
            return _sysService.SaveFileDialog(filter, title, defName);
        });
        dispatcher.Register("pick_directory", args =>
        {
            var title = args.TryGetProperty("title", out var t) ? t.GetString() : null;
            return _sysService.PickDirectory(title);
        });
        dispatcher.Register("open_zip_file_dialog", args => _sysService.OpenFileDialog("Zip Archives (*.zip)|*.zip|All Files (*.*)|*.*", "Select Zip Archive"));
        dispatcher.Register("open_config_folder", args => _sysService.OpenDirectory(SystemIntegrationService.GetConfigDir()));
        dispatcher.Register("open_log_folder", args => _sysService.OpenDirectory(LoggerService.Instance.LogDirectory));
        dispatcher.Register("get_log_path", args => LoggerService.Instance.GetCurrentLogFilePath());
        dispatcher.Register("open_app_config_folder", args =>
        {
            var app = args.TryGetProperty("appType", out var a) ? a.GetString() ?? "" : "";
            return _sysService.OpenAppConfigFolder(app);
        });
        dispatcher.Register("open_external", args =>
        {
            var url = args.TryGetProperty("url", out var u) ? u.GetString() ?? "" : "";
            return _sysService.OpenExternal(url);
        });
        dispatcher.Register("open_provider_terminal", args =>
        {
            var app = args.TryGetProperty("appType", out var a) ? a.GetString() ?? "" : "";
            return _sysService.OpenAppConfigFolder(app);
        });
        dispatcher.Register("open_workspace_directory", args =>
        {
            var path = args.TryGetProperty("path", out var p) ? p.GetString() ?? "" : "";
            return _sysService.OpenDirectory(path);
        });
        dispatcher.Register("copy_text_to_clipboard", args =>
        {
            var text = args.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
            return _sysService.CopyTextToClipboard(text);
        });
        dispatcher.Register("get_auto_launch_status", args => _sysService.GetAutoLaunchStatus());
        dispatcher.Register("set_auto_launch", args => _sysService.SetAutoLaunch(args.TryGetProperty("enable", out var en) && en.GetBoolean()));
        dispatcher.Register("restart_app", args => { _sysService.RestartApp(); return true; });
        dispatcher.Register("check_all_env_conflicts", args => _sysService.CheckAllEnvConflicts());
        dispatcher.Register("check_env_conflicts", args =>
        {
            var app = args.TryGetProperty("appType", out var a) ? a.GetString() ?? "" : "";
            return _sysService.CheckEnvConflicts(app);
        });
        dispatcher.Register("get_env_conflicts", args =>
        {
            var app = args.TryGetProperty("appType", out var a) ? a.GetString() ?? "" : "";
            return _sysService.CheckEnvConflicts(app);
        });
        dispatcher.Register("probe_tool_installations", args => _sysService.ProbeToolInstallations());
        dispatcher.Register("get_tool_versions", args => _sysService.ProbeToolInstallations());
        dispatcher.Register("get_app_config_path", args => Path.Combine(SystemIntegrationService.GetConfigDir(), "config.json"));
        dispatcher.Register("get_config_dir", args => SystemIntegrationService.GetConfigDir());
        dispatcher.Register("get_claude_code_config_path", args => Path.Combine(LiveConfigService.GetClaudeDir(), "settings.json"));
        dispatcher.Register("update_tray_menu", async args =>
        {
            if (_trayService != null) await _trayService.RefreshMenuAsync();
            return true;
        });

        // Auth
        dispatcher.Register("auth_get_status", async args => await _authService.GetAntigravityAuthStatusAsync());
        dispatcher.Register("auth_get_antigravity_status", async args => await _authService.GetAntigravityAuthStatusAsync());
        dispatcher.Register("get_antigravity_quota", args => new
        {
            total = 1000000,
            used = 0,
            remaining = 1000000,
            plan = "Pro Plan",
            is_valid = true
        });

        // Local Proxy (Stubs for backward compatibility)
        dispatcher.Register("get_proxy_status", args => new { is_running = false, port = 15721, active_targets = Array.Empty<string>() });
        dispatcher.Register("is_proxy_running", args => false);
        dispatcher.Register("start_proxy_server", args => new { is_running = false, port = 15721, active_targets = Array.Empty<string>() });
        dispatcher.Register("stop_proxy_server", args => new { is_running = false, port = 15721, active_targets = Array.Empty<string>() });
        dispatcher.Register("get_proxy_takeover_status", args => new Dictionary<string, bool>());
        dispatcher.Register("set_proxy_takeover_for_app", args => new Dictionary<string, bool>());

        // Sessions
        dispatcher.Register("list_sessions", async args => await _sessionService.ScanSessionsAsync());
        dispatcher.Register("get_session_messages", async args =>
        {
            var sourcePath = args.TryGetProperty("sourcePath", out var sp) ? sp.GetString() ?? "" : (args.TryGetProperty("sessionId", out var sid) ? sid.GetString() ?? "" : "");
            return await _sessionService.GetSessionMessagesAsync(sourcePath);
        });
        dispatcher.Register("delete_session", args =>
        {
            var sourcePath = args.TryGetProperty("sourcePath", out var sp) ? sp.GetString() ?? "" : (args.TryGetProperty("sessionId", out var sid) ? sid.GetString() ?? "" : "");
            return _sessionService.DeleteSession(sourcePath);
        });
        dispatcher.Register("delete_sessions", args =>
        {
            var items = new List<DeleteSessionRequestDto>();
            if (args.TryGetProperty("sessionIds", out var sids) && sids.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in sids.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        items.Add(new DeleteSessionRequestDto { SourcePath = item.GetString() ?? "" });
                    else if (item.ValueKind == JsonValueKind.Object)
                    {
                        var req = JsonSerializer.Deserialize<DeleteSessionRequestDto>(item.GetRawText());
                        if (req != null) items.Add(req);
                    }
                }
            }
            return _sessionService.DeleteSessions(items);
        });
        dispatcher.Register("launch_session_terminal", args =>
        {
            var cmd = args.TryGetProperty("command", out var c) ? c.GetString() ?? "" : (args.TryGetProperty("sessionId", out var sid) ? $"claude resume {sid.GetString()}" : "claude");
            var cwd = args.TryGetProperty("cwd", out var d) ? d.GetString() : null;
            return _sessionService.LaunchSessionTerminal(cmd, cwd);
        });

        // Usage Analytics
        dispatcher.Register("get_usage_summary", async args =>
        {
            var s = args.TryGetProperty("startDate", out var sp) && sp.ValueKind == JsonValueKind.Number ? sp.GetInt64() : (long?)null;
            var e = args.TryGetProperty("endDate", out var ep) && ep.ValueKind == JsonValueKind.Number ? ep.GetInt64() : (long?)null;
            var app = args.TryGetProperty("appType", out var ap) ? ap.GetString() : null;
            return await _usageService.GetUsageSummaryAsync(s, e, app);
        });
        dispatcher.Register("get_usage_summary_by_app", async args =>
        {
            var s = args.TryGetProperty("startDate", out var sp) && sp.ValueKind == JsonValueKind.Number ? sp.GetInt64() : (long?)null;
            var e = args.TryGetProperty("endDate", out var ep) && ep.ValueKind == JsonValueKind.Number ? ep.GetInt64() : (long?)null;
            return await _usageService.GetUsageSummaryByAppAsync(s, e);
        });
        dispatcher.Register("get_usage_trends", async args =>
        {
            var s = args.TryGetProperty("startDate", out var sp) && sp.ValueKind == JsonValueKind.Number ? sp.GetInt64() : (long?)null;
            var e = args.TryGetProperty("endDate", out var ep) && ep.ValueKind == JsonValueKind.Number ? ep.GetInt64() : (long?)null;
            var app = args.TryGetProperty("appType", out var ap) ? ap.GetString() : null;
            return await _usageService.GetUsageTrendsAsync(s, e, app);
        });
        dispatcher.Register("get_provider_stats", async args =>
        {
            var s = args.TryGetProperty("startDate", out var sp) && sp.ValueKind == JsonValueKind.Number ? sp.GetInt64() : (long?)null;
            var e = args.TryGetProperty("endDate", out var ep) && ep.ValueKind == JsonValueKind.Number ? ep.GetInt64() : (long?)null;
            var app = args.TryGetProperty("appType", out var ap) ? ap.GetString() : null;
            return await _usageService.GetProviderStatsAsync(s, e, app);
        });
        dispatcher.Register("get_model_stats", async args =>
        {
            var s = args.TryGetProperty("startDate", out var sp) && sp.ValueKind == JsonValueKind.Number ? sp.GetInt64() : (long?)null;
            var e = args.TryGetProperty("endDate", out var ep) && ep.ValueKind == JsonValueKind.Number ? ep.GetInt64() : (long?)null;
            var app = args.TryGetProperty("appType", out var ap) ? ap.GetString() : null;
            return await _usageService.GetModelStatsAsync(s, e, app);
        });
        dispatcher.Register("get_request_logs", async args =>
        {
            var p = args.TryGetProperty("page", out var pp) ? (uint)pp.GetInt32() : 0;
            var ps = args.TryGetProperty("pageSize", out var psp) ? (uint)psp.GetInt32() : 50;
            var app = args.TryGetProperty("appType", out var ap) ? ap.GetString() : null;
            var filters = new LogFiltersDto { AppType = app };
            return await _usageService.GetRequestLogsAsync(filters, p, ps);
        });
        dispatcher.Register("get_request_detail", async args =>
        {
            var id = args.GetProperty("id").GetString() ?? args.GetProperty("id").ToString();
            return await _usageService.GetRequestDetailAsync(id);
        });
        dispatcher.Register("get_model_pricing", async args => await _usageService.GetModelPricingAsync());
        dispatcher.Register("update_model_pricing", async args =>
        {
            var mId = args.GetProperty("modelId").GetString() ?? "";
            var inP = args.TryGetProperty("inputCostPerMillion", out var inp) ? inp.GetString() ?? "0" : (args.TryGetProperty("inputPricePerM", out var ipp) ? ipp.GetDouble().ToString("F4") : "0");
            var outP = args.TryGetProperty("outputCostPerMillion", out var outp) ? outp.GetString() ?? "0" : (args.TryGetProperty("outputPricePerM", out var opp) ? opp.GetDouble().ToString("F4") : "0");
            var cInP = args.TryGetProperty("cacheReadCostPerMillion", out var cp) ? cp.GetString() ?? "0" : "0";
            var cWrP = args.TryGetProperty("cacheCreationCostPerMillion", out var cwp) ? cwp.GetString() ?? "0" : "0";
            return await _usageService.UpdateModelPricingAsync(new ModelPricingDto
            {
                ModelId = mId,
                DisplayName = mId,
                InputCostPerMillion = inP,
                OutputCostPerMillion = outP,
                CacheReadCostPerMillion = cInP,
                CacheCreationCostPerMillion = cWrP
            });
        });
        dispatcher.Register("update_model_pricing_batch", async args =>
        {
            var entries = JsonSerializer.Deserialize<List<ModelPricingDto>>(args.GetProperty("entries").GetRawText()) ?? new();
            return await _usageService.UpdateModelPricingBatchAsync(entries);
        });
        dispatcher.Register("delete_model_pricing", async args =>
        {
            var mId = args.GetProperty("modelId").GetString() ?? "";
            return await _usageService.DeleteModelPricingAsync(mId);
        });
        dispatcher.Register("get_models_dev_sync_config", async args => await _usageService.GetModelsDevSyncConfigAsync());
        dispatcher.Register("save_models_dev_sync_config", async args =>
        {
            var cfg = JsonSerializer.Deserialize<ModelsDevSyncConfigDto>(args.GetProperty("config").GetRawText()) ?? new();
            await _usageService.SaveModelsDevSyncConfigAsync(cfg);
            return true;
        });
        dispatcher.Register("record_models_dev_sync_result", async args =>
        {
            long? s = args.TryGetProperty("syncedAt", out var sp) && sp.ValueKind == JsonValueKind.Number ? sp.GetInt64() : null;
            string? err = args.TryGetProperty("error", out var ep) ? ep.GetString() : null;
            await _usageService.RecordModelsDevSyncResultAsync(s, err);
            return true;
        });
        dispatcher.Register("check_provider_limits", async args =>
        {
            var pId = args.GetProperty("providerId").GetString() ?? "";
            var at = args.TryGetProperty("appType", out var appProp) ? appProp.GetString() ?? "claude" : "claude";
            return await _usageService.CheckProviderLimitsAsync(pId, at);
        });
        dispatcher.Register("sync_session_usage", async args => await _usageService.SyncSessionUsageAsync());
        dispatcher.Register("rebuild_codex_usage", async args => await _usageService.RebuildCodexUsageAsync());
        dispatcher.Register("get_usage_data_sources", async args => await _usageService.GetDataSourceBreakdownAsync());
        dispatcher.Register("queryProviderUsage", args => new { balance = "0.00", currency = "USD", isValid = true });
        dispatcher.Register("testUsageScript", args => new { balance = "0.00", currency = "USD", isValid = true });

        // Window controls
        dispatcher.Register("plugin:window|start_dragging", args =>
        {
            WindowNativeHelper.StartDragging(this);
            return (object?)null;
        });
        dispatcher.Register("plugin:window|minimize", args =>
        {
            Dispatcher.Invoke(() => WindowState = WindowState.Minimized);
            return (object?)null;
        });
        dispatcher.Register("plugin:window|maximize", args =>
        {
            Dispatcher.Invoke(() => WindowState = WindowState.Maximized);
            return (object?)null;
        });
        dispatcher.Register("plugin:window|unmaximize", args =>
        {
            Dispatcher.Invoke(() => WindowState = WindowState.Normal);
            return (object?)null;
        });
        dispatcher.Register("plugin:window|toggle_maximize", args =>
        {
            Dispatcher.Invoke(() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized);
            return (object?)null;
        });
        dispatcher.Register("plugin:window|is_maximized", args =>
        {
            return Dispatcher.Invoke(() => WindowState == WindowState.Maximized);
        });
        dispatcher.Register("plugin:window|close", args =>
        {
            Dispatcher.Invoke(Close);
            return (object?)null;
        });
        dispatcher.Register("plugin:window|destroy", args =>
        {
            Dispatcher.Invoke(() => WpfApplication.Current.Shutdown());
            return (object?)null;
        });
        dispatcher.Register("plugin:process|exit", args =>
        {
            Dispatcher.Invoke(() => WpfApplication.Current.Shutdown());
            return (object?)null;
        });
    }
}