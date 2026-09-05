using Soulstone.SyncServer;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff zzz ";
});
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
if (string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
    builder.WebHost.UseUrls("http://127.0.0.1:5077");

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<SessionRegistry>();
builder.Services.AddHostedService<SessionCleanupService>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("session-creation", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

string configuredUrls = builder.Configuration["urls"] ?? "http://127.0.0.1:5077";
if (!app.Environment.IsDevelopment() && configuredUrls.Contains("https://", StringComparison.OrdinalIgnoreCase))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2),
});
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/api/sessions", (SessionRegistry sessions) =>
{
    var session = sessions.Create();
    return Results.Ok(session);
}).RequireRateLimiting("session-creation");

app.MapPut("/api/sessions/{sessionId}/invite", (
    string sessionId,
    InviteRegistrationRequest request,
    HttpContext context,
    SessionRegistry sessions) =>
{
    if (string.IsNullOrWhiteSpace(request.InviteId) || request.InviteId.Length > 128 ||
        string.IsNullOrWhiteSpace(request.Payload) || request.Payload.Length > 8192)
        return Results.BadRequest();

    string authorization = context.Request.Headers.Authorization.ToString();
    const string bearerPrefix = "Bearer ";
    if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        return Results.Unauthorized();

    return sessions.TryRegisterInvite(sessionId, authorization[bearerPrefix.Length..], request.InviteId, request.Payload) switch
    {
        InviteRegistrationResult.Success => Results.NoContent(),
        InviteRegistrationResult.NotFound => Results.NotFound(),
        InviteRegistrationResult.Unauthorized => Results.Unauthorized(),
        InviteRegistrationResult.Conflict => Results.Conflict(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };
});

app.MapGet("/api/invites/{inviteId}", (string inviteId, SessionRegistry sessions) =>
    sessions.TryResolveInvite(inviteId, out string? payload)
        ? Results.Ok(new InviteResolutionResponse(payload!))
        : Results.NotFound());

app.Map("/api/sessions/{sessionId}/connect", WebSocketRelay.HandleAsync);

app.Logger.LogInformation("Soulstone relay starting; payloads and credentials are never logged or persisted");
app.Run();

public partial class Program;