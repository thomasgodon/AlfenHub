# AlfenHub
Alfen charger modbus hub

A .NET 8 worker that bridges an Alfen EV charger (Modbus TCP) to a KNX bus.

## Architecture

The solution follows a clean DDD / layered structure with a single `Charging` bounded
context and two infrastructure adapters:

| Project | Responsibility |
|---|---|
| `AlfenHub.Domain` | The `Charger` aggregate, `Socket` entity, value objects (`ElectricCurrent`, `Power`, …), domain events and the `IChargerGateway` port. No external dependencies. |
| `AlfenHub.Application` | Use cases & orchestration: the polling loop (`ChargerPollingService`), MediatR commands/notifications, the `IBuildingBus` port and the `IChargerControlBuffer`. |
| `AlfenHub.Infrastructure` | The adapters: `AlfenModbusGateway` (Modbus TCP) and `KnxBuildingBus` (KNX/IP), plus register decoding and KNX encoding. |
| `AlfenHub` | Host / composition root: `Worker`, `Program.cs`, `appsettings.json`. |
| `AlfenHub.Tests` | xUnit tests for the domain and the infrastructure conversions. |

Dependency direction is Domain ← Application ← Infrastructure ← Host. The KNX side
never talks to the Modbus side directly: charger state flows out as a MediatR
notification, and inbound KNX writes flow back as a `SetSocketMaxCurrentCommand` that the
polling loop re-applies to the charger.

Every readable register in the Alfen Modbus TCP/IP spec (product identification, station
status and the full per-phase socket measurement + status set, for **both sockets**; the
SCN block is excluded) is surfaced on the web dashboard and, where a group address is
configured, published to KNX with an appropriate datapoint type. KNX read capabilities are
keyed `Station.{name}` / `Socket{id}.{name}` under `KnxOptions.ReadGroupAddresses` in
`appsettings.json` — an empty address skips that capability. Identity strings (name, serial,
firmware, …) are shown on the dashboard only.

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

### docker compose

A `docker-compose.yml` is provided. Edit the `environment` block (Alfen host, KNX
settings, group-address mappings) then:

```bash
docker compose up -d        # pull + run in the background
docker compose logs -f      # follow logs
docker compose down         # stop and remove
```

By default it pulls `ghcr.io/thomasgodon/alfenhub:latest`. To build from source
instead, uncomment the `build` block (and comment out `image`) in the compose file.

## Releasing

Publishing a GitHub release triggers `.github/workflows/release.yml`, which builds the
Docker image and pushes it to `ghcr.io/thomasgodon/alfenhub` tagged with the release
version and `latest`.
