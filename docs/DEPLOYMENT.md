# Deploy Soulstone Sync Server on Windows with Docker Desktop

This guide targets **Soulstone 1.5.0** and the `Soulstone-sync-docker.zip`
asset from release `V1.5.0`. Upgrade the plugin and backend together. The
first migration from the old in-memory backend requires republishing content;
later upgrades preserve publications when the database and key are retained.

The API, WebSocket relay, .NET runtime, and free SQLCipher Community database
library run in one Linux container. Docker builds everything from the deployment
files. You do not need a .NET SDK, Dalamud, FFXIV, native compiler, or separate
database service on the Windows host.

Public profiles, rulesets, and ownership hashes survive container restarts in a
Docker volume. Rooms and invites remain temporary and must be recreated after
restarting. Party messages are forwarded without being recorded. Encryption
protects database files without their key; public API downloads remain public.
See [storage details](ENCRYPTED_STORAGE.md) and [publication contracts](PUBLICATION_API.md).

## 1. Check Docker Desktop

Start Docker Desktop and wait until its engine is running. In PowerShell:

```powershell
docker version
docker compose version
docker info --format '{{.OSType}}'
```

The last command must print `linux`. If it prints `windows`, use the Docker
Desktop tray menu to switch to Linux containers. Resolve engine/WSL startup
errors before continuing. This image uses Linux, even though the host is Windows.

Docker Desktop supports Windows client operating systems. If your host actually
runs the **Windows Server operating system**, its Docker Desktop installation is
not supported by Docker; the supported alternative for this Linux image is a
Linux VM with Docker Engine and Compose. See
[Docker's Windows requirements](https://docs.docker.com/desktop/setup/install/windows-install/).

## 2. Copy the deployment and create the key

Use a stable directory, such as `C:\SoulstoneSync`. Copy the repository there,
or extract the supplied `Soulstone-sync-docker.zip` deployment bundle:

```powershell
$installDir = 'C:\SoulstoneSync'
Expand-Archive -LiteralPath 'C:\Downloads\Soulstone-sync-docker.zip' -DestinationPath $installDir
Set-Location $installDir
```

Replace the ZIP path with its actual location. The directory must contain
`compose.yaml`, `.dockerignore`, `Soulstone.SyncServer`, and
`Soulstone.SyncServer.Tests`. The tests are required by the image build. Run all
following Compose commands from this directory and with the same Windows user
that runs Docker Desktop.

For a **new deployment**, create `.env` beside `compose.yaml`:

```powershell
@'
COMPOSE_PROJECT_NAME=soulstone-sync
SOULSTONE_BIND_ADDRESS=127.0.0.1
'@ | Set-Content -LiteralPath .\.env -Encoding ASCII
```

The fixed project name keeps Compose using the same database volume even if the
deployment directory changes. If you already started the backend, keep its
existing project name instead: inspect `docker compose ls` and use that name in
`.env`. Changing it selects a different volume and can appear to lose data.
Keep `.env` for future updates. Existing PowerShell environment variables with
these names override `.env`; remove stale overrides before using the file.

Generate the encryption key once:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Soulstone.SyncServer\Initialize-Storage.ps1
```

This creates `secrets\publications.key`, restricts Windows file access, and
refuses to replace an existing key. It does not print the key. Back up this file
separately in protected storage before using the database. Keep the same key
through updates and restarts. If you already have a database, recover its original
key rather than generating a replacement.

The key is mounted read-only into the backend; it is excluded from the image
build and Git. The database lives in a named Docker volume, not in the ZIP or
deployment directory. Do not put the key itself in `.env` or `appsettings.json`.

## 3. Build, start, and check health

```powershell
docker compose config --quiet
docker compose up -d --build --wait --wait-timeout 120
docker compose ps
Invoke-RestMethod -Uri 'http://127.0.0.1:5077/health' -TimeoutSec 10
```

Stop if a command fails. The first build downloads .NET images, packages, and
pinned SQLCipher Community source, compiles the native library, and runs the
relay tests. Internet access is needed for this build. Subsequent builds reuse
cached layers. `--wait-timeout` applies to service readiness, not the build.
Docker documents these options in [Compose up](https://docs.docker.com/reference/cli/docker/compose/up/).

Expected results: the backend is running and healthy; `/health` returns
`{"status":"healthy"}`. Missing or wrong encryption keys prevent startup.
The health check also reads database metadata; it does not test WebSocket
connectivity or whether a particular player can join.

If an old native Soulstone server or scheduled task already uses port 5077,
stop it before starting the container. For the previously documented task:

```powershell
# Only if this is the old Soulstone task you are replacing:
Stop-ScheduledTask -TaskName 'Soulstone Sync Server'
Disable-ScheduledTask -TaskName 'Soulstone Sync Server'
```

Do not run the old native backend alongside the container on the same port.
The first migration from the old in-memory server requires recreating sessions
and republishing content once. Later container restarts preserve publications.

## 4. Make it reachable and connect the plugin

| Connection | `SOULSTONE_BIND_ADDRESS` in `.env` | Player URL | Host networking |
| --- | --- | --- | --- |
| Same computer | `127.0.0.1` | `http://127.0.0.1:5077` | No inbound rule |
| LAN/private VPN | `0.0.0.0` | `http://HOST-IP:5077` | Allow TCP 5077 from the intended subnet |
| HTTPS tunnel on the host | `127.0.0.1` | `https://sync.example.com` | Tunnel targets `http://127.0.0.1:5077` |
| HTTPS proxy on the host | `127.0.0.1` | `https://sync.example.com` | Proxy targets `127.0.0.1:5077`; public ports 80/443 |

`127.0.0.1` always refers to the caller's own computer. A player on another
computer must use the Windows host's address or public HTTPS hostname. Never
give players `0.0.0.0`; it is a bind setting, not a destination.

After editing `.env`, apply the change:

```powershell
docker compose up -d --wait --wait-timeout 120
```

This can recreate the container and disconnect active rooms. Keep the project
name and key unchanged so publications retain their database.

### LAN or private VPN

Set `SOULSTONE_BIND_ADDRESS=0.0.0.0`. Reserve the host's LAN address in the
router, for example `192.168.1.150`. In Administrator PowerShell on the host:

```powershell
New-NetFirewallRule -DisplayName 'Soulstone Sync LAN' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5077 -Profile Private -RemoteAddress LocalSubnet
```

For a VPN, use its actual subnet and Windows network profile instead. Check
`http://192.168.1.150:5077/health` from another computer. No router port forwarding
is needed for LAN access.

### Internet access through HTTPS

Keep the Compose bind address on loopback. Database encryption does not protect
REST publications or bearer credentials over HTTP. Use HTTPS/WSS for Internet
access; the plugin automatically uses WSS when its base URL is HTTPS.

For Cloudflare Tunnel, follow the
[official setup guide](https://developers.cloudflare.com/tunnel/get-started/)
and run its connector on the Windows host. Configure the public hostname to
target `http://127.0.0.1:5077`. This avoids forwarding router ports and works
without direct inbound reachability. Preserve WebSocket upgrades and bearer
headers; the plugin has no interactive browser login flow. The tunnel connector
and Docker Desktop must both be running.

For a host-side Caddy proxy, use its
[installation guide](https://caddyserver.com/docs/install) and a Caddyfile:

```caddy
sync.example.com {
    reverse_proxy 127.0.0.1:5077
}
```

Point DNS to the public IP, forward TCP 80/443 to the Windows host, and allow
those ports in Windows Firewall. Do not forward 5077 for this setup. Configure
Caddy startup using its [running guide](https://caddyserver.com/docs/running).
Test public health from another network and then test a real plugin session.
CGNAT/double NAT can prevent incoming connections; a tunnel avoids that path.

These loopback targets assume the proxy/connector runs **on the Windows host**.
Inside another container, `127.0.0.1` refers to that container; use shared Docker
network routing to the backend instead. Proxy logs must omit authorization
headers, invite secrets, and request bodies containing character data.

### Plugin connection

1. Open `/soulstone`, then **Tools > Group > Host**.
2. Enter the reachable base URL, without `/api` or `/health`, and create a session.
3. Share the generated invite privately. Players paste it into **Join**.
4. Verify roster presence, a roll, a resource update, and host initiative changes.
5. Test public profile inspection and ruleset publication/download separately.

Use the same relay URL consistently: plugin ownership credentials are scoped to
that URL. Invites are plugin input, not a browser setup page. Switching URLs or
losing plugin configuration can change or lose publication update access.

## 5. Startup and routine operation

In Docker Desktop **Settings > General**, enable **Start Docker Desktop when you
sign in to your computer**. This starts Desktop at sign-in, not an unattended
backend before Windows login. See [Desktop settings](https://docs.docker.com/desktop/settings-and-maintenance/settings/).

Compose sets `restart: unless-stopped`: the backend restarts after process
failure and when the Docker engine restarts, unless you explicitly stopped it.
It does not start Docker Desktop itself. After a manual stop, start the backend
again explicitly. An unhealthy health check alone does not restart the process.

```powershell
Set-Location 'C:\SoulstoneSync'
docker compose ps
docker compose logs --tail 100 backend
docker compose stop backend
docker compose start backend
```

Run only the operation you need. To follow logs, use `docker compose logs -f
backend`; Ctrl+C ends log viewing, not the detached backend. Disable automatic
Windows sleep while plugged in if the host must serve players continuously.

After a host reboot, sign in, wait for Docker Desktop, and check health from
another device. If you need availability **before anyone signs in**, use a Linux
VM whose Docker Engine starts as a system service and configure that VM to start
with Windows. The old Local Service scheduled task does not start this
user-scoped Docker Desktop deployment.

## 6. Update and roll back

Before an update, take a database backup as described below. Keep the current
deployment files and a rollback image:

```powershell
$backendContainer = docker compose ps -q backend
if (-not $backendContainer) { throw 'Start the current backend before saving its image.' }
$currentImage = docker inspect --format '{{.Image}}' $backendContainer
docker image tag $currentImage soulstone-sync:rollback
```

Copy the new deployment files into the **same directory**, retaining `.env`,
`secrets\publications.key`, and the same Compose project name. Then:

```powershell
docker compose build --pull
docker compose up -d --wait --wait-timeout 120
Invoke-RestMethod -Uri 'http://127.0.0.1:5077/health' -TimeoutSec 10
```

Building happens before container replacement, so a failed build leaves the old
backend running. Applying the new image disconnects rooms and invites; recreate
them. Check existing public profiles/rulesets after updating.

To roll back, restore the saved deployment files, keeping the original `.env`
and key. Tag the saved image as the service's current image name and run Compose
without rebuilding:

```powershell
$backendContainer = docker compose ps -aq backend
if (-not $backendContainer) { throw 'Backend container not found.' }
$backendImage = docker inspect --format '{{.Config.Image}}' $backendContainer
docker image tag soulstone-sync:rollback $backendImage
docker compose up -d --no-build --pull never --wait --wait-timeout 120
```

An older server rejects a newer database schema. If an upgrade changed the
schema, restore the matching pre-upgrade database backup while the backend is
stopped. Do not remove volumes or regenerate keys to force an old image to start.

## 7. Back up and restore

Keep database backups and key backups in separate protected locations. For a
consistent database backup, stop the backend, copy its data, and restart:

```powershell
Set-Location 'C:\SoulstoneSync'
$backupDir = Join-Path 'C:\SoulstoneBackups' (Get-Date -Format 'yyyyMMdd-HHmmss')
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
docker compose stop backend
if ($LASTEXITCODE -ne 0) { throw 'Backend stop failed; do not copy a live database.' }
$backendContainer = docker compose ps -aq backend
if (-not $backendContainer) { throw 'Backend container not found.' }
docker cp "${backendContainer}:/var/lib/soulstone-sync/." $backupDir
if ($LASTEXITCODE -ne 0) { throw 'Backup failed; investigate before updating.' }
docker compose start backend
```

This uses [Docker cp](https://docs.docker.com/reference/cli/docker/container/cp/),
which supports stopped containers. Check the exit status and confirm the backup
contains `publications.db`. Store the original `secrets\publications.key`
separately; the database cannot be recovered without its key.

To restore, use the matching key, stop the existing backend, and copy the backup:

```powershell
$backupDir = 'C:\SoulstoneBackups\REPLACE-WITH-BACKUP-TIMESTAMP'
if (-not (Test-Path -LiteralPath (Join-Path $backupDir 'publications.db'))) {
    throw 'Select a backup containing publications.db.'
}
docker compose stop backend
if ($LASTEXITCODE -ne 0) { throw 'Backend stop failed.' }
$backendContainer = docker compose ps -aq backend
if (-not $backendContainer) { throw 'Backend container not found.' }
docker cp "$backupDir\." "${backendContainer}:/var/lib/soulstone-sync/"
if ($LASTEXITCODE -ne 0) { throw 'Restore failed; leave the backend stopped.' }
docker compose start backend
Invoke-RestMethod -Uri 'http://127.0.0.1:5077/health' -TimeoutSec 10
```

Take a backup of the current database before overwriting it. Restore a clean-stop
backup into a cleanly stopped backend; do not overlay it onto leftover recovery
journals from a crash. For recovery on a new host, retain the same project name
and key, create the stopped container with `docker compose create --build`, and
then copy the backup before its first start.

`docker compose down` removes containers but retains the named volume.
**`docker compose down -v` deletes the database volume.** Docker Desktop resets
and volume pruning can also delete data. Publications expire after seven days
(profiles) or 30 days (rulesets); backups retain them until their own retention
period ends. Merely replacing the key file does not rotate encryption keys.

## 8. Troubleshooting

```powershell
docker compose ps -a
docker compose logs --tail 100 backend
docker compose config --quiet
Get-NetTCPConnection -LocalPort 5077 -ErrorAction SilentlyContinue
```

| Symptom | Check |
| --- | --- |
| Cannot connect to Docker daemon | Open Docker Desktop and wait for the Linux engine; check `docker version` |
| No matching manifest / Windows image error | `docker info --format '{{.OSType}}'` must return `linux` |
| Missing Compose secret | Confirm `secrets\publications.key` exists and the Docker Desktop user can read it |
| Encrypted storage cannot be opened | Original key, same project/volume, file access, database integrity, and matching schema; do not generate a new key |
| Port 5077 already in use | Stop the old native relay/task or another container using the port |
| Build fails before startup | Inspect the failed build step; downloads require Internet, and test failures stop image creation |
| Local health succeeds, remote fails | Loopback versus LAN binding, `.env` overrides, Windows Firewall, DNS, proxy/tunnel, router forwarding |
| Health succeeds, party joins fail | WebSocket upgrades, proxy login challenges, current invite, session expiry |
| No service after reboot | Sign in and start Docker Desktop; `unless-stopped` cannot start the engine |
| Profiles disappeared after moving files | Keep the original Compose project name, volume, and key; also check publication expiry |
| Publication 401 / 403 | Missing credential / wrong owner credential; recover the original plugin configuration |
| 429 | Wait for the rate window; players behind one proxy can share IP limits |
| 413 / publication 503 | Payload/capacity limits; health 503 separately indicates a database read failure |

The backend uses observed connection IPs and does not process forwarded headers.
A proxy can therefore put multiple players under the same rate limit. Logs
contain lifecycle events rather than payloads/credentials; apply the same policy
to proxies and support reports.

## 9. Native deployment alternatives

Docker Desktop/Compose is the path described above. Native Windows publishing
or Linux systemd deployment is still possible, but requires building and supplying
the target's SQLCipher Community library, configuring absolute database/key paths,
and granting the service identity the required data/key access. Ordinary
`dotnet publish` alone is insufficient. See
[native storage configuration](ENCRYPTED_STORAGE.md#configuration-outside-docker).

The Windows executable is a console application; it cannot be registered directly
with `sc.exe create`. A native deployment needs Task Scheduler or a service
wrapper. This does not apply to the Compose container, whose lifecycle is managed
by Docker.
