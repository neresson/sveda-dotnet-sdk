using System.Text.Json.Nodes;

namespace Sveda.Client;

public sealed class EmbedTokenResponse
{
    public string Token { get; init; } = "";

    public string VisitorId { get; init; } = "";

    public int ExpiresIn { get; init; } = 3600;

    public JsonNode? Appearance { get; init; }
}
