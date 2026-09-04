# Soulstone Sync Server

This is a standalone, non-interactive ASP.NET Core relay. It stores rooms only in memory and forwards opaque end-to-end encrypted messages; credentials and payloads are never logged.

## Run directly

```powershell
dotnet run --project .\Soulstone.SyncServer\Soulstone.SyncServer.csproj
```

The unattended default is `http://127.0.0.1:5077`. Set `ASPNETCORE_URLS` to choose another listener. Public deployments must terminate TLS and expose this service as HTTPS/WSS; the Soulstone client rejects plain remote HTTP.

## Publish a standalone executable

```powershell
dotnet publish .\Soulstone.SyncServer\Soulstone.SyncServer.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Run the generated `Soulstone.SyncServer.exe` directly or install it as a service. No console input or setup wizard is required.

## Container

```powershell
dotnet publish .\Soulstone.SyncServer\Soulstone.SyncServer.csproj -c Release -o .\Soulstone.SyncServer\publish
docker build -t soulstone-sync .\Soulstone.SyncServer
docker run --rm -p 127.0.0.1:5077:5077 soulstone-sync
```

Use a TLS reverse proxy for internet-facing deployments. `GET /health` is available for health checks.

## Full Deployment & Router Setup Guide

For detailed step-by-step instructions on publishing self-contained executables for Linux/Windows, configuring static IPs and firewall rules, setting up persistent background services (systemd / Windows Service), configuring router port forwarding and Dynamic DNS, and configuring TLS/HTTPS reverse proxies (Cloudflare Tunnel, Caddy, NGINX), see:

👉 [**Full Installation & Deployment Guide (`docs/DEPLOYMENT.md`)**](../docs/DEPLOYMENT.md)