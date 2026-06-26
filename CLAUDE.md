# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 8 console worker (`Microsoft.Extensions.Hosting` `BackgroundService`) that bridges an **Alfen EV charger** (read/write over **Modbus TCP**) to a **KNX** building bus. It polls the charger's Modbus registers, publishes the readings onto KNX group addresses, and writes KNX commands (e.g. max charging current) back to the charger. Intended to run on `linux-arm64` (e.g. a Raspberry Pi) as a long-lived service.

The codebase is organised as a **clean DDD / layered solution**: a single `Charging` bounded context (`AlfenHub.Domain`), application use cases (`AlfenHub.Application`), the Modbus + KNX adapters (`AlfenHub.Infrastructure`), and the host (`AlfenHub`). Dependency direction is **Domain ← Application ← Infrastructure ← Host**.

## Commands

All commands use PowerShell. Run from the repo root.

```powershell
dotnet build AlfenHub.sln                              # build
dotnet test AlfenHub.Tests                             # run unit tests (domain + infra conversions)
dotnet run --project AlfenHub                          # run locally (uses DOTNET_ENVIRONMENT=development)
dotnet publish AlfenHub -c Release -r linux-arm64 --self-contained   # publish for the Pi
```

`AlfenHub.Tests` (xUnit) covers the domain value objects, the `Charger` aggregate's events, the Modbus register decode/encode (`ModbusRegisterExtensions`) and the KNX encoding (`KnxReadingEncoder`). It reaches `internal` infrastructure types via `InternalsVisibleTo`. No linter is configured — `dotnet build` + `dotnet test` are the verification steps.

### Docker

A root `Dockerfile` builds a multi-stage, framework-dependent image (`mcr.microsoft.com/dotnet/runtime:8.0` runtime stage — plain runtime, not `aspnet`, since this is a worker).

```powershell
docker build -t alfenhub:test .                        # build image locally (linux/amd64)
docker run --rm -e AlfenModbusOptions__Host=192.168.0.19 alfenhub:test   # run; logs to stdout
```

Config is overridden at runtime via environment variables using .NET's `__` section separator (e.g. `AlfenModbusOptions__Host`, `KnxOptions__Host`). No real hosts/secrets are baked into the image — `appsettings.development.json` is excluded via `.dockerignore`, and `DOTNET_ENVIRONMENT` defaults to `Production`.

`.github/workflows/release.yml` builds and pushes the image to GHCR (`ghcr.io/thomasgodon/alfenhub`) whenever a GitHub **release** is published (also runnable via `workflow_dispatch`). It tags the image with the release semver + `latest`, authenticating with the built-in `GITHUB_TOKEN` (no extra secrets).

Configuration lives in `AlfenHub/appsettings.json` (committed defaults, empty hosts) and `appsettings.development.json` (local overrides, gitignored values). Both are copied to output on build. Environment is selected via `DOTNET_ENVIRONMENT` (`development` set in `launchSettings.json`).

## Architecture

Four projects plus tests, wired in `Program.cs`: the host registers `Worker`, then `AddApplication()` (MediatR + the polling service + the control buffer) and `AddInfrastructure(configuration)` (the Modbus and KNX adapters). **MediatR** lives at the application layer and is the spine connecting the two adapters — there are no direct calls between the Modbus and KNX sides.

- **`AlfenHub.Domain`** — the `Charging` bounded context. `Charger` aggregate root, `Socket` entity, value objects (`ElectricCurrent`, `Voltage`, `Power`, `Energy`, `Frequency`, `PowerFactor`, `Temperature`, `SocketId`, `MaxCurrentSetpoint`), `Mode3State` enum, the `ChargerStateRefreshed` domain event, and the `IChargerGateway` repository port. No MediatR / FluentModbus / Knx.Falcon references.
- **`AlfenHub.Application`** — `ChargerPollingService` (the loop), the `IBuildingBus` port, the `IChargerControlBuffer` (replaces the old shared-mutable bridge), MediatR commands/notifications and handlers, and `MediatRDomainEventDispatcher` (republishes domain events as MediatR notifications so the domain stays messaging-free).
- **`AlfenHub.Infrastructure`** — `AlfenModbusGateway : IChargerGateway` (Modbus TCP, register decode/encode in `ModbusRegisterExtensions`, constants in `AlfenModbusConstants`) and `KnxBuildingBus : IBuildingBus` (Falcon `KnxBus`, capability↔group-address maps, byte encoding in `KnxReadingEncoder`, the diff/last-sent buffer). Both options classes (`AlfenModbusOptions`, `KnxOptions`) live here.
- **`AlfenHub`** — host / composition root: `Worker`, `Program.cs`, `appsettings*.json`.

### Data flow (charger → KNX)

1. `Worker.ExecuteAsync` runs `ChargerPollingService.RunAsync` — a single long-lived loop (`ChargerPollingOptions.PollInterval`, ~1s).
2. Each tick: re-apply any pending setpoints via `IChargerGateway.WriteMaxCurrentAsync`, then read a fresh `Charger` snapshot via `IChargerGateway.GetAsync` (the `AlfenModbusGateway` reconnects lazily).
3. Building the `Charger` raises a `ChargerStateRefreshed` domain event; `MediatRDomainEventDispatcher` publishes it as a `ChargerStateRefreshedNotification`.
4. `ChargerStateRefreshedNotificationHandler` receives it; if `IBuildingBus.IsEnabled`, `KnxBuildingBus` diffs the readings against its buffer and sends only **changed** values to the bus.

### Data flow (KNX → charger)

1. `KnxBuildingBus` subscribes to `GroupMessageReceived` on the KNX bus (on first connect).
2. `ValueRead` → answered directly from its own buffer (no MediatR round-trip). `ValueWrite` → mapped to a capability string.
3. For `Socket1.SlaveMaxCurrent`, it decodes the float (reversing byte order) and sends a `SetSocketMaxCurrentCommand` via MediatR `ISender`.
4. `SetSocketMaxCurrentCommandHandler` stores the setpoint in `IChargerControlBuffer`. The polling loop re-asserts it to register 1210 on every tick.

The **`IChargerControlBuffer`** replaces the old `AlfenModbusClient.SocketWritableData` shared-mutable singleton. It **retains** the latest setpoint per socket (rather than draining it) because an Alfen charger falls back to its safe current once the Modbus max-current validity time elapses, so the loop must re-write it each cycle.

### Capability strings

Both directions key on dotted **capability strings** like `Socket1.RealPowerSum` or `Socket1.SlaveMaxCurrent` — the contract between charger readings and KNX group addresses:

- In `appsettings.json` under `KnxOptions.ReadGroupAddresses` / `WriteGroupAddresses`, each capability maps to a KNX group address. **An empty group address means that capability is skipped** (see `KnxOptionsExtensions.GetReadGroupAddressesFromOptions`).
- The capability strings are defined as literals in `KnxCapabilities` and produced by `KnxReadingEncoder.Encode`. They are still **socket-1-specific** and string-coupled to the appsettings keys (a known TODO — see `KnxCapabilities`).

### Modbus register decoding

`AlfenModbusGateway.GetAsync` reads three register blocks (station status, socket energy measurements, socket status/transaction) by unit/slave address and start address (constants in `AlfenModbusConstants`). `ModbusRegisterExtensions.GetSection` slices a block by absolute register address, and `ToFloat`/`ToDouble`/`ToUshort`/`ToMode3State`/`ToTimespan` decode the Alfen word/byte ordering (note the deliberate word-swap in `ToFloat`/`ToDouble`, and the reversed byte order when writing to KNX in `KnxBuildingBus.SendValuesAsync`). Register numbers and counts come from the Alfen Modbus TCP/IP spec.

Currently only **Socket 1** is populated even though the station reports `TotalSockets`; extending to socket 2 means reading the `Socket2SlaveAddress` block in `AlfenModbusGateway`, adding a second `Socket` to the `Charger`, and generalising the socket-1-specific `KnxCapabilities` + appsettings.

## Conventions

- Each non-domain layer exposes one DI entry point: `AddApplication()` and `AddInfrastructure(configuration)` in `Extensions/ServiceCollectionExtensions.cs`. New cross-adapter communication goes through a MediatR command/notification or a domain port, never a direct dependency.
- The domain raises plain `IDomainEvent`s; the application bridges them onto MediatR. Keep the domain free of MediatR / FluentModbus / Knx.Falcon.
- Options classes (`AlfenModbusOptions`, `KnxOptions`) bind from the config section named after the class (`configuration.GetSection(nameof(...))`); `ChargerPollingOptions` binds from the `AlfenModbusOptions` section (it shares the `PollInterval` key).
- Most types are `internal`; the test project sees infrastructure internals via `InternalsVisibleTo`.
