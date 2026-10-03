# Free encrypted publication storage

Soulstone stores public profiles, published rulesets, ownership hashes, and
original update timestamps in an embedded SQLCipher Community database. The
Community edition is free under its BSD-style license; there is no database
server, paid package, or license key to provision. The backend container builds
the library from upstream SQLCipher 4.19.0 with a verified source SHA-256.
See [SQLCipher Community](https://www.zetetic.net/sqlcipher/community/).

Rooms and invitations remain in memory and must be recreated after restarting.
Party messages are still encrypted in the plugin and are not recorded. Public
GET endpoints remain public: disk encryption protects a copied database without
its key, but the running backend and its operator can still read public content.
Use HTTPS/WSS for Internet access; disk encryption does not protect HTTP bearer
credentials in transit.

## Start with Docker Compose

For the Windows host with Docker Desktop, follow the
[deployment guide](DEPLOYMENT.md) in order. It includes Docker engine checks,
a persistent `.env`/Compose project name, first startup, networking, updates,
and backup/restore commands. Keep that project name and the original key when
updating or moving files, so Compose retains the existing database volume.

The complete backend, .NET runtime, and SQLCipher library run in one Linux
container. The Windows host needs Docker with Linux-container support and Compose,
but no .NET SDK, native compiler, or database installation. The Docker image
builds everything itself; persistent data lives in a Docker volume.

On Windows 10/11, use Docker Desktop's Linux-container backend (WSL 2/Hyper-V).
If the host runs the **Windows Server operating system**, Docker Desktop is not
supported; use a Linux VM on that host with Docker Engine/Compose. This image
does not run in Windows-container mode. See
[Docker's Windows installation requirements](https://docs.docker.com/desktop/setup/install/windows-install/).

From the repository root (or extracted deployment bundle), generate a key
**once**, before creating the first database. On Windows PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Soulstone.SyncServer\Initialize-Storage.ps1
docker compose up -d --build
Invoke-RestMethod http://127.0.0.1:5077/health
```

The key script uses the platform random generator, writes a 32-byte key as 64
hexadecimal characters, restricts Windows file access, and refuses to overwrite
an existing key. It does not print the key. Do not regenerate a missing key for
an existing database: recover the original key from your separate secret backup.

On Linux, generate the same format with OpenSSL:

```sh
mkdir -p secrets
(umask 077; set -C; openssl rand -hex 32 > secrets/publications.key)
docker compose up -d --build
curl --fail http://127.0.0.1:5077/health
```

Shell noclobber refuses to replace an existing key. The Compose secret is a
read-only mount, not a secret vault; protect the host file and its backups.
The key is excluded from Git and the Docker build context.

Compose starts one backend with the database library inside it. Profiles and
rulesets reside in the named `publications` volume; the key is mounted separately
at `/run/secrets/publications-key`. Restarting/recreating the backend keeps
publications as long as both the volume and original key are retained.
`docker compose down` keeps the volume; `docker compose down -v` deletes it.

The host port binds to loopback by default for a local HTTPS proxy. For a LAN or
private VPN listener, set `SOULSTONE_BIND_ADDRESS` before starting Compose:

```powershell
$env:SOULSTONE_BIND_ADDRESS = '0.0.0.0'
docker compose up -d
```

Set the plugin's server URL to your reachable HTTPS proxy URL (or your intended
LAN/VPN URL). No plugin protocol or localization changes are required.
Enable Docker Desktop startup at Windows sign-in; Compose's `unless-stopped`
restart policy depends on the Docker engine running. Docker Desktop sign-in
startup does not provide an unattended backend before Windows login. See
[startup and routine operation](DEPLOYMENT.md#5-startup-and-routine-operation).

The image build runs the relay tests using the freshly compiled Community
library. Build tools and test-only native packages do not enter the runtime
image. SQLCipher/OpenSSL notices ship in `/app/notices`. The image uses Debian
Bookworm rather than the old Alpine image to match the OpenSSL/native build.

## Configuration outside Docker

The backend requires two settings, supplied through configuration or environment:

| Setting | Example | Purpose |
| --- | --- | --- |
| `Storage__DatabasePath` | `/var/lib/soulstone-sync/publications.db` | Absolute database file path in a persistent writable directory |
| `Storage__KeyFile` | `/etc/soulstone-sync/publications.key` | Absolute path to the access-restricted key file |

`appsettings.json` deliberately contains empty paths. Missing/malformed keys,
wrong keys, incompatible schema versions, unavailable storage, or an absent
SQLCipher library prevent startup. There is no plaintext/in-memory fallback.
Do not put the key itself in appsettings, environment variables, command-line
arguments, logs, or container images.

For native Windows/Linux publishing, build SQLCipher Community for that target
and include `sqlcipher.dll` or `libsqlcipher.so` plus required dependencies.
Use `-p:SqlCipherNativeDirectory=ABSOLUTE-DIRECTORY` when publishing the backend
to copy those files. Community source compilation requires a suitable native
compiler and OpenSSL. Docker is the provided free path that performs this build
automatically; ordinary `dotnet publish` cannot create the native library.
Never deploy the old community binary used only as a Windows test fallback.

Grant the service identity write access to the data directory and read access
to the key. Keep data and key files outside replaceable deployment output.
For Windows Task Scheduler's Local Service account, grant read access to the
key explicitly; the initialization script initially grants only the creating
user and SYSTEM access. Give the database directory access only to the required
service and administrators.

## Upgrade, expiry, and backup

The first upgrade from the old volatile relay loses its existing memory state;
republish once. Subsequent restarts preserve publications and ownership claims.
Clients retain their existing write credentials, share codes, HTTP statuses,
and JSON response shapes. Seven-day profile and 30-day ruleset lifetimes remain
unchanged. Expired reads fail before background cleanup, and reopening a database
does not refresh expiry. Losing plugin ownership credentials still loses update
access; persistence does not prove ownership of an FFXIV character.

Writes, ownership checks, capacity checks, and cleanup use transactions. The
server serializes access through one connection, uses full synchronous commits,
encrypted rollback journals, and in-memory temporary storage. Logical limits
remain 1,024 entries and 64 MiB of payload per publication type, with 2 MiB per
payload. Database/free pages and backups also consume disk space; monitor it.
Health checks include a database read and return 503 if storage is unavailable.

For a simple consistent backup, stop the backend, copy the database volume to
restricted backup storage, and restart. For example, from the repository root:

```powershell
New-Item -ItemType Directory -Path .\backups -Force
docker compose stop backend
$backendContainer = docker compose ps -aq backend
docker cp "${backendContainer}:/var/lib/soulstone-sync/." .\backups\
docker compose start backend
```

Only copy after a clean stop. Keep the encryption key in a separate protected
backup; a database backup without its key cannot be restored. Restore with the
backend stopped, using the same key and a compatible SQLCipher 4 version. Avoid
starting the server against an accidentally empty restore volume. Test recovery
before relying on it. Backups retain deleted/expired content until you delete
those backup generations; define a retention period.

Replacing the key file alone does not rotate a database key. Key rotation needs
a controlled SQLCipher rekey/export operation and matching backup/key handling;
there is no automatic rotation or administrative endpoint in this change.

## Development validation

```powershell
dotnet test Soulstone.SyncServer.Tests/Soulstone.SyncServer.Tests.csproj
dotnet test Soulstone.Tests/Soulstone.Tests.csproj
dotnet build Soulstone.sln
docker compose build
```

On machines without Community binaries, the Windows relay tests use a legacy
SQLCipher binary as an isolated test dependency. They test real encrypted files,
restart behavior, ownership, expiry, and concurrent first claims. The Docker
build instead loads the current native library and runs the same tests before
assembling the runtime image. Neither path changes production's requirement
for actual SQLCipher encryption.
