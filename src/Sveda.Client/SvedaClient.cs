namespace Sveda.Client;

public sealed class SvedaClient : IDisposable
{
    private readonly HttpClient http;
    private readonly bool ownsHandler;

    public SvedaClient(SvedaClientOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));

        HttpMessageHandler handler;
        if (options.HttpMessageHandler is not null)
        {
            handler = options.HttpMessageHandler;
            ownsHandler = false;
        }
        else
        {
            handler = new SocketsHttpHandler
            {
                ConnectTimeout = options.ConnectTimeout,
            };
            ownsHandler = true;
        }

        http = new HttpClient(handler, disposeHandler: ownsHandler)
        {
            Timeout = options.Timeout,
        };

        var baseUrl = (options.BaseUrl ?? "").Trim().TrimEnd('/');
        if (baseUrl.Length > 0)
        {
            http.BaseAddress = new Uri(baseUrl + "/");
        }

        var transport = new SvedaHttp(http, options);
        Embed = new EmbedResource(transport);
        Chat = new ChatResource(transport);
        Histories = new HistoriesResource(transport);
    }

    public SvedaClientOptions Options { get; }

    public EmbedResource Embed { get; }

    public ChatResource Chat { get; }

    public HistoriesResource Histories { get; }

    public async Task<SvedaHostSession> StartHostSessionAsync(CreateTokenRequest? request = null, CancellationToken cancellationToken = default)
    {
        var baseUrl = (Options.BaseUrl ?? "").Trim().TrimEnd('/');
        var hostKey = Options.HostApiKey?.Trim();
        if (baseUrl.Length == 0 || string.IsNullOrEmpty(hostKey))
        {
            throw new SvedaConfigurationException("Set SVEDA_CLIENT_BASE_URL and SVEDA_CLIENT_HOST_API_KEY.");
        }

        var token = await Embed.CreateTokenAsync(request ?? new CreateTokenRequest(), cancellationToken).ConfigureAwait(false);
        if (token.Token.Length == 0)
        {
            throw new SvedaApiException("Sidecar returned an empty embed token.", 502);
        }

        return new SvedaHostSession
        {
            Origin = baseUrl,
            Token = token.Token,
            ExpiresIn = token.ExpiresIn,
            Appearance = token.Appearance,
        };
    }

    public void Dispose()
    {
        http.Dispose();
        GC.SuppressFinalize(this);
    }
}
