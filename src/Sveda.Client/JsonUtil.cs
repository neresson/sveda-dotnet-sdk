using System.Text.Json.Nodes;

namespace Sveda.Client;

internal static class JsonUtil
{
    public static string GetString(JsonNode? node, string property)
    {
        var value = node?[property];
        if (value is null)
        {
            return "";
        }

        try
        {
            return value.GetValue<string>() ?? "";
        }
        catch (InvalidOperationException)
        {
            return value.ToJsonString().Trim('"');
        }
    }

    public static int GetInt(JsonNode? node, string property, int fallback = 0)
    {
        var value = node?[property];
        if (value is null)
        {
            return fallback;
        }

        try
        {
            return value.GetValue<int>();
        }
        catch (InvalidOperationException)
        {
            return fallback;
        }
    }

    public static EmbedTokenResponse ToEmbedToken(JsonNode node)
    {
        var expires = node["expires_in"] is null ? 3600 : GetInt(node, "expires_in", 3600);
        var appearance = node["appearance"];
        if (appearance is not JsonObject)
        {
            appearance = null;
        }

        return new EmbedTokenResponse
        {
            Token = GetString(node, "token"),
            VisitorId = GetString(node, "visitor_id"),
            ExpiresIn = Math.Max(60, expires),
            Appearance = appearance,
        };
    }

    public static Dictionary<string, string> ToTokenPayload(CreateTokenRequest request)
    {
        var payload = new Dictionary<string, string>();
        var visitorId = request.VisitorId?.Trim();
        if (!string.IsNullOrEmpty(visitorId))
        {
            payload["visitor_id"] = visitorId;
        }

        var mcpUrl = request.HostMcpUrl?.Trim();
        var mcpToken = request.HostMcpToken?.Trim();
        if (!string.IsNullOrEmpty(mcpUrl) && !string.IsNullOrEmpty(mcpToken))
        {
            payload["host_mcp_url"] = mcpUrl;
            payload["host_mcp_token"] = mcpToken;
        }

        return payload;
    }
}
