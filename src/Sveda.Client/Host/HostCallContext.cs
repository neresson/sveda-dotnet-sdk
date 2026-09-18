namespace Sveda.Host;

public sealed class HostCallContext
{
    public object? User { get; init; }

    public IReadOnlyDictionary<string, object?>? PageContext { get; init; }

    public string? ChatId { get; init; }

    public static HostCallContext Anonymous() => new();
}
