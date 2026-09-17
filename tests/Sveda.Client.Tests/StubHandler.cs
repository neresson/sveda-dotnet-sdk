using System.Net;
using System.Text;

namespace Sveda.Client.Tests;

internal sealed class StubHandler : HttpMessageHandler
{
    public List<RecordedRequest> Requests { get; } = [];

    public Func<HttpRequestMessage, string, HttpResponseMessage>? Responder { get; set; }

    public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

    public string JsonBody { get; set; } = "{}";

    public string? StreamBody { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        request.Headers.TryGetValues("X-Sveda-Embed-Token", out var embedValues);
        Requests.Add(new RecordedRequest(
            request.Method.Method,
            request.RequestUri?.AbsolutePath ?? "",
            body,
            request.Headers.Authorization?.ToString(),
            embedValues?.FirstOrDefault()));

        if (Responder is not null)
        {
            return Responder(request, body);
        }

        var content = StreamBody ?? JsonBody;
        var mediaType = StreamBody is null ? "application/json" : "text/event-stream";
        return new HttpResponseMessage(Status)
        {
            Content = new StringContent(content, Encoding.UTF8, mediaType),
        };
    }
}

internal sealed record RecordedRequest(
    string Method,
    string Uri,
    string Body,
    string? Authorization,
    string? EmbedToken);
