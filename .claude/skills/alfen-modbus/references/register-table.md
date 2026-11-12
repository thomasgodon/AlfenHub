# Alfen NG9xx Modbus Slave TCP/IP — Register Table

Source of truth: **"Implementation of Modbus Slave TCP/IP for Alfen NG9xx platform", v2.3 (2020-10-30)**, based on charging-station firmware 4.10 / Service Installer 3.4.10-130. Register addresses are stable across the NG9xx platform but always confirm against the firmware actually installed.

## Slave (unit) addressing

| Slave address | Scope |
|---|---|
| `200` | Station-level registers: product identification, station status, SCN |
| `1` | Socket 1 (the only socket on a single-socket station, or the **left** socket on a dual-socket station) |
| `2` | Socket 2 (the **right** socket on a dual-socket station, if present) |

All registers are **holding registers** read with FC 0x03. Everything is **big-endian (network byte order)**. A reserved/unavailable 16-bit register reads `0xFFFF` (NaN).

`STRING` registers pack **two 8-bit ASCII chars per 16-bit register** and always include a trailing zero byte.

---

## 3.1 Product identification (slave 200, read-only)

| Register | Addr (start–end) | # regs | Type | Unit | Notes / example |
|---|---|---|---|---|---|
| Name | 100–116 | 17 | STRING | — | e.g. `"ALF_1000"` |
| Manufacturer | 117–121 | 5 | STRING | — | `"Alfen NV"` |
| Modbus table version | 122 | 1 | SIGNED16 | — | e.g. `1` |
| Firmware version | 123–139 | 17 | STRING | — | e.g. `"3.4.0-2990"` |
| Platform type | 140–156 | 17 | STRING | — | e.g. `"NG910"` |
| Station serial number | 157–167 | 11 | STRING | — | e.g. `"00000R000"` |
| Date — year | 168 | 1 | SIGNED16 | 1 yr | |
| Date — month | 169 | 1 | SIGNED16 | 1 mon | |
| Date — day | 170 | 1 | SIGNED16 | 1 d | |
| Time — hour | 171 | 1 | SIGNED16 | 1 hr | |
| Time — minute | 172 | 1 | SIGNED16 | 1 min | |
| Time — second | 173 | 1 | SIGNED16 | 1 s | |
| Uptime | 174–177 | 4 | UNSIGNED64 | 0.001 s | milliseconds since boot |
| Time zone | 178 | 1 | SIGNED16 | 1 min | offset to UTC in minutes |

---

## 3.2 Station status (slave 200, read-only)

| Register | Addr | # regs | Type | Unit | Notes |
|---|---|---|---|---|---|
| Station Active Max Current | 1100–1101 | 2 | FLOAT32 | 1 A | the actual station max current |
| Temperature | 1102–1103 | 2 | FLOAT32 | 1 °C | controller board temperature, **not** ambient |
| OCPP state | 1104 | 1 | UNSIGNED16 | — | whether a back office (OCPP) is connected |
| Nr of sockets | 1105 | 1 | UNSIGNED16 | — | number of sockets on the station |

---

## 3.3 SCN — Smart Charging Network (slave 200)

Present/meaningful only when the station is part of an SCN. See `scn.md` for the concept.

| Register | Addr | # regs | Type | Access | Unit | Notes |
|---|---|---|---|---|---|---|
| SCN name | 1400–1403 | 4 | STRING | R | — | |
| SCN Sockets | 1404 | 1 | UNSIGNED16 | R | — | number of configured sockets in the SCN |
| SCN Total Consumption Phase L1 | 1405–1406 | 2 | FLOAT32 | R | 1 A | |
| SCN Total Consumption Phase L2 | 1407–1408 | 2 | FLOAT32 | R | 1 A | |
| SCN Total Consumption Phase L3 | 1409–1410 | 2 | FLOAT32 | R | 1 A | |
| SCN Actual Max Current Phase L1 | 1411–1412 | 2 | FLOAT32 | R | 1 A | |
| SCN Actual Max Current Phase L2 | 1413–1414 | 2 | FLOAT32 | R | 1 A | |
| SCN Actual Max Current Phase L3 | 1415–1416 | 2 | FLOAT32 | R | 1 A | |
| SCN Max Current per Phase L1 | 1417–1418 | 2 | FLOAT32 | **R/W** | 1 A | EMS setpoint for the whole network, phase L1 |
| SCN Max Current per Phase L2 | 1419–1420 | 2 | FLOAT32 | **R/W** | 1 A | |
| SCN Max Current per Phase L3 | 1421–1422 | 2 | FLOAT32 | **R/W** | 1 A | |
| Remaining valid time — Max Current Phase L1 | 1423–1424 | 2 | UNSIGNED32 | R | 1 s | time left before fallback to safe current |
| Remaining valid time — Max Current Phase L2 | 1425–1426 | 2 | UNSIGNED32 | R | 1 s | |
| Remaining valid time — Max Current Phase L3 | 1427–1428 | 2 | UNSIGNED32 | R | 1 s | |
| SCN Safe current | 1429–1430 | 2 | FLOAT32 | R | 1 A | configured SCN safe current |
| SCN Modbus Slave Max Current enable | 1431 | 1 | UNSIGNED16 | R | — | 1 = enabled, 0 = disabled |

---

## 3.4 Socket measurement (slave 1 or 2, read-only)

Energy-meter readings for the socket. Voltages/currents/power are `FLOAT32` (2 regs); energy counters are `FLOAT64` (4 regs).

| Register | Addr | # regs | Type | Unit | Notes |
|---|---|---|---|---|---|
| Meter state | 300 | 1 | UNSIGNED16 | — | **bitmask** — see below |
| Meter last value timestamp | 301–304 | 4 | UNSIGNED64 | 0.001 s | ms since last received measurement |
| Meter type | 305 | 1 | UNSIGNED16 | — | 0=RTU, 1=TCP/IP, 2=UDP, 3=P1, 4=other |
| Voltage V(L1–N) | 306–307 | 2 | FLOAT32 | 1 V | |
| Voltage V(L2–N) | 308–309 | 2 | FLOAT32 | 1 V | |
| Voltage V(L3–N) | 310–311 | 2 | FLOAT32 | 1 V | |
| Voltage V(L1–L2) | 312–313 | 2 | FLOAT32 | 1 V | |
| Voltage V(L2–L3) | 314–315 | 2 | FLOAT32 | 1 V | |
| Voltage V(L3–L1) | 316–317 | 2 | FLOAT32 | 1 V | |
| Current N | 318–319 | 2 | FLOAT32 | 1 A | |
| Current Phase L1 | 320–321 | 2 | FLOAT32 | 1 A | |
| Current Phase L2 | 322–323 | 2 | FLOAT32 | 1 A | |
| Current Phase L3 | 324–325 | 2 | FLOAT32 | 1 A | |
| Current Sum | 326–327 | 2 | FLOAT32 | 1 A | |
| Power Factor Phase L1 | 328–329 | 2 | FLOAT32 | — | |
| Power Factor Phase L2 | 330–331 | 2 | FLOAT32 | — | |
| Power Factor Phase L3 | 332–333 | 2 | FLOAT32 | — | |
| Power Factor Sum | 334–335 | 2 | FLOAT32 | — | |
| Frequency | 336–337 | 2 | FLOAT32 | 1 Hz | |
| Real Power Phase L1 | 338–339 | 2 | FLOAT32 | 1 W | |
| Real Power Phase L2 | 340–341 | 2 | FLOAT32 | 1 W | |
| Real Power Phase L3 | 342–343 | 2 | FLOAT32 | 1 W | |
| Real Power Sum | 344–345 | 2 | FLOAT32 | 1 W | |
| Apparent Power Phase L1 | 346–347 | 2 | FLOAT32 | 1 VA | |
| Apparent Power Phase L2 | 348–349 | 2 | FLOAT32 | 1 VA | |
| Apparent Power Phase L3 | 350–351 | 2 | FLOAT32 | 1 VA | |
| Apparent Power Sum | 352–353 | 2 | FLOAT32 | 1 VA | |
| Reactive Power Phase L1 | 354–355 | 2 | FLOAT32 | 1 VAr | |
| Reactive Power Phase L2 | 356–357 | 2 | FLOAT32 | 1 VAr | |
| Reactive Power Phase L3 | 358–359 | 2 | FLOAT32 | 1 VAr | |
| Reactive Power Sum | 360–361 | 2 | FLOAT32 | 1 VAr | |
| Real Energy Delivered Phase L1 | 362–365 | 4 | FLOAT64 | 1 Wh | |
| Real Energy Delivered Phase L2 | 366–369 | 4 | FLOAT64 | 1 Wh | |
| Real Energy Delivered Phase L3 | 370–373 | 4 | FLOAT64 | 1 Wh | |
| Real Energy Delivered Sum | 374–377 | 4 | FLOAT64 | 1 Wh | energy delivered **to the vehicle** |
| Real Energy Consumed Phase L1 | 378–381 | 4 | FLOAT64 | 1 Wh | |
| Real Energy Consumed Phase L2 | 382–385 | 4 | FLOAT64 | 1 Wh | |
| Real Energy Consumed Phase L3 | 386–389 | 4 | FLOAT64 | 1 Wh | |
| Real Energy Consumed Sum | 390–393 | 4 | FLOAT64 | 1 Wh | |
| Apparent Energy Phase L1 | 394–397 | 4 | FLOAT64 | 1 VAh | |
| Apparent Energy Phase L2 | 398–401 | 4 | FLOAT64 | 1 VAh | |
| Apparent Energy Phase L3 | 402–405 | 4 | FLOAT64 | 1 VAh | |
| Apparent Energy Sum | 406–409 | 4 | FLOAT64 | 1 VAh | |
| Reactive Energy Phase L1 | 410–413 | 4 | FLOAT64 | 1 VArh | |
| Reactive Energy Phase L2 | 414–417 | 4 | FLOAT64 | 1 VArh | |
| Reactive Energy Phase L3 | 418–421 | 4 | FLOAT64 | 1 VArh | |
| Reactive Energy Sum | 422–425 | 4 | FLOAT64 | 1 VArh | |

### Meter state bitmask (register 300)

| Bit | Value | Meaning |
|---|---|---|
| 0 | `0x01` | Initialised |
| 1 | `0x02` | Updated |
| 2 | `0x04` | Warning |
| 3 | `0x08` | Error |

---

## 3.4 Socket status & transaction (slave 1 or 2)

This is where charging control lives — see `writing-and-control.md`.

| Register | Addr | # regs | Type | Access | Unit | Notes |
|---|---|---|---|---|---|---|
| Availability | 1200 | 1 | UNSIGNED16 | R | — | 1 = operative, 0 = inoperative |
| Mode 3 state | 1201–1205 | 5 | STRING | R | — | IEC 61851 state code — see listing below |
| Actual Applied Max Current | 1206–1207 | 2 | FLOAT32 | R | 1 A | the max current actually applied to the socket |
| Modbus Slave Max Current valid time | 1208–1209 | 2 | UNSIGNED32 | R | 1 s | remaining time before fallback to safe current |
| Modbus Slave Max Current | 1210–1211 | 2 | FLOAT32 | **R/W** | 1 A | the EMS setpoint for this socket |
| Active Load Balancing Safe Current | 1212–1213 | 2 | FLOAT32 | R | 1 A | configured safe (fallback) current |
| Modbus Slave received setpoint accounted for | 1214 | 1 | UNSIGNED16 | R | — | 1 = yes, 0 = no (see note) |
| Charge using 1 or 3 phases | 1215 | 1 | UNSIGNED16 | **R/W** | phases | 1 = single-phase, 3 = three-phase charging |

> **Register 1214** indicates whether the received Max Current setpoint (1210–1211) is being taken into account when computing the Actual Applied Max Current (1206–1207). Depending on other active setpoints (e.g. SCN limits), 1206–1207 may differ from the value you wrote.

> ⚠️ **Do not confuse 1208–1209 with 1210–1211.** 1208–1209 is the *remaining valid time* (`UNSIGNED32` seconds). 1210–1211 is the *setpoint* (`FLOAT32` amps, R/W). Decoding 1208–1209 as a float is a bug.

### 3.4.1 Mode 3 state listing (register 1201–1205, IEC 61851)

The register returns one of these ASCII state codes. The integration-relevant question is usually "is it charging?" — only **C2** and **D2** mean current is flowing.

| State | Vehicle connected | Charging | Meaning |
|---|---|---|---|
| `A` | No | No | No vehicle connected |
| `B1` | Yes | No | Connected, EVSE not yet offering power (no PWM) |
| `B2` | Yes | No | Connected, PWM present, vehicle not yet drawing |
| `C1` | Yes | No | Connected, ready, ventilation not required, not charging |
| `C2` | Yes | **Yes** | **Charging** (no ventilation required) |
| `D1` | Yes | No | Connected, ventilation required, not charging |
| `D2` | Yes | **Yes** | **Charging** (ventilation required) |
| `E` | — | No | Error / no power from EVSE |
| `F` | No | No | Error state — EVSE not available |

> The doc also lists CP signal voltages per state (12 V / 9 V / 6 V / 3 V / 0 V / −12 V), but for software integration the connected/charging columns above are what matter. State `F` is explicitly an error state.
