# Soulstone 1.4.0 server deployment

The API and WebSocket relay are one application: `Soulstone.SyncServer` (`net8.0`).
It runs independently of FFXIV, Dalamud, and the plugin. This guide uses Windows
Task Scheduler to start it at boot, even before anyone logs in.

The main deployment uses **HTTP on TCP 5077** and **WS** for party connections.
No certificate or reverse proxy is required. HTTPS instructions are retained
below as an optional alternative.

## Choose how players will connect

| Deployment | Listener on the host | Player URL | Networking |
| --- | --- | --- | --- |
| Same PC | http://127.0.0.1:5077 | http://127.0.0.1:5077 | No inbound firewall rule |
| LAN or private VPN | http://0.0.0.0:5077 | http://HOST-LAN-OR-VPN-IP:5077 | Allow TCP 5077 from the intended network |
| Public direct HTTP | http://0.0.0.0:5077 | http://PUBLIC-IP-OR-DOMAIN:5077 | Allow and forward TCP 5077 to the host |
| Public HTTPS tunnel | http://127.0.0.1:5077 | https://sync.example.com | Cloudflare Tunnel; no router forwarding |
| Public HTTPS proxy | http://127.0.0.1:5077 | https://sync.example.com | Caddy; forward TCP 80/443 |

With HTTP, REST publications and bearer credentials travel without TLS transport
encryption. Party message payloads retain their end-to-end encryption. The
optional HTTPS setup provides transport encryption if you need it later.
0.0.0.0 is a listening address, never the URL to give players.

All rooms, invites, profiles, rulesets, and ownership hashes are in memory.
Every process restart clears them. Automatic startup restores the API, not
previous sessions or publications. Local plugin sheets and configuration remain
on players' machines. See [publication ownership and migration](PUBLICATION_API.md).

## 1. Publish the Windows server

On the build machine, install a .NET SDK capable of targeting .NET 8. Building
the complete plugin solution also requires .NET 10 and Dalamud; building just
this server does not. Use a maintained Windows x64 host. For Windows ARM64,
replace win-x64 with win-arm64 in the publish command.

Open PowerShell in the repository root:

~~~powershell
dotnet test .\Soulstone.SyncServer.Tests\Soulstone.SyncServer.Tests.csproj
dotnet publish .\Soulstone.SyncServer\Soulstone.SyncServer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\Soulstone.SyncServer\bin\publish\win-x64
~~~

Stop if either command fails. Copy **all** files in the output directory,
including appsettings.json and any native libraries, to the server machine.
Single-file publishing can still produce supporting files. Self-contained
output includes the runtime, so the host does not need an installed SDK or
ASP.NET Core runtime. Rebuild and redeploy to receive runtime security updates.
See Microsoft's [single-file deployment reference](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview).

On the target machine, open **Windows PowerShell as Administrator**.
If you built on this machine, install with:

~~~powershell
$installDir = 'C:\Program Files\SoulstoneSync'
New-Item -ItemType Directory -Path $installDir -Force
Copy-Item -Path '.\Soulstone.SyncServer\bin\publish\win-x64\*' -Destination $installDir -Force
~~~

If you built elsewhere, copy the same complete output into that directory.
Use an administrator-controlled deployment directory rather than Downloads.
All examples below use this installation path.

## 2. Run once and check the API

In PowerShell on the host:

~~~powershell
& 'C:\Program Files\SoulstoneSync\Soulstone.SyncServer.exe' --urls 'http://0.0.0.0:5077'
~~~

Leave the terminal open and check from a second terminal:

~~~powershell
Invoke-RestMethod -Uri 'http://127.0.0.1:5077/health' -TimeoutSec 10
~~~

Expected result: status is healthy (HTTP 200). Stop the foreground server with
**Ctrl+C** before configuring startup so two instances do not compete for 5077.

Without a listener setting, the application defaults to http://0.0.0.0:5077.
The --urls argument explicitly selects the listener and takes precedence over
ASPNETCORE_URLS. appsettings.json is loaded from the executable's directory.
It contains logging settings and AllowedHosts; it is not a database or a place
for plugin ownership credentials.

## 3. Configure automatic Windows startup

Run this block in **Windows PowerShell as Administrator** after installation.
The listener below accepts direct HTTP connections on the host's network
interfaces. A local health check still uses 127.0.0.1.

~~~powershell
$installDir = 'C:\Program Files\SoulstoneSync'
$listenUrl = 'http://0.0.0.0:5077'
$taskName = 'Soulstone Sync Server'
$exePath = Join-Path $installDir 'Soulstone.SyncServer.exe'
if (-not (Test-Path -LiteralPath $exePath)) { throw 'Install the published server first.' }

# Allow Local Service to read and execute, without modifying the deployment.
icacls.exe $installDir /grant '*S-1-5-19:(OI)(CI)RX'
if ($LASTEXITCODE -ne 0) { throw 'Could not grant Local Service access.' }

$action = New-ScheduledTaskAction -Execute $exePath -Argument "--urls $listenUrl" -WorkingDirectory $installDir
$trigger = New-ScheduledTaskTrigger -AtStartup
$principal = New-ScheduledTaskPrincipal -UserId 'LOCALSERVICE' -LogonType ServiceAccount
$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable -RestartCount 999 -RestartInterval (New-TimeSpan -Minutes 1) -ExecutionTimeLimit ([TimeSpan]::Zero) -MultipleInstances IgnoreNew -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries

Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description 'Soulstone API and WebSocket relay; starts at boot and restarts after failure.'
Start-ScheduledTask -TaskName $taskName
~~~

This creates a new task. If that name exists, inspect it first. To intentionally
update this task's definition, stop it and rerun registration with -Force added
to Register-ScheduledTask.

The task uses built-in Local Service without a password or interactive login.
It runs continuously, ignores duplicate launches, and retries failed exits after
one minute, up to 999 times. A zero execution limit prevents the normal runtime
cutoff. Manual stops are not crashes; explicitly start afterward. These settings
follow Microsoft's [task principal](https://learn.microsoft.com/en-us/powershell/module/scheduledtasks/new-scheduledtaskprincipal)
and [task settings](https://learn.microsoft.com/en-us/powershell/module/scheduledtasks/new-scheduledtasksettingsset) references.

Check startup:

~~~powershell
Get-ScheduledTask -TaskName 'Soulstone Sync Server' | Select-Object TaskName, State
Get-ScheduledTaskInfo -TaskName 'Soulstone Sync Server'
Invoke-RestMethod -Uri 'http://127.0.0.1:5077/health' -TimeoutSec 10
~~~

Expected state: Running. For this continuous task, LastTaskResult can be 267009
(0x41301, still running); use /health to confirm API availability. Reboot at a
convenient time and check health from another device before anyone logs in to
the host. Manual startup alone does not verify boot startup.

In **Settings > System > Power**, disable automatic sleep while plugged in if
the host must serve players continuously. A sleeping or shut-down computer
cannot serve requests.

The executable is a console application and does not implement the Windows
Service Control Manager protocol. Do not register it directly with sc.exe
create. Task Scheduler handles it directly; a Windows service needs a wrapper.

### Start, stop, disable, or remove startup

Run as Administrator:

~~~powershell
Stop-ScheduledTask -TaskName 'Soulstone Sync Server'
Start-ScheduledTask -TaskName 'Soulstone Sync Server'
~~~

To prevent boot launches, run Disable-ScheduledTask with the same task name and
stop the running task. To restore them, run Enable-ScheduledTask and start it.
To remove startup, stop the task and run:

~~~powershell
Unregister-ScheduledTask -TaskName 'Soulstone Sync Server'
~~~

Removing the task does not delete the published files.

## 4. Make the server reachable

### LAN or private VPN

The startup task already uses http://0.0.0.0:5077. Reserve the host's LAN address in
your router's DHCP settings, for example 192.168.1.150. On the host, run as
Administrator:

~~~powershell
New-NetFirewallRule -DisplayName 'Soulstone Sync LAN' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5077 -Profile Private -RemoteAddress LocalSubnet
~~~

This permits local-subnet clients on a Private network profile. For a VPN,
adapt -RemoteAddress to the actual VPN subnet and -Profile to the adapter.
From another device, check http://192.168.1.150:5077/health. Give players the
host's address; localhost refers to each player's own computer.

### Public direct HTTP with router port forwarding

1. Reserve the host's LAN address in your router's DHCP settings, for example
   192.168.1.150. Keep the task listener on http://0.0.0.0:5077.
2. In Administrator PowerShell, permit direct HTTP traffic. Unlike the LAN rule,
   this rule accepts remote source addresses:

   ~~~powershell
   New-NetFirewallRule -DisplayName 'Soulstone Sync HTTP' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5077 -Profile Any
   ~~~

3. Add a router forwarding rule: **external TCP 5077 -> 192.168.1.150 TCP 5077**.
   Replace the address with your host's reserved address. UDP is not needed.
4. Give players http://YOUR-PUBLIC-IP:5077 or a DNS hostname pointing to that
   IP, such as http://sync.example.com:5077. A changing public IP needs a DNS updater.
5. From another network, such as mobile data, check:

   ~~~powershell
   Invoke-RestMethod -Uri 'http://YOUR-PUBLIC-IP:5077/health' -TimeoutSec 10
   ~~~

6. Verify a real plugin session using that same base URL. HTTP maps to WS;
   there is no HTTPS redirect or certificate setup in this deployment.

If external access fails, check the host firewall, router forwarding, double
NAT, and ISP CGNAT. CGNAT usually prevents direct inbound forwarding; ask your
ISP for a reachable public address or use a private VPN. Some routers cannot
reach their public IP from the LAN, so test externally. You only need port 5077
for this path; the optional proxy ports 80/443 are unrelated to direct HTTP.

### Optional HTTPS through Cloudflare Tunnel

If choosing this alternative, update the API task listener to
http://127.0.0.1:5077 and remove the direct HTTP router forwarding/firewall rule
if it is no longer used. The API still speaks HTTP locally; the tunnel exposes
HTTPS to players.

You need a domain managed through Cloudflare and a tunnel connector on the host.
This works without inbound router ports, including with CGNAT. Keep the API
bound to 127.0.0.1:5077.

1. Follow Cloudflare's [tunnel setup guide](https://developers.cloudflare.com/tunnel/get-started/)
   to create a remotely managed tunnel and select its Windows connector.
2. Download cloudflared.exe through the official guide into a stable directory,
   for example C:\Program Files\cloudflared.
3. In Administrator PowerShell, run the service command shown for your tunnel:

   ~~~powershell
   & 'C:\Program Files\cloudflared\cloudflared.exe' service install '<YOUR-TUNNEL-TOKEN>'
   Get-Service -Name cloudflared
   Set-Service -Name cloudflared -StartupType Automatic
   Start-Service -Name cloudflared
   ~~~

4. Add a published application route for sync.example.com targeting **HTTP**
   127.0.0.1:5077. The external player URL is HTTPS.
5. Check https://sync.example.com/health from another network, then test a real
   plugin party session. HTTP health does not test WebSocket upgrades.

Keep the tunnel token private. Do not add a browser authentication challenge
or Cloudflare Access login requirement to the API: the plugin has no interactive
browser authentication flow. Preserve bearer headers and WebSocket upgrades.
Proxy request/body/header logging must also avoid credentials and private data.

Both the API task and cloudflared service must start after reboot. Check them
independently. No firewall rule or router forwarding for 5077 is needed when
the connector and API run on the same host.

### Optional HTTPS through Caddy and router forwarding

For this alternative, update the API task listener to http://127.0.0.1:5077
and remove the direct HTTP router forwarding/firewall rule if unused.

Use this if you have a reachable public IP and control router forwarding.
Point DNS for sync.example.com at that IP. Forward TCP 80 and 443 to the host's
reserved LAN address and allow them through Windows Firewall. Keep 5077 on
loopback and do not forward it.

Download Caddy from its [official installation page](https://caddyserver.com/docs/install).
Create C:\Program Files\Caddy\Caddyfile:

~~~caddy
sync.example.com {
    reverse_proxy 127.0.0.1:5077
}
~~~

Validate and test:

~~~powershell
& 'C:\Program Files\Caddy\caddy.exe' validate --config 'C:\Program Files\Caddy\Caddyfile' --adapter caddyfile
& 'C:\Program Files\Caddy\caddy.exe' run --config 'C:\Program Files\Caddy\Caddyfile' --adapter caddyfile
~~~

Caddy's [automatic HTTPS](https://caddyserver.com/docs/automatic-https) manages
certificates; its [reverse proxy](https://caddyserver.com/docs/caddyfile/directives/reverse_proxy)
supports WebSocket upgrades. For unattended startup, follow its
[Windows service instructions](https://caddyserver.com/docs/running#windows-service)
and configure automatic startup and failure recovery. Give that service account
persistent writable certificate storage. A foreground command alone does not
configure proxy startup.

If outside access fails, check double NAT/CGNAT with your ISP. Dynamic public IPs
need a DNS updater. Test from a mobile connection; some routers cannot reach
their own public hostname from the LAN.

### Rate limits behind proxies

The server uses the connection's RemoteIpAddress and does not process forwarded
client IP headers. Players using one tunnel or reverse proxy can share its limits:
10 session creations/minute and 60 publication writes/deletes/minute. A 429 can
affect multiple players. Adding X-Forwarded-For at the proxy alone does not change
this behavior.

## 5. Connect the 1.4.0 plugin

1. Open /soulstone and select **Group** in the **Tools** sidebar.
2. Select **Host**, enter the base URL such as http://YOUR-PUBLIC-IP:5077
   (without /api or /health), and create a session.
3. Share the generated invite privately with the intended players.
4. Players select **Join**, paste the entire invite, and join. The invite
   supplies the URL; HTTP uses WS and HTTPS uses WSS automatically.
5. Verify roster presence, a shared roll, a visible resource update, and a
   host-controlled initiative update. Test profile publication/inspection and
   dice-system sharing separately from the party WebSocket.

Use the same reachable relay URL for publications and sessions. Ownership
credentials are scoped to that URL in plugin configuration, so switching between
LAN/public URLs can change the credential used.

Invites are plugin input, not a browser setup page. A /join/... URL need not
render a page. Do not include actual invites or session tokens in support reports.

## 6. Updates, rollback, and diagnostics

For 1.4.0, upgrade plugin and server together, restart the relay, and republish
content. Old plugins can download public content but unauthenticated publication
writes are rejected. Existing rulesets without a local credential get a new code.

For later deployments:

1. Publish and test into a separate build directory.
2. Notify players: restarting clears rooms, invites, and publications.
3. In Administrator PowerShell, disable and stop before replacing files:

   ~~~powershell
   Disable-ScheduledTask -TaskName 'Soulstone Sync Server'
   Stop-ScheduledTask -TaskName 'Soulstone Sync Server'
   Get-Process -Name 'Soulstone.SyncServer' -ErrorAction SilentlyContinue
   ~~~

4. Wait for the process to exit. Save the old deployment outside the installation
   directory, including custom configuration. Replace the complete output and
   reapply deliberate appsettings.json customizations.
5. Enable and start:

   ~~~powershell
   Enable-ScheduledTask -TaskName 'Soulstone Sync Server'
   Start-ScheduledTask -TaskName 'Soulstone Sync Server'
   Invoke-RestMethod -Uri 'http://127.0.0.1:5077/health' -TimeoutSec 10
   ~~~

6. Check public health and a real plugin session. Create a new room and invite;
   republish profiles and rulesets.

To roll back, disable/stop, restore the old deployment, enable/start, and verify.
Rollback also clears memory and needs compatible plugin/server versions.
Securely back up players' plugin configurations for ownership credentials.
There is no server database to back up or restore.

| Symptom | Check |
| --- | --- |
| Task Ready instead of Running | Get-ScheduledTaskInfo; enable Task Scheduler history and inspect its Operational event log |
| Local health fails | Action path, file permissions, listener, and port usage |
| Address already in use | Stop an old foreground instance or choose another port consistently |
| Local health works, remote fails | Binding, host firewall profile, router route, tunnel, and DNS |
| Health works, party connection fails | WebSocket upgrades, proxy authentication, current invite, and session expiry |
| Publication 401 | Missing/invalid bearer credential or plugin/server version mismatch |
| Publication 403 | Wrong owner credential; restore original plugin configuration |
| 404 after restart | Republish content and create a new session |
| 429 | Wait for the rate window; proxy users may share a limit |
| 413 or 503 | Payload or registry capacity limit; see API reference |
| Works only while logged in | Startup trigger, Local Service principal, host sleep, and proxy/tunnel startup |

Inspect port usage with:

~~~powershell
Get-NetTCPConnection -LocalPort 5077 -ErrorAction SilentlyContinue
~~~

Task Scheduler records execution events, not API console output. To see startup
errors, stop the task and run the executable in a terminal with the same --urls
argument. For a short diagnostic log:

~~~powershell
& 'C:\Program Files\SoulstoneSync\Soulstone.SyncServer.exe' --urls 'http://0.0.0.0:5077' *> "$env:TEMP\soulstone-startup.log"
~~~

Stop this process before restarting the task. The application logs lifecycle and
transport errors, not credentials or payloads. Avoid request-body/authorization
logging at proxies. Health confirms responsiveness, not surviving state or
connectivity for every player.

## 7. Linux and containers

These use the same API, limits, and in-memory lifecycle.

### Linux systemd

Publish for the host architecture (linux-x64 or linux-arm64):

~~~bash
dotnet publish Soulstone.SyncServer/Soulstone.SyncServer.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o Soulstone.SyncServer/bin/publish/linux-x64
~~~

Copy the complete output to /opt/soulstone-sync, make the executable runnable,
and create a dedicated soulstone system user with read/execute access. Install
/etc/systemd/system/soulstone-sync.service:

~~~ini
[Unit]
Description=Soulstone API and WebSocket relay
After=network.target

[Service]
Type=simple
User=soulstone
WorkingDirectory=/opt/soulstone-sync
ExecStart=/opt/soulstone-sync/Soulstone.SyncServer --urls http://0.0.0.0:5077
Restart=on-failure
RestartSec=10
NoNewPrivileges=true
PrivateTmp=true

[Install]
WantedBy=multi-user.target
~~~

Run sudo systemctl daemon-reload, then sudo systemctl enable --now soulstone-sync.
Inspect systemctl status soulstone-sync and journalctl -u soulstone-sync.
Choose loopback/proxy or LAN binding as described above.

### Docker

The existing Dockerfile uses ASP.NET Core 8 Alpine. Publish portable,
framework-dependent output first, rather than copying a Windows executable:

~~~powershell
dotnet publish .\Soulstone.SyncServer\Soulstone.SyncServer.csproj -c Release --self-contained false -o .\Soulstone.SyncServer\publish
docker build -t soulstone-sync:1.4.0 .\Soulstone.SyncServer
docker run -d --name soulstone-sync --restart unless-stopped -p 5077:5077 soulstone-sync:1.4.0
~~~

The container and published host port accept direct HTTP on 5077. Apply the
firewall/router policy for your intended network. For an optional host-side
HTTPS proxy, use -p 127.0.0.1:5077:5077 instead. Docker must start at boot too;
Docker Desktop may depend on
user login, so verify your host's boot behavior. Inspect docker logs soulstone-sync
and check health from the host. The runtime image does not promise curl is
installed; do not use an unverified curl-based container health check.

Restarting or recreating a container clears state. The Docker publish directory
is a build artifact; keep it out of commits.
