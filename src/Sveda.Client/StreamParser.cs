using System.Text.Json;

namespace Sveda.Client;

public sealed class StreamParser
{
    public const string SseDoneLine = "data: [DONE]";

    private static readonly HashSet<string> StreamEvents =
    [
        "message.start",
        "text.delta",
        "reasoning.delta",
        "tool.call",
        "tool.result",
        "tool.progress",
        "context.usage",
        "chat.title",
        "max_steps",
        "message.end",
        "error",
    ];

    public IEnumerable<StreamEvent> Iterate(string content)
    {
        using var reader = new StringReader(content);
        return Iterate(reader).ToList();
    }

    public async IAsyncEnumerable<StreamEvent> IterateAsync(TextReader reader, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                yield break;
            }

            var parsed = ParseLine(line);
            if (parsed is not null)
            {
                yield return parsed;
            }
        }
    }

    public IEnumerable<StreamEvent> Iterate(TextReader reader)
    {
        while (reader.ReadLine() is { } line)
        {
            var parsed = ParseLine(line);
            if (parsed is not null)
            {
                yield return parsed;
            }
        }
    }

    public StreamEvent? ParseLine(string line)
    {
        var trimmed = line.Trim();
        if (!trimmed.StartsWith("data:", StringComparison.Ordinal))
        {
            return null;
        }

        var payload = trimmed[5..].Trim();
        if (payload.Length == 0 || payload == "[DONE]")
        {
            return null;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(payload);
        }
        catch (JsonException)
        {
            return null;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!root.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var type = typeElement.GetString();
            if (string.IsNullOrEmpty(type) || !StreamEvents.Contains(type))
            {
                return null;
            }

            return new StreamEvent(type, root.Clone());
        }
    }
}
