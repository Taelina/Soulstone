# Soulstone Sync Server — 1.4.0

Standalone ASP.NET Core 8 API and WebSocket relay for Soulstone. There is no
Dalamud dependency, database, interactive setup, or durable publication store.
It forwards encrypted party messages and stores public profiles, shared rulesets,
invites, and rooms in memory. Restarting clears all of them.

## Deploy on Windows

Follow the [Windows deployment guide](../docs/DEPLOYMENT.md) in order. It covers
installation, boot startup through Task Scheduler, failure recovery, direct HTTP,
upgrades, rollback, and troubleshooting. This console executable needs Task
Scheduler or a service wrapper; sc.exe create alone does not make it a service.

From the repository root:

~~~powershell
dotnet test .\Soulstone.SyncServer.Tests\Soulstone.SyncServer.Tests.csproj
dotnet publish .\Soulstone.SyncServer\Soulstone.SyncServer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\Soulstone.SyncServer\bin\publish\win-x64
& '.\Soulstone.SyncServer\bin\publish\win-x64\Soulstone.SyncServer.exe' --urls 'http://0.0.0.0:5077'
~~~

Copy the complete output, including configuration and native supporting files.
Self-contained output needs no installed runtime on the host. Development-only
startup: dotnet run --project Soulstone.SyncServer.

The default listener is http://0.0.0.0:5077. Override with --urls or
ASPNETCORE_URLS. The main deployment uses direct HTTP/WS on port 5077, including
router forwarding for internet access. Optional HTTPS proxy/tunnel instructions
remain in the guide; those use a loopback HTTP listener. appsettings.json is loaded from the
executable directory, not the terminal's working directory.

## API surface

| Method and route | Purpose | Authorization |
| --- | --- | --- |
| GET /health | HTTP 200 with {"status":"healthy"} | Public |
| POST /api/sessions | Returns sessionId, hostToken, memberToken | Public; 10/minute per observed IP |
| PUT /api/sessions/{sessionId}/invite | Registers inviteId/payload JSON; success 204 | Host bearer token |
| GET /api/invites/{inviteId} | Returns encrypted invite payload | Public lookup by opaque ID |
| GET /api/sessions/{sessionId}/connect | WebSocket upgrade for group/host envelopes | Host or member bearer token |
| PUT /api/characters/{characterName}[/{worldName}] | Publishes public profile JSON; success 204 | Owner bearer credential |
| GET /api/characters/{characterName}[/{worldName}] | Returns public profile JSON or 404 | Public |
| DELETE /api/characters/{characterName}[/{worldName}] | Deletes profile; success 204 | Owner bearer credential |
| POST /api/dice-systems | Publishes/updates a ruleset; success 200 | Owner bearer credential |
| GET /api/dice-systems/{code} | Downloads publication and serialized payload | Public |
| GET /api/dice-systems/{code}/version | Returns code, systemName, updatedAtUtc | Public |

Brackets denote an optional world segment, not literal brackets. URL-encode
character/world names. World-qualified lookups are exact; unqualified lookups
can return the latest matching name.

Publication credentials are random 32-byte values encoded as 64 hex characters.
First publication claims the resource; updates/deletes need the same credential.
Session tokens and publication credentials serve different purposes.
See [publication contracts and migration](../docs/PUBLICATION_API.md) and
[HTTP examples](Soulstone.SyncServer.http).

## Limits and operations

- Sessions: 12-hour lifetime; empty rooms expire after five minutes; 16 clients
  per room. WebSockets: 64 KiB messages and 20 messages/10 seconds per connection.
- Publications: 2 MiB per payload, 1,024 entries and 64 MiB per registry.
  Profiles expire after seven days; rulesets after 30 days.
- Publication writes/deletes: 60/minute per observed connection IP. Missing or
  invalid credentials return 401; another owner's credential returns 403.
- No forwarded-header middleware: proxy users can share IP-based limits.
- Console logs contain lifecycle/transport events, without credentials or payloads.
  Proxy logging needs the same care.

On direct HTTP, REST profiles and bearer credentials have no TLS transport
encryption. Optional HTTPS adds that protection. The plugin filters private
profile fields before upload; the server
stores submitted JSON and does not independently apply plugin visibility rules.
Do not upload a full private sheet through manual API calls.

Upgrade plugin and server together for 1.4.0. Restart the relay, recreate sessions,
and republish content. Old unauthenticated writes fail. Back up plugin
configuration securely to preserve local ownership credentials.

[Linux and Docker deployment](../docs/DEPLOYMENT.md#7-linux-and-containers)
uses the same server contracts.
