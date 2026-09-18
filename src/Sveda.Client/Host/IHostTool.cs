namespace Sveda.Host;

public interface IHostTool
{
    string Name { get; }

    string Description { get; }

    IReadOnlyDictionary<string, object?> InputSchema { get; }

    string Mode { get; }

    string Domain { get; }

    Task<object?> HandleAsync(IReadOnlyDictionary<string, object?> arguments, HostCallContext context, CancellationToken cancellationToken = default);
}
