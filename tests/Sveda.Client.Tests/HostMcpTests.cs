using System.Text.Json;
using System.Text.Json.Nodes;
using Sveda.Host;
using Xunit;

namespace Sveda.Client.Tests;

public sealed class HostMcpTests
{
    private class EchoTool : IHostTool
    {
        public virtual string Name => "echo_message";
        public string Description => "Echo a message back.";
        public virtual string Mode => HostModes.Read;
        public string Domain => "demo";
        public virtual string Confirmation => "auto";
        public IReadOnlyDictionary<string, object?> InputSchema => HostToolSchema.Object(new Dictionary<string, Dictionary<string, object?>>
        {
            ["message"] = new Dictionary<string, object?> { ["type"] = "string", ["description"] = "Message to echo", ["required"] = true },
        });

        public Task<object?> HandleAsync(IReadOnlyDictionary<string, object?> arguments, HostCallContext context, CancellationToken cancellationToken = default)
        {
            arguments.TryGetValue("message", out var message);
            return Task.FromResult<object?>(new { success = true, data = new { message } });
        }
    }

    [Fact]
    public async Task StartSessionSendsMcpFields()
    {
        var handler = new StubHandler
        {
            JsonBody = """{"token":"embed-token","visitor_id":"aspnet-playground","expires_in":3600}""",
        };

        var host = new HostManager(new HostManagerOptions
        {
            BaseUrl = "https://sveda.test",
            HostApiKey = "host-secret",
            McpUrl = "https://app.test/mcp/sveda",
            HttpMessageHandler = handler,
        });
        host.ResolveToolsUsing(() => [new EchoTool()]);

        var session = await host.StartSessionAsync(new Dictionary<string, object?> { ["id"] = "aspnet-playground" });

        Assert.Equal("embed-token", session.Token);
        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"host_mcp_url\":\"https://app.test/mcp/sveda\"", recorded.Body);
        Assert.Contains("\"host_mcp_token\":", recorded.Body);
    }

    [Fact]
    public async Task StartSessionSendsPolicy()
    {
        var handler = new StubHandler
        {
            JsonBody = """{"token":"embed-token","visitor_id":"aspnet-playground","expires_in":3600}""",
        };

        var host = new HostManager(new HostManagerOptions
        {
            BaseUrl = "https://sveda.test",
            HostApiKey = "host-secret",
            McpUrl = "https://app.test/mcp/sveda",
            HttpMessageHandler = handler,
        });
        host.ResolveToolsUsing(() => [new EchoTool()]);
        host.PolicyUsing(_ => "reader");

        var session = await host.StartSessionAsync(new Dictionary<string, object?> { ["id"] = "aspnet-playground" });

        Assert.Equal("embed-token", session.Token);
        var recorded = Assert.Single(handler.Requests);
        Assert.Contains("\"policy\":\"reader\"", recorded.Body);
    }

    [Fact]
    public async Task ToolsCallFiltersByAuthenticatedUser()
    {
        var host = new HostManager();
        host.ResolveToolsUsing(user =>
        {
            if (user is IReadOnlyDictionary<string, object?> map
                && map.TryGetValue("id", out var id)
                && Convert.ToString(id) == "user-1")
            {
                return [new EchoTool()];
            }

            return [];
        });

        var allowedToken = host.DefaultMintToken(new Dictionary<string, object?> { ["id"] = "user-1" });
        var deniedToken = host.DefaultMintToken(new Dictionary<string, object?> { ["id"] = "other" });

        var allowed = await InvokeAsync(host, allowedToken, "tools/call", new JsonObject
        {
            ["name"] = "echo_message",
            ["arguments"] = new JsonObject { ["message"] = "hello" },
        });
        Assert.False(allowed.Body!["result"]!["isError"]!.GetValue<bool>());

        var denied = await InvokeAsync(host, deniedToken, "tools/call", new JsonObject
        {
            ["name"] = "echo_message",
            ["arguments"] = new JsonObject { ["message"] = "hello" },
        }, 2);
        Assert.True(denied.Body!["result"]!["isError"]!.GetValue<bool>());
        Assert.Contains("Unknown tool", denied.Body!["result"]!["content"]![0]!["text"]!.GetValue<string>());
    }

    [Fact]
    public async Task ZeroArgResolveToolsCallbackStillWorks()
    {
        var host = new HostManager();
        host.ResolveToolsUsing(() => [new EchoTool()]);
        var token = host.DefaultMintToken(new Dictionary<string, object?> { ["id"] = "user-1" });

        var list = await InvokeAsync(host, token, "tools/list", new JsonObject { ["per_page"] = 250 });
        var tools = list.Body!["result"]!["tools"]!.AsArray();
        Assert.Single(tools);
        Assert.Equal("echo_message", tools[0]!["name"]!.GetValue<string>());
    }

    [Fact]
    public async Task InitializeReportsConfiguredNameAndInstructions()
    {
        var host = new HostManager(new HostManagerOptions
        {
            ServerName = "Playground Feed",
            Instructions = "Feed tools for the current user.",
        });
        host.ResolveToolsUsing(() => [new EchoTool()]);
        var token = host.DefaultMintToken(new Dictionary<string, object?> { ["id"] = "user-1" });

        var response = await InvokeAsync(host, token, "initialize", new JsonObject
        {
            ["protocolVersion"] = "2025-11-25",
            ["capabilities"] = new JsonObject(),
            ["clientInfo"] = new JsonObject { ["name"] = "test", ["version"] = "0.1.0" },
        });

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("Playground Feed", response.Body!["result"]!["serverInfo"]!["name"]!.GetValue<string>());
        Assert.Equal("Feed tools for the current user.", response.Body!["result"]!["instructions"]!.GetValue<string>());
    }

    [Fact]
    public async Task AuthenticatedUserCanListAndCallTools()
    {
        var host = new HostManager();
        host.ResolveToolsUsing(() => [new EchoTool()]);
        var token = host.DefaultMintToken(new Dictionary<string, object?> { ["id"] = "user-1" });

        var list = await InvokeAsync(host, token, "tools/list", new JsonObject { ["per_page"] = 250 });
        var tools = list.Body!["result"]!["tools"]!.AsArray();
        Assert.Single(tools);
        Assert.Equal("echo_message", tools[0]!["name"]!.GetValue<string>());
        Assert.Equal("demo", tools[0]!["_meta"]!["domain"]!.GetValue<string>());
        Assert.False(tools[0]!["_meta"]!.AsObject().ContainsKey("confirmation"));

        var call = await InvokeAsync(host, token, "tools/call", new JsonObject
        {
            ["name"] = "echo_message",
            ["arguments"] = new JsonObject { ["message"] = "hello" },
        }, 2);

        Assert.False(call.Body!["result"]!["isError"]!.GetValue<bool>());
        var text = call.Body!["result"]!["content"]![0]!["text"]!.GetValue<string>();
        using var document = JsonDocument.Parse(text);
        Assert.Equal("hello", document.RootElement.GetProperty("data").GetProperty("message").GetString());
    }

    [Fact]
    public async Task ConfirmationMetaIsPublishedWhenRequired()
    {
        var host = new HostManager();
        host.ResolveToolsUsing(() => [new EchoTool(), new DeleteTool()]);
        var token = host.DefaultMintToken(new Dictionary<string, object?> { ["id"] = "user-1" });

        var list = await InvokeAsync(host, token, "tools/list", new JsonObject());
        var tools = list.Body!["result"]!["tools"]!.AsArray().ToDictionary(tool => tool!["name"]!.GetValue<string>());
        Assert.False(tools["echo_message"]!["_meta"]!.AsObject().ContainsKey("confirmation"));
        Assert.Equal("required", tools["delete_post"]!["_meta"]!["confirmation"]!.GetValue<string>());
        Assert.Equal("delete", tools["delete_post"]!["_meta"]!["mode"]!.GetValue<string>());
    }

    private sealed class DeleteTool : EchoTool
    {
        public override string Name => "delete_post";
        public override string Mode => HostModes.Delete;
        public override string Confirmation => "required";
    }

    private static async Task<HostMcpResult> InvokeAsync(HostManager host, string token, string method, JsonObject parameters, int id = 1)
    {
        var auth = await host.AuthenticateBearerTokenAsync(token);
        Assert.NotNull(auth);

        var payload = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = parameters,
        };

        return await HostMcpHandler.HandleAsync(host, payload, new HostCallContext { User = auth!.User });
    }
}
