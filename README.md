# sveda-dotnet-sdk

.NET SDK for the [Sveda AI](https://github.com/neresson/sveda) sidecar HTTP API.

NuGet: `sveda-dotnet-sdk` (C# types stay in `Sveda.Client`)

## Install

```bash
dotnet add package sveda-dotnet-sdk
```

Until NuGet, project-reference the checkout:

```xml
<ProjectReference Include="..\sveda-dotnet-sdk\src\Sveda.Client\Sveda.Client.csproj" />
```

## Usage

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

## License

MIT
