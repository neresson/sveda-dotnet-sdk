namespace Sveda.Host;

public sealed class HostAuth
{
    public HostAuth(string userId)
    {
        UserId = userId;
        User = new Dictionary<string, object?> { ["id"] = userId };
    }

    public string UserId { get; }

    public object User { get; }
}
