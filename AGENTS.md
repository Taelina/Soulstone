# Soulstone contributor guidance

## Repository layout

- `Soulstone/` is the Dalamud plugin (`net10.0-windows`). Keep game-facing UI,
  managers, models, synchronization client code, and plugin configuration here.
- `Soulstone.SyncServer/` is the standalone ASP.NET Core WebSocket relay
  (`net8.0`). Keep it independent from Dalamud and game APIs.
- `Soulstone.Tests/` covers the plugin with xUnit, FluentAssertions, and Moq.
- `Soulstone.SyncServer.Tests/` covers the relay server.
- `Localizations/` and `Soulstone/Localizations/` contain English and French UI
  strings. Update both languages when introducing user-facing text.
- `docs/` contains architecture and deployment documentation.

## Development conventions

- Use four spaces, LF line endings, UTF-8, and a final newline; `.editorconfig`
  is authoritative.
- C# uses nullable reference types and implicit usings. Follow the existing
  namespace, naming, and dependency-injection patterns in the area being changed.
- Make the smallest coherent change. Do not reformat unrelated code or overwrite
  existing uncommitted work.
- Keep server protocol/data-model changes compatible with the plugin and covered
  by tests on both sides.
- Treat sync payloads and character data as sensitive: do not log invite codes,
  credentials, encryption material, or full private character data.

## Testing

Run the narrowest relevant test project first, then the full suite when practical:

```powershell
dotnet test Soulstone.Tests/Soulstone.Tests.csproj
dotnet test Soulstone.SyncServer.Tests/Soulstone.SyncServer.Tests.csproj
dotnet build Soulstone.sln
```

Tests for new behavior should use xUnit and FluentAssertions; use Moq only where
collaboration boundaries require it. Add regression coverage for bug fixes.

## Before handing off

- Confirm localization keys and both language files for UI changes.
- Confirm protocol and serialization compatibility for synchronization changes.
- Report commands run and any validation that could not be performed.
