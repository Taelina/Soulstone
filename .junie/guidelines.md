# Soulstone Contributor & AI Agent Guidelines

These guidelines define the engineering standards, coding conventions, architectural principles, and testing practices for developing and maintaining the **Soulstone** ecosystem. All automated agents and human contributors must adhere to these best practices.

---

## 1. Project Architecture & Repository Layout

The repository is structured as a multi-target solution supporting both an in-game Dalamud client plugin and a high-performance standalone synchronization relay.

```
Soulstone/
├── Soulstone/                    # Dalamud Plugin (net10.0-windows)
│   ├── Datamodels/               # Core domain entities & immutable records
│   ├── Managers/                 # Single-instance state & lifecycle managers
│   ├── Sync/                     # Relay client, WebSocket protocol & cryptographic handlers
│   ├── Utils/                    # Mathematical parser, ImGui widgets, chat helpers
│   ├── Windows/                  # ImGui window presentation layer
│   └── Localizations/            # Embedded language catalogs (en.json, fr.json)
├── Soulstone.SyncServer/         # Standalone ASP.NET Core WebSocket Relay (net8.0)
│   ├── Program.cs                # Minimal API configuration, DI, rate limiting & pipeline
│   ├── SessionRegistry.cs        # In-memory thread-safe session & invite registry
│   ├── SessionCleanupService.cs  # Background hosted service for expired sessions
│   └── WebSocketRelay.cs         # Low-latency WebSocket multiplexer & relay handler
├── Soulstone.Tests/              # Unit & integration tests for Dalamud client
├── Soulstone.SyncServer.Tests/   # Integration tests for ASP.NET Core relay server
├── Localizations/                # Root copy of localization catalogs
└── docs/                         # Technical specifications and architecture documentation
```

### Framework & Target Alignment
- **Soulstone Client**: Targets `net10.0-windows` with C# 14 / modern language features. Interacts with Dalamud SDK, ImGui, and ECommons.
- **Soulstone.SyncServer**: Targets `net8.0` (LTS) using ASP.NET Core Minimal APIs and WebSockets. Kept strictly decoupled from Dalamud and game APIs.
- **Test Projects**: Target their respective platform frameworks (`net10.0-windows` and `net8.0`).

---

## 2. Modern C# Language Conventions & Best Practices

All C# code in this repository must leverage modern language features for clarity, type safety, memory efficiency, and conciseness.

### Nullability & Type Safety
- **Nullable Reference Types**: `#nullable enable` is globally activated. Treat compiler warnings as errors.
- **Zero Null-Forgiving Operators**: Avoid `!` unless working with verified framework lifecycle invariants. Use pattern matching, null-coalescing (`??`, `??=`), and null-conditional (`?.`) operators.
- **Defensive Arguments**: Use `ArgumentNullException.ThrowIfNull(arg)` and `ArgumentException.ThrowIfNullOrWhiteSpace(str)` for parameter validation.

### Modern Syntax & Expressions
- **File-Scoped Namespaces**: Always use file-scoped namespaces (`namespace Soulstone.Managers;`).
- **Primary Constructors**: Use primary constructors for classes and structs where dependency injection or direct field initialization is straightforward.
- **Collection Expressions**: Use collection expressions (`[item1, item2]`, `[]`) instead of verbose `new List<T>()` or `new T[] { ... }`.
- **Pattern Matching**: Utilize relational patterns, property patterns, `switch` expressions, and `is` type checks.
- **Record Types**: Use `record` or `record struct` for data transfer objects (DTOs), API models, and immutable protocol packets.
- **Raw String Literals**: Use multi-line raw string literals (`"""..."""`) for JSON payloads, embedded templates, and regex expressions.

### Memory & Performance Optimization
- **Read-Only Structures**: Use `readonly struct` or `ref struct` for high-frequency temporary stack allocations.
- **Spans & Memory**: Prefer `ReadOnlySpan<T>` and `ReadOnlyMemory<T>` for parsing strings, binary buffers, and slices to eliminate unnecessary heap allocations.
- **String Handling**: Use `string.Create`, `StringBuilder`, or string interpolation with formatters for complex formatting; avoid excessive string concatenation in hot paths.
- **Avoid Boxing**: Use generic constraints (`where T : struct`) and avoid casting value types to `object` or interfaces in loops.

---

## 3. ASP.NET Core REST API & Relay Server Standards

The `Soulstone.SyncServer` provides REST endpoints for session negotiation and WebSockets for real-time state relay.

### REST API Design Norms
- **Resource-Oriented URIs**: Use lowercase, pluralized kebab-case or plural noun paths (e.g., `/api/sessions`, `/api/sessions/{sessionId}/invite`, `/api/invites/{inviteId}`).
- **Semantic HTTP Verbs**:
  - `GET`: Safe, idempotent read operations.
  - `POST`: Create a new resource (e.g., creating a session).
  - `PUT`: Idempotent resource replacement or explicit registration.
  - `DELETE`: Idempotent resource removal.
- **Standard HTTP Status Codes**:
  - `200 OK`: Successful retrieval or synchronous operation result.
  - `201 Created`: Successful creation (include `Location` header where appropriate).
  - `204 NoContent`: Successful mutation returning no payload.
  - `400 BadRequest`: Malformed syntax, invalid lengths, or schema validation failures.
  - `401 Unauthorized`: Missing or invalid Bearer authentication header.
  - `403 Forbidden`: Authenticated identity lacks permission for the resource.
  - `404 NotFound`: Requested session, invite, or endpoint does not exist.
  - `409 Conflict`: Resource collision (e.g., invite already claimed or registered).
  - `429 TooManyRequests`: Rate limit exceeded.
  - `500 InternalServerError`: Unhandled infrastructure errors.
- **Typed Results**: Return strongly-typed endpoint results (`TypedResults.Ok(...)`, `TypedResults.NotFound()`, etc.) in Minimal APIs to facilitate OpenAPI generation and testing.
- **Request / Response DTOs**: Never expose internal storage entities directly through the API. Use dedicated, immutable request and response records.

### WebSocket Relay Norms
- **Connection Handshake**: Validate headers, session presence, and bearer tokens before accepting the WebSocket connection.
- **Keep-Alives**: Configure explicit `KeepAliveInterval` on `WebSocketOptions` to detect dead client connections.
- **Graceful Termination**: Always observe cancellation tokens, handle `WebSocketCloseStatus.NormalClosure` cleanly, and ensure unclosed sockets release buffer resources.
- **Multiplexing & Backpressure**: Forward messages asynchronously using pooled buffers (`ArrayPool<byte>.Shared` or `Memory<byte>`) without unbounded queue accumulation.

### Infrastructure, Security & Reliability
- **Rate Limiting**: Apply ASP.NET Core RateLimiter policies (`FixedWindowRateLimiterOptions` or `SlidingWindowRateLimiterOptions`) on sensitive or expensive creation endpoints.
- **Time Abstraction**: Always inject `TimeProvider.System` (or a mock `TimeProvider`) instead of relying directly on `DateTime.UtcNow`, ensuring deterministic time-based testing.
- **Hosted Services**: Implement long-running background maintenance (such as session expiration cleanup) via `BackgroundService` / `IHostedService` with robust `CancellationToken` handling.
- **Zero-Persistence & Privacy**: The relay server is a blind conduit. **NEVER** persist or log invite codes, passwords, private keys, authentication tokens, or unencrypted character payloads.

---

## 4. Dalamud Plugin & ImGui UI Best Practices

Code within `Soulstone/` runs inside the Final Fantasy XIV game process and requires strict UI safety and thread affinity.

### ImGui Immediate-Mode Safety
- **RAII Pattern**: Always use `Dalamud.Interface.Utility.Raii.ImRaii` (`ImRaii.PushId`, `ImRaii.PushColor`, `ImRaii.PushStyleVar`, `ImRaii.Child`, `ImRaii.Table`, `ImRaii.Group`) for every pushable ImGui state. Never call raw `ImGui.Pop*` manually where `ImRaii` can guarantee stack unwinding across exceptions and early returns.
- **Unique Identification**: Ensure all dynamic list items, table rows, and interactive buttons push unique string or numerical IDs via `ImRaii.PushId`.
- **Zero Heavy Work on UI Thread**: Never perform network requests, file I/O, or CPU-intensive math inside the `Window.Draw()` loop. Offload background operations via tasks and dispatch state changes back to the UI thread.

### Formula Parsing & Math Evaluation
- **Recursive Descent Parser**: Mathematical expressions (e.g., `@STR * 2 + 10`, dice formulas) must be evaluated through `StatFormulaEvaluator` or dedicated AST parsers.
- **No Dynamic Compilation**: Avoid dynamic C# code execution or untrusted reflection; retain sandboxed arithmetic evaluation.

### Internationalization & Localization
- **Bilingual Consistency**: All user-facing strings must be routed through `LocalizationManager`.
- **Synchronized Updates**: Any change or addition of UI text must be reflected simultaneously in both `Localizations/en.json` and `Localizations/fr.json`.

---

## 5. Code Style & Formatting Rules

- **Indentation**: 4 spaces (no tabs).
- **Line Endings**: LF line endings.
- **Encoding**: UTF-8 without BOM (with final newline). `.editorconfig` is the authoritative source.
- **Naming Conventions**:
  - `PascalCase`: Classes, records, structs, interfaces (`IInterfaceName`), enums, methods, properties, public fields.
  - `camelCase`: Method parameters, local variables.
  - `_camelCase`: Private instance fields.
- **Minimal Changes**: Keep edits focused and atomic. Never reformat unrelated files or reorder existing untouched members.

---

## 6. Testing & Quality Assurance Standards

### Test Frameworks & Libraries
- **Unit Testing**: [xUnit.net](https://xunit.net/) (`[Fact]`, `[Theory]`, `[InlineData]`).
- **Assertions**: [FluentAssertions](https://fluentassertions.com/) (`result.Should().Be(...)`, `action.Should().Throw<...>()`).
- **Mocking**: [Moq](https://github.com/devlooped/moq) — use exclusively at collaboration boundaries and interface contracts; do not mock simple datamodels or pure functions.
- **API Integration Testing**: `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) for end-to-end endpoint and WebSocket tests.

### Test Structure & Guidelines
- **Naming Convention**: `MethodUnderTest_StateOrCondition_ExpectedBehavior` (e.g., `RegisterInvite_WhenSessionExists_ReturnsNoContent`).
- **Deterministic Time**: Inject fake or controlled `TimeProvider` instances in tests to verify timeouts, expirations, and rate limiters without arbitrary `Thread.Sleep` or `Task.Delay`.
- **Regression Testing**: Every bug fix must be accompanied by a regression test replicating the failure prior to the fix.

### Test Execution Commands
Run tests from the repository root using PowerShell:

```powershell
# Plugin client unit tests
dotnet test Soulstone.Tests/Soulstone.Tests.csproj

# Relay server integration tests
dotnet test Soulstone.SyncServer.Tests/Soulstone.SyncServer.Tests.csproj

# Full solution build verification
dotnet build Soulstone.sln
```

---

## 7. Security, Privacy & Sensitive Data Handling

- **Zero-Knowledge Architecture**: Encryption keys (AES-GCM, RSA) and decrypted payload contents remain solely on the client.
- **No Logging of Secrets**: Never log or output Bearer tokens, session IDs, private keys, encryption secrets, or full character data in logs, exceptions, or console output.
- **Sanitized Chat Output**: When echoing rolls or messages to the in-game chat, sanitize inputs to prevent format injection or unintended command triggering.
