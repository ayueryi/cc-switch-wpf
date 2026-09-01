using System.Text.Json;
using System.Text.Json.Serialization;

namespace CCSwitch.Wpf.Bridge;

public class IpcRequest
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "invoke";

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("cmd")]
    public string Cmd { get; set; } = "";

    [JsonPropertyName("args")]
    public JsonElement Args { get; set; }
}

public class IpcResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "invoke_response";

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public class IpcEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "event_emit";

    [JsonPropertyName("event")]
    public string Event { get; set; } = "";

    [JsonPropertyName("payload")]
    public object? Payload { get; set; }
}
