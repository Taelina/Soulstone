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

app.Map("/api/sessions/{sessionId}/connect", WebSocketRelay.HandleAsync);

app.Logger.LogInformation("Soulstone relay starting; payloads and credentials are never logged or persisted");
app.Run();

public partial class Program;