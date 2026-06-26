# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 8 worker (`Microsoft.Extensions.Hosting` `BackgroundService`) that bridges an **Alfen EV charger** (read/write over **Modbus TCP**) to a **KNX** building bus. It polls the charger's Modbus registers, publishes the readings onto KNX group addresses, and writes KNX commands (e.g. max charging current) back to the charger. It also serves a **read-only web dashboard** (Kestrel, default port **8080**) that live-streams every charger reading to the browser over Server-Sent Events. Intended to run on `linux-arm64` (e.g. a Raspberry Pi) as a long-lived service.

The host is an ASP.NET Core app (`Microsoft.NET.Sdk.Web` + `WebApplication`): the polling worker runs as a hosted service, and Kestrel binds only when the dashboard is enabled (`DashboardOptions.Enabled`). When disabled it binds no port and behaves like the original plain worker.

The codebase is organised as a **clean DDD / layered solution**: a single `Charging` bounded context (`AlfenHub.Domain`), application use cases (`AlfenHub.Application`), the Modbus + KNX adapters (`AlfenHub.Infrastructure`), and the host (`AlfenHub`). Dependency direction is **Domain ← Application ← Infrastructure ← Host**.

## Commands

All commands use PowerShell. Run from the repo root.

```powershell
dotnet build AlfenHub.sln                              # build
dotnet test AlfenHub.Tests                             # run unit tests (domain + infra conversions)
dotnet run --project AlfenHub                          # run locally (uses DOTNET_ENVIRONMENT=development); dashboard at http://localhost:8080
dotnet publish AlfenHub -c Release -r linux-arm64 --self-contained   # publish for the Pi
```

`AlfenHub.Tests` (xUnit) covers the domain value objects, the `Charger` aggregate's events, the Modbus register decode/encode (`ModbusRegisterExtensions`), the KNX encoding (`KnxReadingEncoder`) and the dashboard projection (`DashboardSnapshot.FromCharger`). It reaches `internal` infrastructure types via `InternalsVisibleTo`. No linter is configured — `dotnet build` + `dotnet test` are the verification steps.

### Docker

A root `Dockerfile` builds a multi-stage, framework-dependent image. The runtime stage uses `mcr.microsoft.com/dotnet/aspnet:8.0` (the ASP.NET Core shared framework is required for the Kestrel dashboard) and `EXPOSE`s port 8080.

```powershell
docker build -t alfenhub:test .                        # build image locally (linux/amd64)
docker run --rm -p 8080:8080 -e AlfenModbusOptions__Host=192.168.0.19 alfenhub:test   # run; dashboard on :8080
```

Config is overridden at runtime via environment variables using .NET's `__` section separator (e.g. `AlfenModbusOptions__Host`, `KnxOptions__Host`, `DashboardOptions__Port`). No real hosts/secrets are baked into the image — `appsettings.development.json` is excluded via `.dockerignore`, and `DOTNET_ENVIRONMENT` defaults to `Production`.

`.github/workflows/release.yml` builds and pushes the image to GHCR (`ghcr.io/thomasgodon/alfenhub`) whenever a GitHub **release** is published (also runnable via `workflow_dispatch`). It tags the image with the release semver + `latest`, authenticating with the built-in `GITHUB_TOKEN` (no extra secrets).

Configuration lives in `AlfenHub/appsettings.json` (committed defaults, empty hosts) and `appsettings.development.json` (local overrides, gitignored values). Both are copied to output on build. Environment is selected via `DOTNET_ENVIRONMENT` (`development` set in `launchSettings.json`).

## Architecture

Four projects plus tests, wired in `Program.cs` (`WebApplication.CreateBuilder`): the host registers `Worker`, then `AddApplication()` (MediatR + the polling service + the control buffer + the dashboard broadcaster) and `AddInfrastructure(configuration)` (the Modbus and KNX adapters), binds `ChargerPollingOptions`/`DashboardOptions`, points Kestrel at the dashboard port (only when enabled) and calls `app.MapDashboard()`. **MediatR** lives at the application layer and is the spine connecting the adapters and the dashboard — there are no direct calls between the Modbus, KNX and web sides.

- **`AlfenHub.Domain`** — the `Charging` bounded context. `Charger` aggregate root (station status **and** product-identification: name, serial, firmware, platform, clock, uptime, …), `Socket` entity, value objects (`ElectricCurrent`, `Voltage`, `Power`, `Energy`, `Frequency`, `PowerFactor`, `Temperature`, `SocketId`, `MaxCurrentSetpoint`), `Mode3State` + `MeterType` enums, the `ChargerStateRefreshed` domain event, and the `IChargerGateway` repository port. `EnergyMeasurements` carries the full per-phase (L1/L2/L3) + summed measurement set. No MediatR / FluentModbus / Knx.Falcon references.
- **`AlfenHub.Application`** — `ChargerPollingService` (the loop), the `IBuildingBus` port, the `IChargerControlBuffer` (replaces the old shared-mutable bridge), MediatR commands/notifications and handlers, `MediatRDomainEventDispatcher` (republishes domain events as MediatR notifications so the domain stays messaging-free), and the **`Dashboard/`** projection: `DashboardOptions`, `DashboardSnapshot` (+ `SocketDto`, with the `FromCharger` mapper), `IChargerSnapshotBroadcaster`/`ChargerSnapshotBroadcaster` (in-memory SSE pub/sub) and `ChargerStateRefreshedDashboardHandler`.
- **`AlfenHub.Infrastructure`** — `AlfenModbusGateway : IChargerGateway` (Modbus TCP, register decode/encode in `ModbusRegisterExtensions`, constants in `AlfenModbusConstants`) and `KnxBuildingBus : IBuildingBus` (Falcon `KnxBus`, capability↔group-address maps, byte encoding in `KnxReadingEncoder`, the diff/last-sent buffer). Both options classes (`AlfenModbusOptions`, `KnxOptions`) live here.
- **`AlfenHub`** — host / composition root (ASP.NET Core `Microsoft.NET.Sdk.Web`): `Worker`, `Program.cs`, `appsettings*.json`, `Dashboard/DashboardEndpoints.cs` (`MapDashboard()` — static files + the `/events` SSE endpoint) and `wwwroot/index.html` (the self-contained dashboard page with a day/night toggle).

### Data flow (charger → KNX)

1. `Worker.ExecuteAsync` runs `ChargerPollingService.RunAsync` — a single long-lived loop (`ChargerPollingOptions.PollInterval`, ~1s).
2. Each tick: re-apply any pending setpoints via `IChargerGateway.WriteMaxCurrentAsync`, then read a fresh `Charger` snapshot via `IChargerGateway.GetAsync` (the `AlfenModbusGateway` reconnects lazily).
3. Building the `Charger` raises a `ChargerStateRefreshed` domain event; `MediatRDomainEventDispatcher` publishes it as a `ChargerStateRefreshedNotification`.
4. `ChargerStateRefreshedNotificationHandler` receives it; if `IBuildingBus.IsEnabled`, `KnxBuildingBus` diffs the readings against its buffer and sends only **changed** values to the bus.

### Data flow (charger → web dashboard)

1. The same `ChargerStateRefreshedNotification` (see above) is also handled by `ChargerStateRefreshedDashboardHandler`.
2. If `DashboardOptions.Enabled`, it projects the `Charger` into a flat `DashboardSnapshot` via `DashboardSnapshot.FromCharger` (units normalised: A/V/W/Hz/°C, energy → kWh; `KnxEnabled` comes from `IBuildingBus.IsEnabled`), serializes it (web/camelCase JSON) and publishes it to `IChargerSnapshotBroadcaster`.
3. The broadcaster fans the JSON out to every connected SSE client (drop-oldest bounded channel per client, plus a `Latest` cache).
4. `GET /events` (in `DashboardEndpoints`) streams `data: <json>` frames; `wwwroot/index.html` consumes them via `EventSource` and re-renders. The page is **read-only** — it only reads `/events`, never writes back.

> **Maintenance rule:** when new data is read from the charger (new Modbus registers / new domain fields), the web dashboard MUST be updated to surface it — extend `DashboardSnapshot`/`SocketDto`, the `FromCharger` mapper, and `wwwroot/index.html` in the **same change**.

### Data flow (KNX → charger)

1. `KnxBuildingBus` subscribes to `GroupMessageReceived` on the KNX bus (on first connect).
2. `ValueRead` → answered directly from its own buffer (no MediatR round-trip). `ValueWrite` → mapped to a capability string.
3. For `Socket1.SlaveMaxCurrent`, it decodes the float (reversing byte order) and sends a `SetSocketMaxCurrentCommand` via MediatR `ISender`.
4. `SetSocketMaxCurrentCommandHandler` stores the setpoint in `IChargerControlBuffer`. The polling loop re-asserts it to register 1210 on every tick.

The **`IChargerControlBuffer`** replaces the old `AlfenModbusClient.SocketWritableData` shared-mutable singleton. It **retains** the latest setpoint per socket (rather than draining it) because an Alfen charger falls back to its safe current once the Modbus max-current validity time elapses, so the loop must re-write it each cycle.

### Capability strings

Both directions key on dotted **capability strings** — the contract between charger readings and KNX group addresses. Station-level readings are keyed `Station.{name}` (e.g. `Station.Temperature`); per-socket readings `Socket{id}.{name}` (e.g. `Socket1.RealPowerSum`, `Socket2.CurrentSum`). Identity **strings** (name/serial/firmware/…) are deliberately **not** exposed to KNX — they appear on the dashboard only.

- In `appsettings.json` under `KnxOptions.ReadGroupAddresses` / `WriteGroupAddresses`, each capability maps to a KNX group address. **An empty group address means that capability is skipped** (see `KnxOptionsExtensions.GetReadGroupAddressesFromOptions`). The committed file lists every capability with an empty address.
- Capability suffixes live in `KnxCapabilities` (`StationNames` / `SocketNames`, with `ForStation`/`ForSocket` builders); `KnxReadingEncoder.Encode` emits the station block then every socket. The only **writable** capability is `Socket1.SlaveMaxCurrent`.
- `KnxReadingEncoder` produces **wire-order (big-endian) DPT payloads** via `KnxDpt`: DPT 14 (4-byte float) for analog values, energies as DPT 14 in kWh, 1-byte (DPT 5) for counts/enums/booleans, DPT 13 (4-byte signed) seconds for durations, DPT 8 for the time-zone offset and DPT 19 for the station clock. `KnxBuildingBus.SendValuesAsync` writes these bytes verbatim (no byte reversal); inbound writes still reverse to decode the float.

### Modbus register decoding

`AlfenModbusGateway.GetAsync` reads the **product-identification** block (slave 200, regs 100-178) and the **station-status** block (slave 200, regs 1100-1105), then per socket reads the **energy-measurement** (regs 300-425) and **status/transaction** (regs 1200-1215) blocks. Block addresses/counts are constants in `AlfenModbusConstants` (counts derived from start/end). `ReadRegistersAsync` splits a block into requests of ≤ 125 registers (the Modbus function-3 limit — the 126-register energy block is read as 125 + 1) and concatenates them. `ModbusRegisterExtensions.GetSection` slices a block by absolute register address, and `ToFloat`/`ToDouble`/`ToUshort`/`ToShort`/`ToMode3State`/`ToTimespan`/`ToULong`/`ToMilliseconds`/`ToAsciiString` decode the Alfen word/byte ordering. Register numbers, datatypes and counts come from the Alfen Modbus TCP/IP spec (NG9xx v2.3). The **SCN** register block (1400-1431) is intentionally not read.

**Socket 2** is read only when the station status reports ≥ 2 sockets; the read is guarded so a single-socket station that rejects slave address 2 falls back to one socket. Note the spec correction baked into the decode: the Modbus-slave max-current **setpoint** is regs 1210-1211 (FLOAT32) while its **remaining valid time** is regs 1208-1209 (UNSIGNED32, s). The setpoint is also the write target (`ModbusSlaveMaxCurrentRegister` = 1210).

## Conventions

- Each non-domain layer exposes one DI entry point: `AddApplication()` and `AddInfrastructure(configuration)` in `Extensions/ServiceCollectionExtensions.cs`. New cross-adapter communication goes through a MediatR command/notification or a domain port, never a direct dependency.
- The domain raises plain `IDomainEvent`s; the application bridges them onto MediatR. Keep the domain free of MediatR / FluentModbus / Knx.Falcon.
- Options classes (`AlfenModbusOptions`, `KnxOptions`) bind from the config section named after the class (`configuration.GetSection(nameof(...))`); `ChargerPollingOptions` binds from the `AlfenModbusOptions` section (it shares the `PollInterval` key). `DashboardOptions` binds from its own section in `Program.cs` (the host needs its value pre-build to decide whether to bind Kestrel).
- Most types are `internal`; the test project sees infrastructure internals via `InternalsVisibleTo`.
