# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET 8 console worker (`Microsoft.Extensions.Hosting` `BackgroundService`) that bridges an **Alfen EV charger** (read/write over **Modbus TCP**) to a **KNX** building bus. It polls the charger's Modbus registers, publishes the readings onto KNX group addresses, and writes KNX commands (e.g. max charging current) back to the charger. Intended to run on `linux-arm64` (e.g. a Raspberry Pi) as a long-lived service.

## Commands

All commands use PowerShell. Run from the repo root.

```powershell
dotnet build AlfenHub.sln                              # build
dotnet run --project AlfenHub                          # run locally (uses DOTNET_ENVIRONMENT=development)
dotnet publish AlfenHub -c Release -r linux-arm64 --self-contained   # publish for the Pi
```

There is **no test project** and no linter configured — `dotnet build` is the only verification step.

Configuration lives in `AlfenHub/appsettings.json` (committed defaults, empty hosts) and `appsettings.development.json` (local overrides, gitignored values). Both are copied to output on build. Environment is selected via `DOTNET_ENVIRONMENT` (`development` set in `launchSettings.json`).

## Architecture

The app is wired in `Program.cs`: a generic host registers `Worker` (the hosted service), the Alfen and KNX feature modules (each via its own `AddXxx` `ServiceCollectionExtensions`), and **MediatR** for in-process messaging. MediatR is the spine connecting the two halves — there are no direct calls between the Alfen and KNX modules.

### Data flow (charger → KNX)

1. `Worker.ExecuteAsync` calls `AlfenModbusClient.Start`, which runs a single long-lived loop (interval ~1s).
2. Each tick: reconnect if needed, **write** any pending KNX-originated commands to Modbus, then **read** the charger's holding registers into an `AlfenData` object (`Alfen/Models`).
3. It publishes `AlfenDataArrivedNotification` via MediatR `IPublisher`.
4. `KnxAlfenDataNotificationHandler` receives it; if KNX is enabled, it diffs the new data against the buffer (`KnxValueBufferService`) and sends only **changed** values to the bus via `KnxClient`.

### Data flow (KNX → charger)

1. `KnxClient` subscribes to `GroupMessageReceived` on the KNX bus.
2. `ValueRead` → `KnxReadValueRequest` (returns the buffered value for that group address). `ValueWrite` → `KnxWriteValueRequest`.
3. `KnxWriteValueRequestHandler` maps the group address to a capability string and stores the desired value on `AlfenModbusClient.SocketWritableData` (a shared singleton dictionary).
4. On its next tick, the Modbus loop's `WriteValuesAsync` flushes that to the charger (e.g. writing Modbus Slave Max Current to register 1210).

So `IAlfenModbusClient` is the **shared mutable bridge**: KNX writes stage values onto it; the Modbus loop reads them out. Both `IAlfenModbusClient` and `IKnxValueBufferService` are singletons.

### Capability strings

Both directions key on dotted **capability strings** like `Socket1.RealPowerSum` or `Socket1.SlaveMaxCurrent`. These strings are the contract between the Modbus register layer and KNX group addresses:

- In `appsettings.json` under `KnxOptions.ReadGroupAddresses` / `WriteGroupAddresses`, each capability is mapped to a KNX group address. **An empty group address means that capability is skipped** (see `KnxExtensions.GetReadGroupAddressesFromOptions`).
- `KnxValueBufferService.UpdateValues` builds the capability→value mapping; the read/write request handlers build group-address→capability maps from the same options.
- Most capability strings are generated with `nameof(...)` against the `AlfenData` model tree, so **renaming a property on `AlfenData` silently changes the capability string** and breaks the appsettings mapping. Keep them in sync.

### Modbus register decoding

`AlfenModbusClient.GetAlfenModbusData` reads three register blocks (station status, socket energy measurements, socket status/transaction) by unit/slave address and start address (constants in `AlfenModbusConstants`). `ExtensionMethods.GetSection` slices a block by absolute register address, and `ToFloat`/`ToDouble`/`ToUshort`/`ToMode3State`/`ToTimespan` decode the Alfen word/byte ordering (note the deliberate word-swap in `ToFloat`/`ToDouble` and the reversed byte order when writing to KNX in `KnxClient`/`KnxValueBufferService`). Register numbers and counts come from the Alfen Modbus TCP/IP spec.

Currently only **Socket 1** is fully populated even though the station reports `TotalSockets`; extending to socket 2 means reading the `Socket2SlaveAddress` block and adding `Socket2` to `AlfenData` + the buffer service + appsettings.

## Conventions

- Feature modules (`Alfen/`, `Knx/`) are self-contained, each with `Extensions/ServiceCollectionExtensions.cs` exposing one `AddXxx(configuration)` entry point. New cross-module communication goes through a MediatR notification or request, not a direct dependency.
- Options classes (`AlfenModbusOptions`, `KnxOptions`) bind from the config section named after the class (`configuration.GetSection(nameof(...))`).
- Most types are `internal`.
- Note: `KnxWriteValueRequestHandler .cs` has a stray space in its filename.
