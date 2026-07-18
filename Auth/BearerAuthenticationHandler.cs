using System.Security.Claims;
using System.Text.Encodings.Web;
using AICenterProject.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AICenterProject.Auth;

public sealed class BearerAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly TokenService _tokens;
    private readonly UserStore _users;

    public BearerAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, TokenService tokens, UserStore users) : base(options, logger, encoder)
        => (_tokens, _users) = (tokens, users);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();
        var userId = _tokens.Validate(header[7..].Trim());
        if (userId is null) return AuthenticateResult.Fail("Token 无效或已过期");
        var user = await _users.FindByIdAsync(userId.Value);
        if (user is null) return AuthenticateResult.Fail("用户不存在");
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Superuser ? "Superuser" : "User")
        };
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name)), Scheme.Name));
    }
}
