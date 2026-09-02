# Prism (WPF + Blazor Hybrid Native Gateway)

基于 **WPF (.NET 10) + Microsoft Edge WebView2** 实现的 Prism Windows 原生多模型分发网关，旨在 100% 完美复刻并复用现有 React + Tailwind 前端。

---

## 架构特性

1. **前端 100% 零修改复用**：
   - 内置 [`Bridge/TauriShim.js`](Bridge/TauriShim.js)，在 WebView2 加载任何文档前自动注入 `window.__TAURI_INTERNALS__` 与 `window.__TAURI__` polyfill。
   - 前端无需改动任何 `invoke` / `listen` 代码，直接对接 C# 后端。
2. **现代化无边框窗体**：
   - 使用 WPF `WindowChrome` 实现无边框半透明窗体与原生拖拽区域。
3. **极速 SQLite 与 WAL 模式**：
   - 基于 `Microsoft.Data.Sqlite` + `Dapper`，支持高并发轻量存取与数据防损坏。
4. **虚拟域名资源映射**：
   - 自动将 `cc-switch-diy/dist` 映射至 `https://app.ccswitch.local/`，本地离线极速加载。

---

## 快速运行与构建

### 1. 开发调试模式（Dev）
- 启动前端开发服务器：
  ```powershell
  cd ../cc-switch-diy
  pnpm dev:renderer
  ```
- 启动 WPF 客户端：
  ```powershell
  dotnet run
  ```

### 2. 编译发布（Release）
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
