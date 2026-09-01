// ============================================================================
// Comprehensive Tauri v2 API Polyfill for WPF Microsoft.Web.WebView2
// ============================================================================

(function () {
  console.log("[WPF Bridge] Initializing Tauri v2 Shim...");

  const pendingRequests = new Map();
  const callbacks = new Map();
  const eventListeners = new Map(); // eventName -> Set<callbackId>
  let nextRequestId = 1;

  // Forward console messages to C# backend
  const origLog = console.log;
  const origWarn = console.warn;
  const origError = console.error;

  function forwardLog(level, args) {
    try {
      if (window.chrome && window.chrome.webview) {
        const msg = args.map((a) => {
          if (typeof a === "object") {
            try { return JSON.stringify(a); } catch (e) { return String(a); }
          }
          return String(a);
        }).join(" ");
        window.chrome.webview.postMessage({
          type: "console_log",
          level: level,
          message: msg,
        });
      }
    } catch (_) {}
  }

  console.log = function (...args) {
    origLog.apply(console, args);
    forwardLog("log", args);
  };
  console.warn = function (...args) {
    origWarn.apply(console, args);
    forwardLog("warn", args);
  };
  console.error = function (...args) {
    origError.apply(console, args);
    forwardLog("error", args);
  };

  function registerCallback(callback, once = false) {
    const identifier = window.crypto.getRandomValues(new Uint32Array(1))[0];
    callbacks.set(identifier, (data) => {
      if (once) {
        unregisterCallback(identifier);
      }
      return callback && callback(data);
    });
    return identifier;
  }

  function unregisterCallback(id) {
    callbacks.delete(id);
  }

  function runCallback(id, data) {
    const callback = callbacks.get(id);
    if (callback) {
      try {
        callback(data);
      } catch (err) {
        console.error(`[WPF Bridge] Error in callback ${id}:`, err);
      }
    }
  }

  // Handle messages received from C# backend
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener("message", function (event) {
      let data = event.data;
      if (!data) return;

      if (typeof data === "string") {
        try {
          data = JSON.parse(data);
        } catch (e) {
          console.warn("[WPF Bridge] Failed to parse message from C#:", data);
          return;
        }
      }

      if (data.type === "invoke_response") {
        const req = pendingRequests.get(data.id);
        if (req) {
          pendingRequests.delete(data.id);
          if (data.error) {
            console.error(`[WPF Bridge] Command ${req.cmd} error:`, data.error);
            req.reject(new Error(data.error));
          } else {
            req.resolve(data.result);
          }
        }
      } else if (data.type === "event_emit") {
        const targetEvent = data.event;
        const listeners = eventListeners.get(targetEvent);
        if (listeners) {
          listeners.forEach((callbackId) => {
            runCallback(callbackId, {
              event: targetEvent,
              id: callbackId,
              payload: data.payload,
            });
          });
        }
      }
    });
  }

  // Intercept window drag regions
  document.addEventListener("mousedown", function (e) {
    if (e.button !== 0) return; // Left mouse button only
    const target = e.target;
    if (!target || !(target instanceof Element)) return;

    const dragElement = target.closest(
      "[data-tauri-drag-region], [style*='WebkitAppRegion: drag'], [style*='-webkit-app-region: drag']"
    );
    const noDragElement = target.closest(
      "button, a, input, select, textarea, [data-no-drag], [style*='WebkitAppRegion: no-drag'], [style*='-webkit-app-region: no-drag']"
    );

    if (dragElement && !noDragElement) {
      invoke("plugin:window|start_dragging").catch(() => {});
    }
  });

  // Core invoke method matching Tauri v2 internals
  function invoke(cmd, args = {}, options = {}) {
    // 1. Internal Event plugin emulation
    if (cmd === "plugin:event|listen") {
      const evName = args.event;
      const handlerId = args.handler;
      if (!eventListeners.has(evName)) {
        eventListeners.set(evName, new Set());
      }
      eventListeners.get(evName).add(handlerId);
      return Promise.resolve(handlerId);
    }

    if (cmd === "plugin:event|unlisten") {
      const evName = args.event;
      const eventId = args.eventId;
      const listeners = eventListeners.get(evName);
      if (listeners) {
        listeners.delete(eventId);
      }
      unregisterCallback(eventId);
      return Promise.resolve();
    }

    if (cmd === "plugin:event|emit") {
      const evName = args.event;
      const payload = args.payload;
      const listeners = eventListeners.get(evName);
      if (listeners) {
        listeners.forEach((callbackId) => {
          runCallback(callbackId, {
            event: evName,
            id: callbackId,
            payload: payload,
          });
        });
      }
      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({
          type: "event_emit",
          event: evName,
          payload: payload,
        });
      }
      return Promise.resolve();
    }

    // 2. Synchronous window queries with instant fallback
    if (cmd === "plugin:window|is_fullscreen") return Promise.resolve(false);
    if (cmd === "plugin:window|is_focused") return Promise.resolve(true);
    if (cmd === "plugin:window|is_visible") return Promise.resolve(true);
    if (cmd === "plugin:window|is_decorated") return Promise.resolve(true);
    if (cmd === "plugin:window|is_resizable") return Promise.resolve(true);
    if (cmd === "plugin:window|theme") return Promise.resolve("dark");
    if (cmd === "set_window_theme") return Promise.resolve();
    if (cmd === "plugin:log|log") return Promise.resolve();

    // 3. Dispatch to C# Backend
    return new Promise((resolve, reject) => {
      const id = nextRequestId++;
      pendingRequests.set(id, { cmd, resolve, reject });

      if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage({
          type: "invoke",
          id: id,
          cmd: cmd,
          args: args || {},
        });
      } else {
        reject(new Error("WebView2 environment not detected"));
      }
    });
  }

  // Define Tauri v2 globals
  window.__TAURI_INTERNALS__ = {
    invoke: invoke,
    transformCallback: registerCallback,
    unregisterCallback: unregisterCallback,
    runCallback: runCallback,
    callbacks: callbacks,
    convertFileSrc: function (filePath, protocol = "asset") {
      return filePath;
    },
    metadata: {
      currentWindow: {
        label: "main",
      },
      currentWebview: {
        label: "main",
        windowLabel: "main",
      },
    },
    plugins: {},
  };

  window.__TAURI_EVENT_PLUGIN_INTERNALS__ = {
    unregisterListener: function (event, eventId) {
      const listeners = eventListeners.get(event);
      if (listeners) {
        listeners.delete(eventId);
      }
      unregisterCallback(eventId);
    },
  };

  window.__TAURI__ = window.__TAURI_INTERNALS__;
  window.isTauri = true;
  if (typeof globalThis !== "undefined") {
    globalThis.isTauri = true;
  }

  console.log("[WPF Bridge] Tauri v2 Shim ready喵!");
})();