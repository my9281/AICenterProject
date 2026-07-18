namespace AICenterProject.Models;

public sealed class User
{
    public Guid UserId { get; init; } = Guid.NewGuid();
    public required string Username { get; init; }
    public required string PasswordHash { get; set; }
    public string? Note { get; set; }
    public bool Superuser { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record RegisterRequest(string Username, string Password, string? Note);
public sealed record LoginRequest(string Username, string Password);
public sealed record UserResponse(Guid UserId, string Username, string? Note, bool Superuser, DateTimeOffset CreatedAt);
public sealed record LoginResponse(string Token, DateTimeOffset ExpiresAt, UserResponse User);
