using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sveda.Host;

public static class HostManifest
{
    public const string Schema = "sveda.host/v1";

    public static JsonObject Describe(HostManager host, object? user = null)
    {
        var authenticated = user is not null;
        string? policy = authenticated ? host.PolicyFor(user) : null;

        return new JsonObject
        {
            ["schema"] = Schema,
            ["sdk"] = new JsonObject
            {
                ["language"] = "dotnet",
                ["version"] = typeof(HostManifest).Assembly.GetName().Version?.ToString() ?? "unknown",
            },
            ["subject"] = new JsonObject
            {
                ["authenticated"] = authenticated,
                ["policy"] = policy,
            },
            ["hooks"] = JsonSerializer.SerializeToNode(host.RegisteredHooks())!.AsObject(),
            ["tools"] = JsonSerializer.SerializeToNode(
                host.ResolveTools(user).Select(HostMcpHandler.ToMcpTool).ToList()),
        };
    }

    public static string DescribeJson(HostManager host, object? user = null, bool indented = true)
        => (Describe(host, user)).ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = indented,
        });
}
