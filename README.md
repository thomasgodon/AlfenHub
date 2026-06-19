# AlfenHub
Alfen charger modbus hub

A .NET 8 worker that bridges an Alfen EV charger (Modbus TCP) to a KNX bus.

## Running in Docker

Images are published to GitHub Container Registry on every GitHub release:

```bash
docker pull ghcr.io/thomasgodon/alfenhub:latest
```

Configuration is supplied at runtime via environment variables (the image ships only
empty-host defaults). Variables map to config keys using .NET's `__` section separator:

| Env var | Description | Example |
|---|---|---|
| `AlfenModbusOptions__Host` | Alfen charger IP | `192.168.0.19` |
| `AlfenModbusOptions__Port` | Modbus TCP port | `502` |
| `KnxOptions__Enabled` | Enable KNX bridging | `true` |
| `KnxOptions__Host` | KNX/IP gateway IP | `192.168.0.16` |
| `DOTNET_ENVIRONMENT` | Hosting environment | `Production` (default) |

```bash
docker run -d --name alfenhub \
  -e AlfenModbusOptions__Host=192.168.0.19 \
  -e KnxOptions__Enabled=true \
  -e KnxOptions__Host=192.168.0.16 \
  ghcr.io/thomasgodon/alfenhub:latest
```

> The image targets `linux/amd64`. KNX group-address mappings still come from the
> baked `appsettings.json`; override individual keys with `KnxOptions__...` env vars as needed.

## Releasing

Publishing a GitHub release triggers `.github/workflows/release.yml`, which builds the
Docker image and pushes it to `ghcr.io/thomasgodon/alfenhub` tagged with the release
version and `latest`.
