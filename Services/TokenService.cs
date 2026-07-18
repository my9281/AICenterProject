using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace AICenterProject.Services;

public sealed class TokenService
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);

    public (string Token, DateTimeOffset ExpiresAt) Create(Guid userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var expiresAt = DateTimeOffset.UtcNow.Add(Lifetime);
        _sessions[token] = new Session(userId, expiresAt);
        return (token, expiresAt);
    }

    public Guid? Validate(string token)
    {
        if (!_sessions.TryGetValue(token, out var session)) return null;
        if (session.ExpiresAt > DateTimeOffset.UtcNow) return session.UserId;
        _sessions.TryRemove(token, out _);
        return null;
    }

    private sealed record Session(Guid UserId, DateTimeOffset ExpiresAt);
}
