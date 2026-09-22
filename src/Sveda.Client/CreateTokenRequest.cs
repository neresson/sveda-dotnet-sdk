namespace Sveda.Client;

public sealed class CreateTokenRequest
{
    public string? VisitorId { get; set; }

    public string? HostMcpUrl { get; set; }

    public string? HostMcpToken { get; set; }

    public string? Policy { get; set; }
}
