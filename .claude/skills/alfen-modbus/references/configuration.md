# Enabling & configuring Modbus Slave TCP/IP on an Alfen NG9xx station

Sources: the NG9xx implementation spec (v2.3, 2020-10-30) and "Active Load Balancing via Modbus TCP/IP" (v1.0, 2018). Modbus Slave TCP/IP is the transport Alfen uses for its **Active Load Balancing** feature with an external Energy Management System.

## The license gate (read this first)

Modbus Slave functionality is **locked behind the "Active Load Balancing" license**. It cannot be enabled at all until that feature has been purchased and the station's unique license key updated. This is the single most common reason "Modbus doesn't work" on an Alfen — the registers simply won't respond to control without the license.

Active Load Balancing via Modbus TCP/IP requires **firmware 3.4.0 or higher** (the implementation spec itself was validated on firmware 4.10).

## Configuration tool: ACE Service Installer (SIA)

All Modbus configuration is done with the **Alfen ACE Service Installer Application (SIA)** — there is no web UI for it. You need:
- An **administrator** account, **or** a **service** account with SIA version 4.0+.
- An on-site connection (typically Ethernet) to the charging station.
- Request an account at `https://support.alfen.com` if you don't have one. Download SIA from `https://alfen.com/downloads`.

These settings are **persistent across reboots**.

## Master vs Slave: pick the data source

Under **Load Balancing → Active balancing → Data Source**:

- **Meter** → the station takes the **Master** role. It reads a connected energy meter (e.g. Socomec Countis E27 over Modbus TCP/IP) directly and calculates its own available charging budget, treating other loads as higher priority. *This is the station acting as a Modbus master/client — not what you want for EMS control.*
- **Energy Management System (EMS)** → the station takes the **Slave** role. An external EMS sends setpoints; the station ramps up/down on command. **This is the mode that exposes the Modbus Slave TCP/IP registers for an external master to write.**

When you select EMS, a **TCP/IP EMS** menu appears in SIA. Modbus TCP/IP is the only protocol available there.

## EMS-mode options

| Setting | Meaning |
|---|---|
| **Mode: Socket** | Control each socket individually (write to slave 1 / slave 2 registers, e.g. Modbus Slave Max Current @1210). |
| **Mode: SCN** | Control the whole station or an entire Smart Charging Network as one entity (write the SCN per-phase registers @1417–1422 on slave 200). See `scn.md`. |
| **Validity time (s)** | After this many seconds without a fresh setpoint write, the station assumes the EMS is gone and reverts to the **safe current**. Default **60 s**. Poll/refresh faster than this. |
| **Allow reading** | Enables reading registers over TCP/IP. **Off by default.** |
| **Allow writing maximum currents** | Enables writing the max-current registers. **Off by default.** |
| **Enable sockets** | Station takes written per-socket max currents into account. |
| **Enable SCN** | Station takes written SCN max currents into account. |

## Safe current — a prerequisite for writing

The **safe current** (per socket: Active Load Balancing Safe Current @1212–1213; for SCN: @1429–1430) **must be configured before any max current can be written** over Modbus. It is the fallback the station applies when:
- the validity time expires without a refresh, or
- the EMS/meter connection is lost.

The safe current is persistent; the max current and remaining valid time are **not** preserved across a reboot — after a reboot the station sits at the safe current and waits for the master to (re)write the max current.

## Network & connectivity

- **Port 502** (standard Modbus TCP). UDP is **not** supported.
- The master connects to the IP of the station's **wired Ethernet** connection.
- **IPv4 only** — Alfen charging equipment does not support IPv6.
- Up to **two Modbus masters** may be connected simultaneously.
- **60-second keep-alive**: if no read/write arrives within 60 s, the station closes the master connection.
- Power over Ethernet is not supported; use CAT5e (≤100 m) or CAT6 (>100 m).
- The station can use DHCP or a fixed IP. A fixed IP is recommended so config is one-and-done. To discover a DHCP station, use **mDNS**: service type `_alfen._tcp.local` (HTTP on port 80) returns the model-serial (e.g. `NG920-61001-ACE0012345`) and station identifier.

## "Allow 1- and 3-phase charging"

A separate checkbox in the Active balancing section. It **must be ticked locally in SIA** for an EMS to switch a socket between single-phase and three-phase charging (register 1215). Without it, phase-switching writes are ignored.

## OCPP coexistence

The station can be connected to an OCPP back office and still serve Modbus. Station register 1104 (OCPP state) reports whether a back office is connected. The two are independent control paths — be aware that an OCPP backend issuing its own limits can interact with Modbus setpoints.
