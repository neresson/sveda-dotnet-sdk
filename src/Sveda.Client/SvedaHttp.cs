using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sveda.Client;

internal sealed class SvedaHttp
{
    internal static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient http;
    private readonly SvedaClientOptions options;

    public SvedaHttp(HttpClient http, SvedaClientOptions options)
    {
        this.http = http;
        this.options = options;
    }

    public async Task<JsonNode> RequestJsonAsync(HttpMethod method, string uri, object? payload = null, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(method, uri, payload, "application/json");
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw new SvedaTransportException(exception.Message, exception);
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return DecodeResponse(response, body);
        }
    }

    public async Task<HttpResponseMessage> RequestStreamAsync(HttpMethod method, string uri, object? payload, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(method, uri, payload, "application/vnd.sveda.stream+json");
        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw new SvedaTransportException(exception.Message, exception);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            DecodeResponse(response, body);
            return response;
        }
        finally
        {
            response.Dispose();
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri, object? payload, string accept)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

        var hostApiKey = options.HostApiKey?.Trim();
        if (!string.IsNullOrEmpty(hostApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", hostApiKey);
        }

        var embedToken = options.EmbedToken?.Trim();
        if (!string.IsNullOrEmpty(embedToken))
        {
            request.Headers.TryAddWithoutValidation("X-Sveda-Embed-Token", embedToken);
        }

        if (payload is not null && method != HttpMethod.Get && method != HttpMethod.Delete && method != HttpMethod.Head)
        {
            var json = JsonSerializer.Serialize(payload, SerializerOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private static JsonNode DecodeResponse(HttpResponseMessage response, string body)
    {
        var status = (int)response.StatusCode;
        if (status is 401 or 403)
        {
            throw new SvedaAuthenticationException($"Sveda API authentication failed with status {status}");
        }

        if (status < 200 || status >= 300)
        {
            string message = $"Sveda API request failed with status {status}";
            try
            {
                var decoded = JsonNode.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                if (decoded?["message"]?.GetValue<string>() is { Length: > 0 } apiMessage)
                {
                    message = apiMessage;
                }
            }
            catch (JsonException)
            {
            }

            throw new SvedaApiException(message, status, body);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return new JsonObject();
        }

        try
        {
            return JsonNode.Parse(body) ?? new JsonObject();
        }
        catch (JsonException exception)
        {
            throw new SvedaUnserializableResponseException("Unable to decode Sveda API response as JSON.", exception);
        }
    }
}
