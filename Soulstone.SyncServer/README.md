# Soulstone Sync Server — 1.5.0

Standalone ASP.NET Core 8 API and WebSocket relay for Soulstone, independent of
Dalamud. Public profiles, shared rulesets, and ownership hashes persist in an
embedded encrypted SQLCipher Community database. Rooms and invites remain in
memory; party messages are forwarded without being recorded.

For the free Docker Compose setup, native library build, key creation, and
backups, follow [encrypted storage setup](../docs/ENCRYPTED_STORAGE.md).
The container builds SQLCipher Community from pinned upstream source; no paid
packages or license are required. Configure storage before running natively.

## Deploy on Windows with Docker Desktop

Follow the [Windows deployment guide](../docs/DEPLOYMENT.md) in order. Docker
Desktop must be running in Linux-container mode. The image includes the complete
backend, .NET runtime, and free SQLCipher Community library; no host SDK or
separate database service is required.

Copy the deployment bundle or repository to a stable directory, configure `.env`
with a consistent Compose project name and bind address, then generate the key
once and start the backend:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Soulstone.SyncServer\Initialize-Storage.ps1
docker compose up -d --build --wait --wait-timeout 120
Invoke-RestMethod http://127.0.0.1:5077/health
```

Keep `secrets\publications.key` and the named database volume through updates.
The host port binds to loopback by default; change the `.env` bind setting for
LAN/VPN access or use a host-side HTTPS proxy/tunnel for Internet access.
Docker Desktop startup at sign-in and the container restart policy are separate;
the deployment guide explains both, plus updates, rollback, backup, and restore.

For native Windows/Linux alternatives, see
[storage configuration](../docs/ENCRYPTED_STORAGE.md#configuration-outside-docker).
Native publishing needs SQLCipher binaries and absolute database/key paths;
ordinary `dotnet publish` alone is insufficient. The container handles these.

## API surface

| Method and route | Purpose | Authorization |
| --- | --- | --- |
| GET /health | HTTP 200 with {"status":"healthy"}; 503 if storage is unavailable | Public |
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

Upgrade plugin and server together for 1.5.0. Restart the relay, recreate sessions,
and republish once when migrating from the old in-memory server. Later restarts
preserve publications and their ownership claims. Old unauthenticated writes fail.
Back up plugin
configuration securely to preserve local ownership credentials.

[Windows Docker deployment](../docs/DEPLOYMENT.md)
uses the same server contracts.
