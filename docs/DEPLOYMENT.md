# Soulstone Sync Server - Installation & Deployment Guide

This guide provides comprehensive, step-by-step instructions for installing, configuring, deploying, and routing the **Soulstone Sync Server** (`Soulstone.SyncServer`) on a dedicated machine, home server, VPS, or cloud instance.

---

## 📑 Table of Contents
1. [Architecture Overview](#1-architecture-overview)
2. [Prerequisites](#2-prerequisites)
3. [Building the Server](#3-building-the-server)
   - [Method A: Standalone Single-File Executable (Recommended)](#method-a-standalone-single-file-executable-recommended)
   - [Method B: Docker Container](#method-b-docker-container)
4. [Host Machine Setup](#4-host-machine-setup)
   - [Network Binding & Static IP](#network-binding--static-ip)
   - [Firewall Configuration](#firewall-configuration)
   - [Running as a Persistent Background Service](#running-as-a-persistent-background-service)
     - [Linux (systemd)](#linux-systemd-service)
     - [Windows (Task Scheduler / NSSM)](#windows-background-service)
5. [Router & Network Routing](#5-router--network-routing)
   - [Static DHCP Reservation](#static-dhcp-reservation)
   - [Port Forwarding](#port-forwarding)
   - [Dynamic DNS (DDNS)](#dynamic-dns-ddns)
6. [TLS / HTTPS Reverse Proxy Setup (Optional)](#6-tls--https-reverse-proxy-setup-optional)
   - [Option A: Cloudflare Tunnel (Recommended - No Port Forwarding Required)](#option-a-cloudflare-tunnel-recommended)
   - [Option B: Caddy Reverse Proxy (Automated Let's Encrypt)](#option-b-caddy-reverse-proxy)
   - [Option C: NGINX + Certbot](#option-c-nginx--certbot)
7. [Verification & Client Connection](#7-verification--client-connection)
8. [Troubleshooting & FAQ](#8-troubleshooting--faq)

---

## 1. Architecture Overview

```
┌───────────────────────────────────────────────────────────────────┐
│                      Internet / External Players                  │
│       (Soulstone Dalamud Plugin Instances: Party Members & DM)    │
└─────────────────┬───────────────────────────────┬─────────────────┘
                  │                               │
       (Option A: Direct HTTP / WS)      (Option B: HTTPS / WSS)
                  │ (Port 5077)                   │ (Port 443)
                  │                               ▼
                  │                 ┌───────────────────────────────┐
                  │                 │   TLS Reverse Proxy (Optional)│
                  │                 │  (Cloudflare / Caddy / NGINX) │
                  │                 └─────────────┬─────────────────┘
                  │                               │ (Port 5077)
                  ▼                               ▼
┌───────────────────────────────────────────────────────────────────┐
│                    Soulstone.SyncServer (ASP.NET)                 │
│  - In-memory WebSocket session relay                              │
│  - In-memory Character Sheet Cloud Registry                       │
│  - Full HTTP (`http://`) & HTTPS (`https://`) support             │
│  - Zero persistence, zero logging of credentials/payloads         │
│  - Health Check: GET /health                                      │
└───────────────────────────────────────────────────────────────────┘
```

- **Transport Flexibility**: The Soulstone Dalamud plugin natively supports both plain HTTP/WS (`http://`, `ws://`) and secure HTTPS/WSS (`https://`, `wss://`) endpoints without requiring HTTPS or localhost restrictions. Direct IP connections (LAN, port-forwarded WAN, or VPN networks like Tailscale / WireGuard) work directly with plain HTTP.
- **Transport Security**: All character data, dice rolls, and shared resource bars are end-to-end encrypted (AES-GCM for group broadcasts, RSA-2048 for DM-only stats). The relay server operates in memory and only routes encrypted envelopes.
- **REST & Relay API Surface**:
  - `GET /health` — Health check endpoint.
  - `POST /api/sessions` — Creates a party sync room session.
  - `PUT /api/sessions/{sessionId}/invite` — Registers an invite payload with host bearer token.
  - `GET /api/invites/{inviteId}` — Resolves invite payload for joining party members.
  - `GET /api/sessions/{sessionId}/connect` — WebSocket relay upgrade endpoint.
  - `PUT /api/characters/{characterName}[/{worldName}]` — Registers/updates a character sheet JSON profile.
  - `GET /api/characters/{characterName}[/{worldName}]` — Retrieves a stored character sheet JSON profile.
  - `DELETE /api/characters/{characterName}[/{worldName}]` — Deletes a stored character sheet.

---

## 2. Prerequisites

- **Development / Build Machine**:
  - [.NET 8.0 SDK or higher](https://dotnet.microsoft.com/download)
  - Git
- **Target Host Machine** (Where the server will run):
  - **Option 1 (Standalone Binary)**: Windows 10/11/Server (x64) or Linux (Ubuntu, Debian, Alpine, CentOS, etc. x64/ARM64). *No .NET runtime installation required if built as self-contained.*
  - **Option 2 (Docker)**: Any OS with Docker / Docker Compose installed.
- **Network / Domain Requirements**:
  - A public IP address or a domain name (e.g. via DuckDNS, No-IP, or custom domain).
  - Access to your home router admin panel (if self-hosting with port forwarding).

---

## 3. Building the Server

### Method A: Standalone Single-File Executable (Recommended)

Self-contained publishing bundles the .NET runtime into a single standalone binary. You do not need to install the .NET SDK or runtime on the target machine.

#### 1. Build for Windows Host (x64)
Run from the root of the repository:
```powershell
dotnet publish .\Soulstone.SyncServer\Soulstone.SyncServer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\server-win
```
*Output artifact:* `.\publish\server-win\Soulstone.SyncServer.exe`

#### 2. Build for Linux Host (x64)
```powershell
dotnet publish .\Soulstone.SyncServer\Soulstone.SyncServer.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o .\publish\server-linux
```
*Output artifact:* `.\publish\server-linux\Soulstone.SyncServer`

#### 3. Build for Linux Host (ARM64 / Raspberry Pi)
```powershell
dotnet publish .\Soulstone.SyncServer\Soulstone.SyncServer.csproj -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true -o .\publish\server-arm64
```

Transfer the contents of the generated `publish` directory to your target machine using SCP, SFTP, or a USB drive.

---

### Method B: Docker Container

If you prefer deploying with Docker:

#### 1. Publish and Build the Image
```bash
# Publish binaries for Linux container
dotnet publish ./Soulstone.SyncServer/Soulstone.SyncServer.csproj -c Release -o ./Soulstone.SyncServer/publish

# Build Docker image
docker build -t soulstone-sync ./Soulstone.SyncServer
```

#### 2. Run the Container
```bash
docker run -d \
  --name soulstone-sync \
  --restart unless-stopped \
  -p 5077:5077 \
  -e ASPNETCORE_URLS="http://0.0.0.0:5077" \
  soulstone-sync
```

#### 3. (Optional) Docker Compose
Create a `docker-compose.yml`:
```yaml
version: '3.8'

services:
  soulstone-sync:
    image: soulstone-sync
    build:
      context: ./Soulstone.SyncServer
      dockerfile: Dockerfile
    container_name: soulstone-sync
    restart: unless-stopped
    environment:
      - ASPNETCORE_URLS=http://0.0.0.0:5077
    ports:
      - "5077:5077"
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5077/health"]
      interval: 30s
      timeout: 5s
      retries: 3
```

Run with:
```bash
docker compose up -d
```

---

## 4. Host Machine Setup

### Network Binding & Static IP

By default, the server listens on `http://127.0.0.1:5077`. To accept connections from your local network or reverse proxy, configure it to bind to `0.0.0.0:5077`.

#### Setting Static Local IP on the Host Machine:
Assign a static local IP address (e.g. `192.168.1.150`) to the host machine in its network adapter settings, or configure a DHCP reservation on your router (see Section 5).

---

### Firewall Configuration

Allow inbound traffic on port `5077` (and port `80`/`443` if terminating TLS directly on this machine).

#### Windows Firewall:
Run PowerShell as Administrator:
```powershell
New-NetFirewallRule -DisplayName "Soulstone Sync Server (5077)" -Direction Inbound -LocalPort 5077 -Protocol TCP -Action Allow
```

#### Linux (ufw):
```bash
sudo ufw allow 5077/tcp
sudo ufw reload
```

#### Linux (firewalld):
```bash
sudo firewall-cmd --permanent --add-port=5077/tcp
sudo firewall-cmd --reload
```

---

### Running as a Persistent Background Service

#### Linux (systemd service)

1. Copy the published executable to `/opt/soulstone-sync`:
   ```bash
   sudo mkdir -p /opt/soulstone-sync
   sudo cp -r ./publish/server-linux/* /opt/soulstone-sync/
   sudo chmod +x /opt/soulstone-sync/Soulstone.SyncServer
   ```

2. Create a system user:
   ```bash
   sudo useradd -r -s /bin/false soulstone
   sudo chown -R soulstone:soulstone /opt/soulstone-sync
   ```

3. Create the systemd service file:
   ```bash
   sudo nano /etc/systemd/system/soulstone-sync.service
   ```
   Add the following content:
   ```ini
   [Unit]
   Description=Soulstone Sync Server Relay
   After=network.target

   [Service]
   Type=simple
   User=soulstone
   WorkingDirectory=/opt/soulstone-sync
   ExecStart=/opt/soulstone-sync/Soulstone.SyncServer
   Restart=always
   RestartSec=10
   Environment=ASPNETCORE_URLS=http://0.0.0.0:5077
   Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

   # Security hardening
   NoNewPrivileges=true
   PrivateTmp=true
   ProtectSystem=full

   [Install]
   WantedBy=multi-user.target
   ```

4. Enable and start the service:
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable soulstone-sync
   sudo systemctl start soulstone-sync
   sudo systemctl status soulstone-sync
   ```

5. View logs:
   ```bash
   journalctl -u soulstone-sync -f
   ```

---

#### Windows (Background Service)

##### Option 1: Using NSSM (Non-Sucking Service Manager)
1. Download [NSSM](https://nssm.cc/).
2. In Administrator Command Prompt / PowerShell:
   ```cmd
   nssm install SoulstoneSync "C:\Path\To\Soulstone.SyncServer.exe"
   nssm set SoulstoneSync AppDirectory "C:\Path\To\"
   nssm set SoulstoneSync AppEnvironmentExtra ASPNETCORE_URLS=http://0.0.0.0:5077
   nssm start SoulstoneSync
   ```

##### Option 2: Windows Task Scheduler (At Startup)
1. Open **Task Scheduler** (`taskschd.msc`).
2. Create a new Task named `Soulstone Sync Server`.
3. Set Security options to **"Run whether user is logged on or not"**.
4. Set Trigger to **"At startup"**.
5. Set Action to **"Start a program"**:
   - Program: `C:\Path\To\Soulstone.SyncServer.exe`
   - Start in: `C:\Path\To\`
6. In Settings, enable **"If the task fails, restart every 1 minute"**.

---

## 5. Router & Network Routing

If hosting at home and making the server accessible across the internet via standard port forwarding, follow these steps:

### Static DHCP Reservation
1. Log into your router’s administration console (typically `192.168.1.1` or `192.168.0.1`).
2. Find **LAN Setup / DHCP Server / Static IP Assignment**.
3. Locate your host machine's MAC address and assign it a fixed IP (e.g. `192.168.1.150`).

### Port Forwarding
Create port forwarding rules to direct incoming traffic from your public IP to your host machine:

| Rule Name | Protocol | External (WAN) Port | Internal IP Address | Internal (LAN) Port |
| :--- | :--- | :--- | :--- | :--- |
| **Soulstone Direct (HTTP/WS)** | TCP | `5077` | `192.168.1.150` | `5077` |
| **Soulstone Reverse Proxy (TLS)** | TCP | `80` & `443` | `192.168.1.150` | `80` & `443` |

> 💡 **Note**: The Soulstone plugin natively supports direct `http://` and `ws://` connections on port `5077`. If you prefer using a custom domain with SSL certificates (port `443`), set up an optional reverse proxy as described in Section 6.

### Dynamic DNS (DDNS)
Most home ISPs provide dynamic public IP addresses that change over time. Use a DDNS service so players can connect to a domain name instead of an IP:
1. Register a free hostname on [DuckDNS](https://www.duckdns.org/), [No-IP](https://www.noip.com/), or [Dynu](https://www.dynu.com/).
   * Example: `my-soulstone-sync.duckdns.org`
2. Configure the DDNS updater in your router's **Dynamic DNS** settings, or run a lightweight DDNS client/cron script on the host machine.

---

## 6. TLS / HTTPS Reverse Proxy Setup (Optional)

While Soulstone works directly with plain HTTP/WS, setting up an optional reverse proxy allows you to use custom domain names, automatic TLS certificate management, and DDoS protection through Cloudflare.

---

### Option A: Cloudflare Tunnel (Recommended)
**Benefits:**
- **Zero port forwarding**: No ports need to be opened on your home router.
- **Hidden IP**: Your home public IP address is never exposed to players or the internet.
- **Free Automatic SSL**: Cloudflare manages the certificate.

#### Setup Steps:
1. Sign up for a free [Cloudflare](https://www.cloudflare.com/) account and add your domain (or a free sub-domain).
2. Install `cloudflared` on the host machine:
   - **Linux**: `sudo apt install cloudflared` (or download the binary)
   - **Windows**: Download `cloudflared.exe` from GitHub releases.
3. Authenticate and create a tunnel:
   ```bash
   cloudflared tunnel login
   cloudflared tunnel create soulstone-relay
   ```
4. Configure the tunnel (`~/.cloudflared/config.yml`):
   ```yaml
   tunnel: <TUNNEL_UUID>
   credentials-file: /path/to/<TUNNEL_UUID>.json

   ingress:
     - hostname: sync.yourdomain.com
       service: http://localhost:5077
     - service: http_status:404
   ```
5. Route the DNS:
   ```bash
   cloudflared tunnel route dns soulstone-relay sync.yourdomain.com
   ```
6. Run as a service:
   ```bash
   sudo cloudflared service install
   sudo systemctl start cloudflared
   ```
Players can now connect using: `https://sync.yourdomain.com`

---

### Option B: Caddy Reverse Proxy
**Benefits:**
- Simplest standalone reverse proxy.
- Automatically acquires and renews free Let's Encrypt / ZeroSSL TLS certificates with zero manual certificate management.

#### 1. Install Caddy:
- **Debian/Ubuntu**:
  ```bash
  sudo apt install -y debian-keyring debian-archive-keyring apt-transport-https curl
  curl -1sLF 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
  curl -1sLF 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list
  sudo apt update && sudo apt install caddy
  ```
- **Windows**: Download `caddy.exe` from [caddyserver.com](https://caddyserver.com/).

#### 2. Configure `/etc/caddy/Caddyfile`:
```caddy
sync.yourdomain.com {
    reverse_proxy localhost:5077
}
```

#### 3. Start Caddy:
```bash
sudo systemctl enable --now caddy
```

---

### Option C: NGINX + Certbot

If you already use NGINX on your server:

#### 1. NGINX Site Configuration (`/etc/nginx/sites-available/soulstone-sync`):
```nginx
server {
    listen 80;
    server_name sync.yourdomain.com;

    location / {
        proxy_pass http://127.0.0.1:5077;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400s;
        proxy_send_timeout 86400s;
    }
}
```

#### 2. Enable Site and Obtain SSL Certificate:
```bash
sudo ln -s /etc/nginx/sites-available/soulstone-sync /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
sudo certbot --nginx -d sync.yourdomain.com
```

---

## 7. Verification & Client Connection

### 1. Verify Health Endpoint
From a device outside or inside your local network, run:
```bash
# Direct HTTP check
curl -i http://<your-host-ip-or-domain>:5077/health

# Or via HTTPS reverse proxy
curl -i https://sync.yourdomain.com/health
```
Expected output:
```http
HTTP/1.1 200 OK
Content-Type: application/json

{"status":"healthy"}
```

### 2. Configure the Soulstone Plugin in FFXIV

1. Open FFXIV and type `/soulstone` to open the plugin window.
2. Navigate to **Settings (`ConfigWindow`)** -> **Party Synchronization**.
3. In **Sync Server URL**, enter your endpoint:
   - **Direct HTTP (LAN, IP, or Port Forwarding):**
     ```
     http://192.168.1.150:5077
     ```
     or
     ```
     http://my-soulstone-sync.duckdns.org:5077
     ```
   - **HTTPS Domain (via Reverse Proxy):**
     ```
     https://sync.yourdomain.com
     ```
4. **As the DM / Host:**
   - Click **Create Sync Session**.
   - Copy the generated **Invite Link** and send it to your party members over Discord / private message. It combines this relay URL and a random 16-character validation code in one value, for example `http://192.168.1.150:5077/join/AbCdEf1234567890` or `https://sync.yourdomain.com/join/AbCdEf1234567890`.
5. **As a Party Member:**
   - Paste the complete **Invite Link** and click **Join Session**; no separate server URL is needed.
   - The plugin will connect over `ws://` (for HTTP) or `wss://` (for HTTPS) and synchronize resource bars, rolls, and initiative turns automatically.

---

## 8. Troubleshooting & FAQ

### Q: Can I use plain HTTP / direct IP without HTTPS?
**A**: Yes! Soulstone fully supports direct `http://` and `ws://` endpoints. You can connect via local IP (`http://192.168.1.x:5077`), public IP, VPN (Tailscale, WireGuard, ZeroTier), or Dynamic DNS domain without setting up SSL certificates. HTTPS is recommended when exposing the server publicly on the open internet with custom domains, but is completely optional.

### Q: The health check works in browser, but WebSocket fails to connect.
**A**: Ensure your reverse proxy supports WebSocket upgrades:
- In NGINX, verify `proxy_set_header Upgrade $http_upgrade;` and `proxy_set_header Connection "upgrade";` are present.
- In Cloudflare, ensure WebSockets are enabled under **Network** settings in the Cloudflare dashboard.
- If using direct HTTP, ensure port `5077` is open in host and router firewalls.

### Q: Do players need to open any ports?
**A**: No. Only the server host needs port forwarding or a tunnel. Players connect outward via standard HTTP/WS (port 5077) or HTTPS/WSS (port 443).

### Q: How much RAM / CPU does the relay use?
**A**: The server is extremely lightweight. It typically consumes less than **30 MB of RAM** and negligible CPU, making it suitable for low-cost VPS instances or a Raspberry Pi.

### Q: What happens if the server restarts during a session?
**A**: Sessions and character registry caches are kept strictly in memory for maximum privacy. If the server restarts, the DM simply clicks **Create Sync Session** to generate a new session code for the party.
