using System.Net.Http;

namespace Sveda.Host;

public sealed class HostManagerOptions
{
    public string BaseUrl { get; set; } = "";

    public string HostApiKey { get; set; } = "";

    public string McpUrl { get; set; } = "";

    public string McpPath { get; set; } = "/mcp/sveda";

    public string ServerName { get; set; } = "Host Application";

    public string ServerVersion { get; set; } = "0.1.0";

    public string Instructions { get; set; } = "";

    public string McpAbility { get; set; } = "sveda:mcp";

    public int TokenTtlSeconds { get; set; } = 3600;

    public string VisitorPrefix { get; set; } = "host";

    public HttpMessageHandler? HttpMessageHandler { get; set; }
}
