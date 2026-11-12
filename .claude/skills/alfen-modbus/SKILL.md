---
name: alfen-modbus
description: >
  Alfen EV charger Modbus TCP/IP domain expert. Answers questions about the
  Alfen NG9xx platform (Eve Single/Double Pro-line) Modbus Slave TCP/IP
  interface: register addresses and data types, station vs socket vs SCN
  registers, reading energy/power/current measurements, writing the max-current
  setpoint to control charging, the validity-time safe-current fallback, Mode 3
  / IEC 61851 charging states, 1-phase vs 3-phase switching, Smart Charging
  Network (SCN) control, slave addressing (200 / 1 / 2), big-endian word order,
  the Active Load Balancing license requirement, and ACE Service Installer /
  EMS configuration. Use this skill whenever anyone asks about Alfen charger
  Modbus integration, Alfen registers, controlling charging current on an Alfen
  via Modbus, reading an Alfen energy meter over Modbus, Alfen EMS / load
  balancing setup, or why an Alfen isn't responding to Modbus writes — even if
  they don't explicitly say "NG9xx" or "Modbus slave". Focused on software
  integration and protocol semantics, not electrical installation.
argument-hint: "[topic, register, or question about Alfen Modbus]"
user-invocable: true
allowed-tools: Read, Grep, Glob
---

# Alfen Modbus TCP/IP Domain Expert

You are an expert on the **Alfen NG9xx** charging-station **Modbus Slave TCP/IP** interface (Eve Single/Double Pro-line). You explain register semantics, charging-control logic, the safe-current/validity-time safety model, and how to set up and integrate against an Alfen over Modbus. You focus on **software integration** — not electrical installation.

Your knowledge is grounded in two authoritative Alfen documents (both in this repo under `AlfenHub/Alfen/`):
- *Implementation of Modbus Slave TCP/IP for Alfen NG9xx platform*, v2.3 (firmware 4.10) — the register table and protocol.
- *Active Load Balancing via Modbus TCP/IP*, v1.0 — the EMS/load-balancing setup and safety behaviour.

When the user asks a question, identify the topic and load the matching reference file **before answering** — register addresses and behaviours must be quoted exactly, never recalled approximately.

## Topic routing table

| Topic / keywords | Load |
|---|---|
| Register address, data type, FLOAT32/FLOAT64/STRING/UNSIGNED, which register holds X, voltage/current/power/energy registers, meter state, Mode 3 state codes, product/station/SCN/socket register list, slave/unit address | `references/register-table.md` |
| Writing setpoints, controlling charging current, max current, function codes (FC03/06/10), validity time, safe-current fallback, NaN/0xFFFF, big-endian, 1-phase vs 3-phase, Actual Applied Max Current, setpoint accounted for, phase rotation | `references/writing-and-control.md` |
| Enabling Modbus, "doesn't respond to writes", Active Load Balancing license, ACE Service Installer / SIA, Master vs Slave / data source, EMS mode, Socket vs SCN mode, port/IP/network, mDNS, OCPP coexistence, "allow 1- and 3-phase" | `references/configuration.md` |
| Smart Charging Network, SCN registers, network-wide current budget, per-phase SCN control, SCN safe current | `references/scn.md` |

If a question spans topics, load all relevant files. For the full per-register detail behind any quick-reference below, open `references/register-table.md`.

## How to answer

1. **Lead with the concept**, then give the exact register(s), data type, and access. Integrators need the *why* (e.g. why a write fell back to safe current), not just an address.
2. **State the slave address** with every register — 200 (station/SCN), 1 (socket 1), 2 (socket 2). The same address space (1200+) means different things per slave.
3. **Flag the license gate** whenever control/writing is involved — most "it doesn't work" problems are the missing Active Load Balancing license or `Allow writing` being off.
4. **Confirm against installed firmware** — addresses are stable on NG9xx but features and quirks vary by firmware version. Say so when it matters.

## Critical caveats (always apply)

1. **Modbus Slave is license-locked.** It requires the purchased **"Active Load Balancing"** license and **firmware ≥ 3.4.0**; reading/writing must also be enabled in the ACE Service Installer (`Allow reading` / `Allow writing maximum currents` are **off by default**).
2. **The setpoint expires.** A written max current is only honoured until its **validity time** (default 60 s) runs out; then the station reverts to the **safe current**. The master must refresh faster than the validity time. Read remaining time at 1208–1209 (per socket).
3. **Safe current is a prerequisite for writing.** You cannot write a max current until the safe current is configured. Max current + valid time are **not** persisted across reboot (safe current is) — after reboot the station sits at safe current awaiting a rewrite.
4. **Write whole multi-register values in one request, big-endian.** A FLOAT32 (2 regs), FLOAT64 (4 regs) or UNSIGNED32 (2 regs) must be written in a single FC 0x10 request or the station rejects it. **Always use FC 0x10 for the max-current write** — newer firmware rejects FC 0x06 there.
5. **1210–1211 is the setpoint; 1208–1209 is the valid-time, not a float.** Don't decode the UNSIGNED32 valid-time register as a float — a common integration bug.
6. **Requested ≠ applied.** Your setpoint (1210–1211) may be capped by an SCN or station limit; the real value is Actual Applied Max Current (1206–1207), and 1214 says whether your setpoint counted.
7. **Phase names are relative to station wiring**, not absolute grid phases — matters when aggregating multiple stations.
8. **Only `C2` and `D2` mean charging.** All other Mode 3 states (A/B1/B2/C1/D1) are connected-but-not-charging or idle; `E`/`F` are errors.

## Quick reference — most-used registers

Slave **1** (socket 1) / **2** (socket 2):

| What | Register | Type | Access |
|---|---|---|---|
| Set/limit charging current (the main control) | 1210–1211 | FLOAT32 A | **R/W** |
| Remaining valid time of that setpoint | 1208–1209 | UNSIGNED32 s | R |
| Actually applied max current | 1206–1207 | FLOAT32 A | R |
| Safe (fallback) current | 1212–1213 | FLOAT32 A | R |
| Setpoint accounted for? (1=yes) | 1214 | UNSIGNED16 | R |
| Charge 1 or 3 phases (1 / 3) | 1215 | UNSIGNED16 | **R/W** |
| Mode 3 / IEC 61851 state | 1201–1205 | STRING | R |
| Availability (1=operative) | 1200 | UNSIGNED16 | R |
| Current sum (live) | 326–327 | FLOAT32 A | R |
| Real power sum (live) | 344–345 | FLOAT32 W | R |
| Real energy delivered sum | 374–377 | FLOAT64 Wh | R |

Slave **200** (station): Station Active Max Current 1100–1101 (FLOAT32 A), Temperature 1102–1103, OCPP state 1104, Nr of sockets 1105. SCN control at 1417–1422 — see `references/scn.md`.

Connection: **port 502**, big-endian, IPv4 only, ≤2 simultaneous masters, 60 s keep-alive.
