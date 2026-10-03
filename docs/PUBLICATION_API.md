# Public profiles and ruleset ownership

Contract for **Soulstone 1.5.0**. For Windows Docker Desktop/Compose installation,
startup, networking, backups, updates, and diagnostics, follow [deployment](DEPLOYMENT.md).

Deploy the plugin and relay server updates together. WebSocket envelope version 1,
integer event identifiers, existing routes, and public download response shapes
are preserved. Full initiative snapshots now require the same host signature as
other initiative commands. Local participant flags and file paths are ignored
when reading legacy snapshots and are no longer sent.

Character PUT/DELETE and ruleset POST requests now require a bearer credential:
a cryptographically random 32-byte value encoded as 64 hexadecimal characters.
The first successful publication claims the profile or ruleset. Later updates
and deletions require the original credential, compared against its SHA-256 hash
in the server registry. Public names and share codes do not authorize writes.
The credential establishes ownership of a publication; it does not verify the
publisher's Final Fantasy XIV account or prevent first-claim name squatting.

The plugin stores credentials in local configuration, scoped to the relay URL
and publication. Credentials are never included in character sheets, exported
rulesets, public responses, or logs. Downloading someone else's ruleset and
publishing it creates a new code. Losing configuration loses update access to
existing publications. Back up configuration securely. Direct HTTP is supported;
bearer credentials and REST profiles have no TLS transport encryption in that
mode. Optional HTTPS deployment instructions remain in the deployment guide.

The server persists publications and ownership hashes in an encrypted SQLCipher
Community database. The first upgrade from the old in-memory server requires
republishing; subsequent restarts preserve content, codes, ownership claims,
and original expiry dates. Follow [storage setup](ENCRYPTED_STORAGE.md) before
starting the backend. Old plugin builds can still fetch public profiles/rulesets,
but their
unauthenticated publication requests are rejected. Old ruleset codes without a
locally stored ownership credential are published under a new code.

Public profiles contain visible identity, biography, OOC fields, remote portrait
URLs, and visible resource values. Hidden fields, inventory, attributes, skills,
abilities, feats, buffs, class/level, resource formulas, and local portrait paths
are excluded. The original local sheet is unchanged. Saving an NPC or backup
does not publish it as the player. Automatic profile publication is debounced
for one second; explicit publication starts immediately.

World-qualified profile lookups are exact. Only lookups without a world retain
the latest matching-name fallback. Use a world when identifying a character.

Limits and responses:

- Character payloads and decoded ruleset payloads: at most 2 MiB in UTF-8.
- Ruleset request bodies: at most 2 MiB plus 16 KiB of envelope overhead.
- Each publication registry: at most 1,024 entries and 64 MiB of payload storage.
- Character profiles expire after seven days; rulesets expire after 30 days.
  Publishing an owned update refreshes expiry. Expiry releases ownership claims.
- Publication writes/deletes: 60 requests per minute per observed client IP.
- Missing/invalid bearer credentials: 401; wrong ownership credential: 403;
  invalid input: 400; missing publication: 404; oversized body: 413;
  write rate exceeded: 429; registry capacity exhausted: 503.

GET requests remain public. See
[`Soulstone.SyncServer.http`](../Soulstone.SyncServer/Soulstone.SyncServer.http)
for request examples. Use fresh credentials rather than the sample credential.

## Routes and request shapes

Character routes support PUT, GET, and DELETE at
`/api/characters/{characterName}[/{worldName}]`. Brackets denote the optional
world segment; URL-encode both names. PUT takes a public profile JSON object
and returns 204; DELETE returns 204; GET returns submitted profile JSON.
PUT/DELETE require `Authorization: Bearer <ownerCredential>`.

The server validates JSON and ownership but does not apply the plugin's field
visibility projection. Manual callers must redact private sheets before upload.

`POST /api/dice-systems` requires the owner credential and this request shape:

```json
{
  "playerName": "Example Player",
  "worldName": "Moogle",
  "systemName": "Example Rules",
  "payload": "{\"SystemName\":\"Example Rules\"}"
}
```

Payload is a serialized JSON object stored as a string. Omit `code` for a new
publication; include the returned code for updates with the same credential.
Success returns HTTP 200 with `code`, `playerName`, `worldName`, `systemName`,
`payload`, and `updatedAtUtc`. Codes contain 10 characters.
`GET /api/dice-systems/{code}` returns this public publication;
`GET /api/dice-systems/{code}/version` returns `code`, `systemName`, and
`updatedAtUtc`. There is no ruleset DELETE endpoint.

## Deployment behavior

API and relay share a process and port. `/health` checks responsiveness and a
database read, returning 503 when storage is unavailable; it does not test
WebSocket connectivity or ownership. Public publications survive restart when
the database and original key are retained. Hosts recreate sessions/invites.

Limits use the observed connection IP. No forwarded-header middleware is
configured; players using one proxy/tunnel can share publication/session-creation
limits. Keep ownership/session credentials out of proxy logs and diagnostics.
