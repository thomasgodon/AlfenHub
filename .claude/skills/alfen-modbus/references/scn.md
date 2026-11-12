# Smart Charging Network (SCN)

Source: NG9xx implementation spec v2.3. The SCN registers live on **slave address 200** (station level) — see the SCN table in `register-table.md` (1400–1431).

## Concept

A **Smart Charging Network (SCN)** is a group of Alfen charging stations that coordinate to share a single grid connection's capacity. Instead of an EMS managing each socket independently, the EMS can treat the whole network as **one entity** and set a per-phase current budget for the entire SCN; the stations distribute it among active sessions internally.

This is selected in SIA under EMS mode as **Mode: SCN** (vs **Mode: Socket**). See `configuration.md`.

## Controlling an SCN over Modbus

Write the **SCN Max Current per Phase** registers on slave 200 (all FLOAT32, amps, R/W):

| Phase | Register |
|---|---|
| L1 | 1417–1418 |
| L2 | 1419–1420 |
| L3 | 1421–1422 |

The same validity-time and safe-current safety model as per-socket control applies, but per phase:

- Each phase setpoint has its own **remaining valid time** (UNSIGNED32 s): L1 @1423–1424, L2 @1425–1426, L3 @1427–1428. Refresh before it hits 0 or the SCN falls back to its safe current.
- The **SCN Safe current @1429–1430 (FLOAT32)** is the network-wide fallback. As with sockets, it must be configured before SCN max currents can be written.
- **SCN Modbus Slave Max Current enable @1431 (UNSIGNED16)**: 1 = enabled, 0 = disabled.

## Monitoring an SCN

Read-only (slave 200, FLOAT32 amps unless noted):

- **SCN name @1400–1403** (STRING), **SCN Sockets @1404** (UNSIGNED16, configured socket count).
- **SCN Total Consumption** per phase: L1 @1405–1406, L2 @1407–1408, L3 @1409–1410.
- **SCN Actual Max Current** per phase (what's actually being applied): L1 @1411–1412, L2 @1413–1414, L3 @1415–1416.

## Practical notes

- For SCN load balancing, Alfen strongly recommends a **single energy meter** connected to the network to avoid conflicting information streams.
- The **safe current entered on one station of the SCN represents the available power for the entire SCN**, not just that station.
- Choose Socket mode **or** SCN mode for control — they are different strategies. Use SCN mode when you want to give the network a total budget and let it self-distribute; use Socket mode when your EMS owns the per-socket allocation logic.
