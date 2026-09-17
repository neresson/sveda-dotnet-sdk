namespace Sveda.Client;

public sealed class ChatResource
{
    private readonly SvedaHttp http;

    internal ChatResource(SvedaHttp http)
    {
        this.http = http;
    }

    public async Task<MessageResponse> CreateAsync(object payload, CancellationToken cancellationToken = default)
    {
        var json = await http.RequestJsonAsync(HttpMethod.Post, "/sveda/message", payload, cancellationToken).ConfigureAwait(false);
        return new MessageResponse(json);
    }

    public async IAsyncEnumerable<StreamEvent> CreateStreamedAsync(object payload, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var response = await http.RequestStreamAsync(HttpMethod.Post, "/sveda/stream", payload, cancellationToken).ConfigureAwait(false);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);
        var parser = new StreamParser();
        await foreach (var parsed in parser.IterateAsync(reader, cancellationToken).ConfigureAwait(false))
        {
            yield return parsed;
        }
    }
}
