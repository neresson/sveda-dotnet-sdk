using System.Text.Json;

namespace Sveda.Client;

public sealed class StreamEvent
{
    public StreamEvent(string type, JsonElement payload)
    {
        Type = type;
        Payload = payload;
    }

    public string Type { get; }

    public JsonElement Payload { get; }

    public string? this[string name]
    {
        get
        {
            if (!Payload.TryGetProperty(name, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return null;
            }

            return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }
    }
}
