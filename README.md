# sveda-dotnet-sdk

.NET SDK for the [Sveda](https://sveda.dev) sidecar HTTP API.

Docs: [sveda.dev/docs/hosts/dotnet](https://sveda.dev/docs/hosts/dotnet)

NuGet: `sveda-dotnet-sdk` (C# types stay in `Sveda.Client` and `Sveda.Host`)

## Install

```bash
dotnet add package sveda-dotnet-sdk
```

From a local checkout:

```xml
<ProjectReference Include="..\sveda-dotnet-sdk\src\Sveda.Client\Sveda.Client.csproj" />
```

## Sidecar client

```csharp
using var client = new SvedaClient(new SvedaClientOptions
{
    BaseUrl = "https://sveda.example.com",
    HostApiKey = hostKey,
});

var session = await client.StartHostSessionAsync(new CreateTokenRequest
{
    VisitorId = "user-1",
});
```

## Host integration (embed session + MCP tools)

The `Sveda.Host` namespace mirrors the Laravel SDK: mint an embed token with `host_mcp_url` / `host_mcp_token`, and expose `POST /mcp/sveda` for the sidecar to list and call your tools.

```csharp
using Sveda.Host;

var host = new HostManager(new HostManagerOptions
{
    BaseUrl = Environment.GetEnvironmentVariable("SVEDA_CLIENT_BASE_URL")!,
    HostApiKey = Environment.GetEnvironmentVariable("SVEDA_CLIENT_HOST_API_KEY")!,
    McpUrl = Environment.GetEnvironmentVariable("SVEDA_CLIENT_MCP_URL"),
    ServerName = "My App",
    Instructions = "Tools for the current user.",
});
host.ResolveToolsUsing(() => [new SearchPostsTool()]);

app.MapSvedaMcp(host);

var session = await host.StartSessionAsync(new Dictionary<string, object?> { ["id"] = "user-1" }, requestOrigin);
```

By default, `HostManager` mints opaque MCP bearer tokens with an in-memory store (fine for development). Override with `MintTokenUsing` and `VerifyBearerUsing` for production auth.

Implement `IHostTool` with `Name`, `Description`, `InputSchema`, `Mode`, `Domain`, and `HandleAsync`.

## License

MIT
