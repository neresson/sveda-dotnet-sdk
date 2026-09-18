using System.Security.Cryptography;

namespace Sveda.Host;

public sealed class McpTokenStore
{
    private readonly TimeSpan ttl;
    private readonly Dictionary<string, TokenRecord> tokens = new();
    private readonly object gate = new();

    public McpTokenStore(TimeSpan? ttl = null)
    {
        this.ttl = ttl is null || ttl <= TimeSpan.Zero ? TimeSpan.FromHours(1) : ttl.Value;
    }

    public string Mint(string userId, string ability = "sveda:mcp")
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var expires = DateTimeOffset.UtcNow.Add(ttl);

        lock (gate)
        {
            PruneExpired();
            tokens[token] = new TokenRecord(userId, ability, expires);
        }

        return token;
    }

    public void RevokeForUser(string userId)
    {
        lock (gate)
        {
            foreach (var key in tokens.Where(pair => pair.Value.UserId == userId).Select(pair => pair.Key).ToList())
            {
                tokens.Remove(key);
            }
        }
    }

    public HostAuth? Verify(string token, string requiredAbility)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        lock (gate)
        {
            PruneExpired();
            if (!tokens.TryGetValue(token, out var record) || record.Expires <= DateTimeOffset.UtcNow)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(requiredAbility) && record.Ability != requiredAbility)
            {
                return null;
            }

            return new HostAuth(record.UserId);
        }
    }

    private void PruneExpired()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var key in tokens.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToList())
        {
            tokens.Remove(key);
        }
    }

    private sealed record TokenRecord(string UserId, string Ability, DateTimeOffset Expires);
}
