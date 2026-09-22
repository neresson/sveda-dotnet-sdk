using Sveda.Client;

namespace Sveda.Host;

public sealed class HostManager
{
    private readonly HostManagerOptions options;
    private readonly McpTokenStore tokenStore;
    private Func<object?, IReadOnlyList<IHostTool>>? resolveToolsUsing;
    private Func<object?, string?>? policyUsing;
    private Func<object?, string>? visitorIdUsing;
    private Func<object?, Task<string>>? mintTokenUsing;
    private Func<string, Task<HostAuth?>>? verifyBearerUsing;
    private Func<object?, bool>? authorizeUsing;
    private Func<object?, Task>? afterAuthenticateUsing;
    private readonly List<IHostTool> registeredTools = [];

    public HostManager(HostManagerOptions? options = null, McpTokenStore? tokenStore = null)
    {
        this.options = Normalize(options ?? new HostManagerOptions());
        this.tokenStore = tokenStore ?? new McpTokenStore(TimeSpan.FromSeconds(Math.Max(60, this.options.TokenTtlSeconds)));
        mintTokenUsing = user => Task.FromResult(DefaultMintToken(user));
        verifyBearerUsing = token => Task.FromResult<HostAuth?>(this.tokenStore.Verify(token, this.options.McpAbility));
    }

    public bool Authorize(object? user) => authorizeUsing?.Invoke(user) ?? true;

    public Task AfterAuthenticateAsync(object? user)
        => afterAuthenticateUsing?.Invoke(user) ?? Task.CompletedTask;

    public HostManagerOptions Options => options;

    public McpTokenStore TokenStore => tokenStore;

    public void ResolveToolsUsing(Func<IReadOnlyList<IHostTool>> callback)
        => resolveToolsUsing = _ => callback();

    public void ResolveToolsUsing(Func<object?, IReadOnlyList<IHostTool>> callback)
        => resolveToolsUsing = callback;

    public void PolicyUsing(Func<object?, string?> callback) => policyUsing = callback;

    public void VisitorIdUsing(Func<object?, string> callback) => visitorIdUsing = callback;

    public void MintTokenUsing(Func<object?, Task<string>> callback) => mintTokenUsing = callback;

    public void VerifyBearerUsing(Func<string, Task<HostAuth?>> callback) => verifyBearerUsing = callback;

    public void AuthorizeUsing(Func<object?, bool> callback) => authorizeUsing = callback;

    public void AfterAuthenticateUsing(Func<object?, Task> callback) => afterAuthenticateUsing = callback;

    public void RegisterTool(IHostTool tool) => registeredTools.Add(tool);

    public IReadOnlyList<IHostTool> ResolveTools() => ResolveTools(null);

    public IReadOnlyList<IHostTool> ResolveTools(object? user)
    {
        if (resolveToolsUsing is not null)
        {
            return resolveToolsUsing(user);
        }

        return registeredTools;
    }

    public string? PolicyFor(object? user)
    {
        if (policyUsing is null)
        {
            return null;
        }

        var value = policyUsing(user);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    public bool IsConfigured()
        => !string.IsNullOrWhiteSpace(options.BaseUrl) && !string.IsNullOrWhiteSpace(options.HostApiKey);

    public string McpPublicUrl(string? requestOrigin)
    {
        if (!string.IsNullOrWhiteSpace(options.McpUrl))
        {
            return TrimSlash(options.McpUrl);
        }

        var origin = TrimSlash(requestOrigin);
        if (origin.Length == 0)
        {
            return NormalizePath(options.McpPath);
        }

        return origin + NormalizePath(options.McpPath);
    }

    public async Task<SvedaHostSession> StartSessionAsync(object? user, string? requestOrigin = null, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            throw new SvedaConfigurationException("Set SVEDA_CLIENT_BASE_URL and SVEDA_CLIENT_HOST_API_KEY.");
        }

        var mcpToken = await (mintTokenUsing ?? (u => Task.FromResult(DefaultMintToken(u)))).Invoke(user).ConfigureAwait(false);
        var visitorId = VisitorId(user);
        var mcpUrl = McpPublicUrl(requestOrigin);

        using var client = new SvedaClient(new SvedaClientOptions
        {
            BaseUrl = TrimSlash(options.BaseUrl),
            HostApiKey = options.HostApiKey.Trim(),
            HttpMessageHandler = options.HttpMessageHandler,
        });

        return await client.StartHostSessionAsync(new CreateTokenRequest
        {
            VisitorId = visitorId,
            HostMcpUrl = mcpUrl,
            HostMcpToken = mcpToken,
            Policy = PolicyFor(user),
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<HostAuth?> AuthenticateBearerTokenAsync(string? bearerToken)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return null;
        }

        return await (verifyBearerUsing ?? (token => Task.FromResult<HostAuth?>(tokenStore.Verify(token, options.McpAbility))))
            .Invoke(bearerToken.Trim())
            .ConfigureAwait(false);
    }

    public static string ReadBearerToken(string? authorization)
    {
        if (string.IsNullOrWhiteSpace(authorization))
        {
            return "";
        }

        const string prefix = "Bearer ";
        return authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[prefix.Length..].Trim()
            : "";
    }

    private string VisitorId(object? user)
    {
        if (visitorIdUsing is not null)
        {
            return visitorIdUsing(user);
        }

        var id = user is IReadOnlyDictionary<string, object?> map && map.TryGetValue("id", out var value)
            ? Convert.ToString(value) ?? "anonymous"
            : "anonymous";

        return $"{options.VisitorPrefix}-{id}";
    }

    public string DefaultMintToken(object? user)
    {
        var userId = user is IReadOnlyDictionary<string, object?> map && map.TryGetValue("id", out var value)
            ? Convert.ToString(value) ?? "anonymous"
            : "anonymous";

        tokenStore.RevokeForUser(userId);
        return tokenStore.Mint(userId, options.McpAbility);
    }

    private static HostManagerOptions Normalize(HostManagerOptions source)
    {
        return new HostManagerOptions
        {
            BaseUrl = TrimSlash(source.BaseUrl),
            HostApiKey = (source.HostApiKey ?? "").Trim(),
            McpUrl = TrimSlash(source.McpUrl),
            McpPath = NormalizePath(source.McpPath),
            ServerName = string.IsNullOrWhiteSpace(source.ServerName) ? "Host Application" : source.ServerName,
            ServerVersion = string.IsNullOrWhiteSpace(source.ServerVersion) ? "0.1.0" : source.ServerVersion,
            Instructions = source.Instructions ?? "",
            McpAbility = string.IsNullOrWhiteSpace(source.McpAbility) ? "sveda:mcp" : source.McpAbility,
            TokenTtlSeconds = Math.Max(60, source.TokenTtlSeconds),
            VisitorPrefix = string.IsNullOrWhiteSpace(source.VisitorPrefix) ? "host" : source.VisitorPrefix,
            HttpMessageHandler = source.HttpMessageHandler,
        };
    }

    private static string TrimSlash(string value)
    {
        var trimmed = (value ?? "").Trim();
        while (trimmed.EndsWith('/'))
        {
            trimmed = trimmed[..^1];
        }

        return trimmed;
    }

    private static string NormalizePath(string path)
    {
        var normalized = string.IsNullOrWhiteSpace(path) ? "/mcp/sveda" : path.Trim();
        return normalized.StartsWith('/') ? normalized : "/" + normalized;
    }
}
