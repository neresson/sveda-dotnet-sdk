using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sveda.Host;

public static class HostMcpHandler
{
    public const string McpProtocolVersion = "2025-11-25";

    public static async Task<HostMcpResult> HandleAsync(
        HostManager host,
        JsonObject payload,
        HostCallContext context,
        CancellationToken cancellationToken = default)
    {
        var method = payload["method"]?.GetValue<string>() ?? "";
        var id = payload["id"];
        var isNotification = id is null;
        var parameters = payload["params"] as JsonObject ?? new JsonObject();

        if (method == "notifications/initialized")
        {
            return HostMcpResult.Empty(202);
        }

        if (method == "initialize")
        {
            return JsonRpc(200, id, InitializeResult(host), sessionHeaders: true);
        }

        if (method == "tools/list")
        {
            var perPage = Math.Clamp(parameters["per_page"]?.GetValue<int?>() ?? parameters["perPage"]?.GetValue<int?>() ?? 250, 1, 250);
            var tools = host.ResolveTools().Select(ToMcpTool).ToList();
            var start = CursorStart(parameters);
            var slice = tools.Skip(start).Take(perPage).ToList();
            var result = new JsonObject { ["tools"] = JsonSerializer.SerializeToNode(slice) };
            if (start + slice.Count < tools.Count)
            {
                result["nextCursor"] = start + slice.Count;
            }

            return JsonRpc(200, id, result);
        }

        if (method == "tools/call")
        {
            var name = parameters["name"]?.GetValue<string>() ?? "";
            var arguments = ReadArguments(parameters["arguments"]);
            var tool = host.ResolveTools().FirstOrDefault(candidate => candidate.Name == name);
            if (tool is null)
            {
                return JsonRpc(200, id, new JsonObject
                {
                    ["content"] = JsonSerializer.SerializeToNode(new[] { new { type = "text", text = $"Unknown tool: {name}" } }),
                    ["isError"] = true,
                });
            }

            try
            {
                var output = await tool.HandleAsync(arguments, context, cancellationToken).ConfigureAwait(false);
                return JsonRpc(200, id, EncodeToolResult(output));
            }
            catch (Exception exception)
            {
                return JsonRpc(200, id, new JsonObject
                {
                    ["content"] = JsonSerializer.SerializeToNode(new[] { new { type = "text", text = exception.Message } }),
                    ["isError"] = true,
                });
            }
        }

        if (isNotification)
        {
            return HostMcpResult.Empty(202);
        }

        return JsonRpcError(200, id, -32601, $"Method not found: {method}");
    }

    private static JsonObject InitializeResult(HostManager host)
    {
        var result = new JsonObject
        {
            ["protocolVersion"] = McpProtocolVersion,
            ["capabilities"] = new JsonObject
            {
                ["tools"] = new JsonObject { ["listChanged"] = false },
            },
            ["serverInfo"] = new JsonObject
            {
                ["name"] = host.Options.ServerName,
                ["version"] = host.Options.ServerVersion,
            },
        };

        if (!string.IsNullOrWhiteSpace(host.Options.Instructions))
        {
            result["instructions"] = host.Options.Instructions.Trim();
        }

        return result;
    }

    private static Dictionary<string, object?> ToMcpTool(IHostTool tool)
    {
        var schema = tool.InputSchema.Count == 0
            ? new Dictionary<string, object?> { ["type"] = "object", ["properties"] = new Dictionary<string, object?>() }
            : tool.InputSchema;

        return new Dictionary<string, object?>
        {
            ["name"] = tool.Name,
            ["title"] = tool.Name,
            ["description"] = tool.Description,
            ["inputSchema"] = schema,
            ["annotations"] = Annotations(tool.Mode),
            ["_meta"] = new Dictionary<string, object?> { ["domain"] = tool.Domain, ["mode"] = tool.Mode },
        };
    }

    private static Dictionary<string, object?> Annotations(string mode)
    {
        if (mode == HostModes.Read)
        {
            return new Dictionary<string, object?> { ["readOnlyHint"] = true };
        }

        if (mode == HostModes.Delete)
        {
            return new Dictionary<string, object?> { ["readOnlyHint"] = false, ["destructiveHint"] = true };
        }

        return new Dictionary<string, object?> { ["readOnlyHint"] = false, ["destructiveHint"] = false };
    }

    private static JsonObject EncodeToolResult(object? output)
    {
        var text = output switch
        {
            null => "null",
            string value => value,
            byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
            _ => JsonSerializer.Serialize(output),
        };

        return new JsonObject
        {
            ["content"] = JsonSerializer.SerializeToNode(new[] { new { type = "text", text } }),
            ["isError"] = false,
        };
    }

    private static int CursorStart(JsonObject parameters)
    {
        var cursor = parameters["cursor"]?.GetValue<string>();
        return int.TryParse(cursor, out var start) ? Math.Max(0, start) : 0;
    }

    private static IReadOnlyDictionary<string, object?> ReadArguments(JsonNode? node)
    {
        if (node is not JsonObject json)
        {
            return new Dictionary<string, object?>();
        }

        return json.ToDictionary(pair => pair.Key, pair => (object?)pair.Value?.Deserialize<object>());
    }

    private static HostMcpResult JsonRpc(int status, JsonNode? id, JsonObject result, bool sessionHeaders = false)
    {
        var body = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["result"] = result,
        };

        var headers = sessionHeaders
            ? new Dictionary<string, string>
            {
                ["mcp-protocol-version"] = McpProtocolVersion,
                ["mcp-session-id"] = $"sess-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            }
            : new Dictionary<string, string>();

        return new HostMcpResult(status, body, headers);
    }

    private static HostMcpResult JsonRpcError(int status, JsonNode? id, int code, string message)
    {
        var body = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message,
            },
        };

        return new HostMcpResult(status, body, new Dictionary<string, string>());
    }
}

public sealed record HostMcpResult(int StatusCode, JsonObject? Body, IReadOnlyDictionary<string, string> Headers)
{
    public static HostMcpResult Empty(int statusCode) => new(statusCode, null, new Dictionary<string, string>());
}
