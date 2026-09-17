using System.Text.Json.Nodes;

namespace Sveda.Client;

public sealed class MessageResponse
{
    public MessageResponse(JsonNode payload)
    {
        Payload = payload;
    }

    public JsonNode Payload { get; }

    public string Explanation => JsonUtil.GetString(Payload, "explanation");

    public int TokensUsed => JsonUtil.GetInt(Payload, "tokens_used");

    public string ChatId => JsonUtil.GetString(Payload, "chat_id");
}
