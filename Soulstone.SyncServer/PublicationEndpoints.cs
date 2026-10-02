using System.Buffers;
using System.Text;
using System.Text.Json;

namespace Soulstone.SyncServer;

internal static class PublicationEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaximumRequestBytes = PublicationSecurity.MaximumPayloadBytes + 16 * 1024;

    public static void MapPublications(this WebApplication app)
    {
        app.MapPut("/api/characters/{characterName}/{worldName?}", async (
            string characterName, string? worldName, HttpRequest request, CharacterSheetRegistry sheets, CancellationToken ct) =>
        {
            var token = PublicationSecurity.ReadToken(request);
            if (token == null)
                return Results.Unauthorized();
            var payload = await ReadBodyAsync(request, PublicationSecurity.MaximumPayloadBytes, ct);
            if (payload == null)
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            return ToResult(sheets.Store(characterName, worldName, payload, token));
        }).RequireRateLimiting("publication-write").WithSummary("Publish a public character profile using a private ownership credential.");

        app.MapGet("/api/characters/{characterName}/{worldName?}", (
            string characterName, string? worldName, CharacterSheetRegistry sheets) =>
            sheets.TryGet(characterName, worldName, out var payload)
                ? Results.Content(payload, "application/json") : Results.NotFound());

        app.MapDelete("/api/characters/{characterName}/{worldName?}", (
            string characterName, string? worldName, HttpRequest request, CharacterSheetRegistry sheets) =>
        {
            var token = PublicationSecurity.ReadToken(request);
            return token == null ? Results.Unauthorized() : ToResult(sheets.Delete(characterName, worldName, token));
        }).RequireRateLimiting("publication-write").WithSummary("Delete a profile using its ownership credential.");

        app.MapPost("/api/dice-systems", async (HttpRequest request, DiceSystemRegistry systems, CancellationToken ct) =>
        {
            var token = PublicationSecurity.ReadToken(request);
            if (token == null)
                return Results.Unauthorized();
            var json = await ReadBodyAsync(request, MaximumRequestBytes, ct);
            if (json == null)
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            PublishDiceSystemRequest? publication;
            try { publication = JsonSerializer.Deserialize<PublishDiceSystemRequest>(json, JsonOptions); }
            catch (JsonException) { return Results.BadRequest(); }
            if (publication == null)
                return Results.BadRequest();
            var result = systems.Publish(publication, token, out var published);
            return result == RegistryWriteResult.Success ? Results.Ok(published) : ToResult(result);
        }).RequireRateLimiting("publication-write").WithSummary("Publish or update a ruleset using a private ownership credential.");

        app.MapGet("/api/dice-systems/{code}", (string code, DiceSystemRegistry systems) =>
            systems.TryGet(code, out var published) ? Results.Ok(published) : Results.NotFound());
        app.MapGet("/api/dice-systems/{code}/version", (string code, DiceSystemRegistry systems) =>
            systems.TryGetVersion(code, out var version) ? Results.Ok(version) : Results.NotFound());
    }

    private static IResult ToResult(RegistryWriteResult result) => result switch
    {
        RegistryWriteResult.Success => Results.NoContent(),
        RegistryWriteResult.Unauthorized => Results.StatusCode(StatusCodes.Status403Forbidden),
        RegistryWriteResult.NotFound => Results.NotFound(),
        RegistryWriteResult.Full => Results.StatusCode(StatusCodes.Status503ServiceUnavailable),
        _ => Results.BadRequest(),
    };

    private static async Task<string?> ReadBodyAsync(HttpRequest request, int maximumBytes, CancellationToken ct)
    {
        if (request.ContentLength > maximumBytes)
            return null;
        var buffer = ArrayPool<byte>.Shared.Rent(16 * 1024);
        try
        {
            using var body = new MemoryStream();
            int count;
            while ((count = await request.Body.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, maximumBytes + 1 - (int)body.Length)), ct)) > 0)
            {
                if (body.Length + count > maximumBytes)
                    return null;
                body.Write(buffer, 0, count);
            }
            return Encoding.UTF8.GetString(body.GetBuffer(), 0, (int)body.Length);
        }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
    }
}
