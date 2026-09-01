using System.IO;
using System.Net;
using System.Text;

namespace CCSwitch.Wpf.Services;

public class LocalStaticServer : IDisposable
{
    private HttpListener? _listener;
    private readonly string _distPath;
    private bool _isRunning;
    public int Port { get; private set; }
    public string Url => $"http://127.0.0.1:{Port}";

    public LocalStaticServer(string distPath, int preferredPort = 15820)
    {
        _distPath = distPath;
        Port = preferredPort;
    }

    public void Start()
    {
        if (_isRunning) return;

        // Try preferred port, then cycle through next 100 ports if in use
        for (int p = Port; p < Port + 100; p++)
        {
            try
            {
                var listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{p}/");
                listener.Start();

                _listener = listener;
                Port = p;
                _isRunning = true;
                Task.Run(ListenLoop);
                return;
            }
            catch (HttpListenerException)
            {
                // Port occupied, try next port
            }
            catch (Exception)
            {
                // Try next
            }
        }

        throw new InvalidOperationException("Could not find an available HTTP port for LocalStaticServer.");
    }

    private async Task ListenLoop()
    {
        while (_isRunning && _listener != null && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => ProcessRequest(context));
            }
            catch
            {
                if (!_isRunning) break;
            }
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        try
        {
            var rawUrl = context.Request.Url?.AbsolutePath ?? "/";
            if (rawUrl == "/" || string.IsNullOrEmpty(rawUrl))
            {
                rawUrl = "/index.html";
            }

            var relPath = rawUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var filePath = Path.Combine(_distPath, relPath);

            if (!File.Exists(filePath))
            {
                // SPA fallback: return index.html for non-asset routes
                filePath = Path.Combine(_distPath, "index.html");
            }

            if (File.Exists(filePath))
            {
                var ext = Path.GetExtension(filePath).ToLowerInvariant();
                var contentType = ext switch
                {
                    ".html" => "text/html; charset=utf-8",
                    ".js" or ".mjs" => "application/javascript; charset=utf-8",
                    ".css" => "text/css; charset=utf-8",
                    ".json" => "application/json; charset=utf-8",
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".svg" => "image/svg+xml",
                    ".ico" => "image/x-icon",
                    ".webp" => "image/webp",
                    ".woff" => "font/woff",
                    ".woff2" => "font/woff2",
                    ".ttf" => "font/ttf",
                    _ => "application/octet-stream"
                };

                context.Response.ContentType = contentType;
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Cache-Control", "no-cache");

                var bytes = File.ReadAllBytes(filePath);
                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            }
            else
            {
                context.Response.StatusCode = 404;
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            var errBytes = Encoding.UTF8.GetBytes(ex.Message);
            context.Response.OutputStream.Write(errBytes, 0, errBytes.Length);
        }
        finally
        {
            try { context.Response.Close(); } catch { }
        }
    }

    public void Dispose()
    {
        _isRunning = false;
        try { _listener?.Stop(); } catch { }
        try { _listener?.Close(); } catch { }
    }
}
