using System.Text.Json.Nodes;

namespace Sveda.Client;

public sealed class EmbedResource
{
    private readonly SvedaHttp http;

    internal EmbedResource(SvedaHttp http)
    {
        this.http = http;
    }

    public async Task<EmbedTokenResponse> CreateTokenAsync(CreateTokenRequest? request = null, CancellationToken cancellationToken = default)
    {
        var payload = JsonUtil.ToTokenPayload(request ?? new CreateTokenRequest());
        var json = await http.RequestJsonAsync(HttpMethod.Post, "/sveda/embed/token", payload, cancellationToken).ConfigureAwait(false);
        return JsonUtil.ToEmbedToken(json);
    }

    public Task<JsonNode> ConfigAsync(CancellationToken cancellationToken = default)
    {
        return http.RequestJsonAsync(HttpMethod.Get, "/sveda/embed/config", cancellationToken: cancellationToken);
    }
}
