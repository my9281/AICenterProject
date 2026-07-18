using System.Security.Claims;
using System.Threading.RateLimiting;
using AICenterProject.Auth;
using AICenterProject.Models;
using AICenterProject.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<UserStore>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddAuthentication("Bearer").AddScheme<AuthenticationSchemeOptions, BearerAuthenticationHandler>("Bearer", null);
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("auth", limiter =>
{
    limiter.Window = TimeSpan.FromMinutes(1);
    limiter.PermitLimit = 10;
    limiter.QueueLimit = 0;
    limiter.AutoReplenishment = true;
}));

var app = builder.Build();
var store = app.Services.GetRequiredService<UserStore>();
await store.InitializeAsync();

var adminName = builder.Configuration["Superuser:Username"];
var adminPassword = builder.Configuration["Superuser:Password"];
if (!string.IsNullOrWhiteSpace(adminName) && !string.IsNullOrWhiteSpace(adminPassword) && await store.FindByUsernameAsync(adminName) is null)
{
    await store.AddAsync(new User
    {
        Username = adminName.Trim(),
        PasswordHash = app.Services.GetRequiredService<PasswordService>().Hash(adminPassword),
        Note = "系统超级用户",
        Superuser = true
    });
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "AICenter User API", status = "running" }));

app.MapPost("/api/auth/register", async (RegisterRequest request, UserStore users, PasswordService passwords) =>
{
    var username = request.Username?.Trim() ?? "";
    if (username.Length is < 3 or > 50) return Results.ValidationProblem(new Dictionary<string, string[]> { ["username"] = ["用户名长度必须为 3-50 个字符"] });
    if (request.Password?.Length is < 8 or > 128) return Results.ValidationProblem(new Dictionary<string, string[]> { ["password"] = ["密码长度必须为 8-128 个字符"] });
    if (request.Note?.Length > 500) return Results.ValidationProblem(new Dictionary<string, string[]> { ["note"] = ["备注不能超过 500 个字符"] });
    var user = new User { Username = username, PasswordHash = passwords.Hash(request.Password), Note = request.Note?.Trim(), Superuser = false };
    return await users.AddAsync(user) ? Results.Created($"/api/users/{user.UserId}", ToResponse(user)) : Results.Conflict(new { message = "用户名已存在" });
}).RequireRateLimiting("auth");

app.MapPost("/api/auth/login", async (LoginRequest request, UserStore users, PasswordService passwords, TokenService tokens) =>
{
    var user = await users.FindByUsernameAsync(request.Username?.Trim() ?? "");
    if (user is null || !passwords.Verify(request.Password ?? "", user.PasswordHash)) return Results.Json(new { message = "用户名或密码错误" }, statusCode: 401);
    var session = tokens.Create(user.UserId);
    return Results.Ok(new LoginResponse(session.Token, session.ExpiresAt, ToResponse(user)));
}).RequireRateLimiting("auth");

app.MapGet("/api/users/me", async (ClaimsPrincipal principal, UserStore users) =>
{
    var id = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var user = await users.FindByIdAsync(id);
    return user is null ? Results.NotFound() : Results.Ok(ToResponse(user));
}).RequireAuthorization();

app.MapGet("/api/admin/users", async (UserStore users) => Results.Ok((await users.GetAllAsync()).Select(ToResponse)))
    .RequireAuthorization(policy => policy.RequireRole("Superuser"));

app.Run();

static UserResponse ToResponse(User user) => new(user.UserId, user.Username, user.Note, user.Superuser, user.CreatedAt);

public partial class Program;
