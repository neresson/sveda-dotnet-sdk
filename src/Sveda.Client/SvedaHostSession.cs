using System.Text.Json.Nodes;

namespace Sveda.Client;

public sealed class SvedaHostSession
{
    public string Origin { get; init; } = "";

    public string Token { get; init; } = "";

    public int ExpiresIn { get; init; }

    public JsonNode? Appearance { get; init; }
}
