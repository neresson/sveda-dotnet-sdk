namespace Sveda.Client;

public sealed class SvedaClientOptions
{
    public string BaseUrl { get; set; } = "";

    public string? HostApiKey { get; set; }

    public string? EmbedToken { get; set; }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

    public HttpMessageHandler? HttpMessageHandler { get; set; }
}
