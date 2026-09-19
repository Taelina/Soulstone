# Soulstone Sync Server

This is a standalone, non-interactive ASP.NET Core WebSocket relay and character sheet cloud registry for the **Soulstone** Dalamud plugin. It stores active party rooms and registered character sheet profiles in memory and forwards end-to-end encrypted messages; credentials and sync payloads are never logged or stored on disk.

## Features

- **Encrypted WebSocket Relay**: Forwards end-to-end encrypted party sync envelopes without storing message history.
- **Character Sheet Cloud Registry**: In-memory REST API (`/api/characters/...`) for uploading, retrieving, and inspecting player character sheets remotely.
- **Protocol Flexibility (HTTP & HTTPS)**: Supports both plain HTTP/WS (`http://`, `ws://`) and secure HTTPS/WSS (`https://`, `wss://`) connections. HTTPS is optional and recommended for public internet deployments, while plain HTTP works directly for LAN, VPN (Tailscale/WireGuard), or direct IP setups.
- **Zero Disk Footprint**: Operates completely in-memory with automatic session timeouts and garbage collection.

## Run directly

```powershell
dotnet run --project .\Soulstone.SyncServer\Soulstone.SyncServer.csproj
```

The unattended default listener is `http://0.0.0.0:5077`. Set `ASPNETCORE_URLS` to configure custom hostnames, IP addresses, or ports.

## Publish a standalone executable

```powershell
dotnet publish .\Soulstone.SyncServer\Soulstone.SyncServer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Run the generated `Soulstone.SyncServer.exe` directly or install it as a persistent service. No console input or setup wizard is required.

## Container

```powershell
dotnet publish .\Soulstone.SyncServer\Soulstone.SyncServer.csproj -c Release -o .\Soulstone.SyncServer\publish
docker build -t soulstone-sync .\Soulstone.SyncServer
docker run --rm -p 5077:5077 soulstone-sync
```

## REST API & Protocol Reference

### Health Check

- **`GET /health`**
  - **Description**: Verifies relay availability.
  - **Response**: `200 OK` `{"status":"healthy"}`

### Session Relay & Party Sync

- **`POST /api/sessions`**
  - **Description**: Creates a new in-memory sync room session (rate-limited to 10/min per IP).
  - **Response**: `200 OK` JSON with `sessionId`, `hostToken`, and `memberToken`.

- **`PUT /api/sessions/{sessionId}/invite`**
  - **Description**: Registers an opaque, encrypted invite payload.
  - **Headers**: `Authorization: Bearer <hostToken>`, `Content-Type: application/json`
  - **Body**: `{"inviteId": "...", "payload": "..."}`
  - **Response**: `204 NoContent` (or `401 Unauthorized`, `404 NotFound`, `409 Conflict`)

- **`GET /api/invites/{inviteId}`**
  - **Description**: Resolves invite ciphertext for joining a session.
  - **Response**: `200 OK` `{"payload": "..."}` (or `404 NotFound`)

- **`GET /api/sessions/{sessionId}/connect`** (WebSocket Upgrade)
  - **Description**: Establishes a persistent bidirectional WebSocket connection for party sync.
  - **Headers**: `Authorization: Bearer <hostToken|memberToken>`
  - **Protocol**: Encrypted JSON `RelayEnvelope` messages (group-scoped or DM-scoped).

### Character Sheet Cloud Registry

- **`PUT /api/characters/{characterName}`**
- **`PUT /api/characters/{characterName}/{worldName}`**
  - **Description**: Uploads and registers a character sheet JSON payload in memory.
  - **Headers**: `Content-Type: application/json`
  - **Body**: Serialized `CharacterSheet` JSON payload.
  - **Response**: `204 NoContent` on success, `400 BadRequest` if empty or invalid.

- **`GET /api/characters/{characterName}`**
- **`GET /api/characters/{characterName}/{worldName}`**
  - **Description**: Retrieves a registered character sheet JSON profile.
  - **Response**: `200 OK` with `application/json` payload, or `404 NotFound`.

- **`DELETE /api/characters/{characterName}`**
- **`DELETE /api/characters/{characterName}/{worldName}`**
  - **Description**: Removes a registered character sheet from the registry.
  - **Response**: `204 NoContent` on success, `404 NotFound` if not registered.

## Full Deployment & Router Setup Guide

For detailed step-by-step instructions on publishing self-contained executables for Linux/Windows, configuring static IPs and firewall rules, setting up persistent background services (systemd / Windows Service), configuring router port forwarding and Dynamic DNS, or setting up optional TLS/HTTPS reverse proxies (Cloudflare Tunnel, Caddy, NGINX), see:

👉 [**Full Installation & Deployment Guide (`docs/DEPLOYMENT.md`)**](../docs/DEPLOYMENT.md)