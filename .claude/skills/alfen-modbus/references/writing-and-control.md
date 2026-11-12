# Reading, writing & charging control

Source: NG9xx implementation spec v2.3 (2020-10-30). All addresses cross-referenced in `register-table.md`.

## Function codes & framing

| Function code | Use |
|---|---|
| `0x03` Read Holding Registers | All reads. Multiple registers per request, as long as they are **contiguous**. |
| `0x06` Write Single Register | Single 16-bit register writes. |
| `0x10` Write Multiple Registers | Multi-register writes — **required** for any multi-register value (FLOAT32, FLOAT64, UNSIGNED32). |

Rules that bite if ignored:

- **Big-endian / network byte order**, always — both reads and writes.
- **A multi-register value must be written in a single request.** A FLOAT32 spans two 16-bit registers; both must be written in one FC 0x10 request. If you write only part of it, the station **rejects the whole request with a Modbus error**. The same applies to FLOAT64 (4 regs) and UNSIGNED32 (2 regs).
- Reserved or unavailable registers read back as **NaN = `0xFFFF`** per 16-bit register.
- `STRING` registers hold two ASCII bytes each and end with a trailing zero.
- A written value is only logged by the station if it **differs** from the current value.

> **Firmware caveat (community-reported, newer firmware):** some recent firmware (≈7.3.0) **rejects FC 0x06 on the max-current registers (1210/1211)** and requires FC 0x10 (write multiple) even for what looks like a single value. Since the max current is a FLOAT32 spanning 1210–1211 anyway, **always write it with FC 0x10** — that is both spec-correct and firmware-robust.

## Writing the per-socket max current (the core control)

To throttle/enable charging on a socket, write **Modbus Slave Max Current @1210–1211 (FLOAT32, amps)** on slave 1 (socket 1) or slave 2 (socket 2).

The mechanics:

1. **Safe current must already be configured** (register 1212–1213, set in SIA) — you cannot write a max current otherwise. See `configuration.md`.
2. Each write to 1210–1211 **resets that setpoint's remaining valid time** to the configured validity time. Read it back at **1208–1209 (UNSIGNED32 seconds)**. Example: validity time 60 s, last write 10 s ago → 1208–1209 reads `50`.
3. If you **stop refreshing** and the valid time reaches 0, the station **falls back to the safe current**. This is a safety feature: an EMS that crashes can't leave a socket stuck at a high limit. **Poll/refresh faster than the validity time** (default 60 s).
4. The value you write is the *requested* limit. The **actually applied** limit is **Actual Applied Max Current @1206–1207** — it can be lower if an SCN limit or station limit is more restrictive. **Register 1214** tells you whether your setpoint was taken into account (1 = yes).
5. After a station reboot, the max current and valid time are **not** restored — the station sits at safe current until the master rewrites the setpoint.

### Current value semantics

- Units are **amps** (FLOAT32, step 1 A).
- A value **below ~6 A** does not charge — IEC 61851 has a 6 A minimum; setting below it effectively pauses charging. Writing `0` is the way to suspend.
- The value must not exceed the station/socket hardware rating or the configured station max current (register 1100–1101).
- The car's actual draw also depends on the vehicle's own ramp behaviour — a new setpoint is not applied instantaneously.

## Switching 1-phase vs 3-phase (register 1215)

- Write **1 = single-phase**, **3 = three-phase** to **register 1215 (UNSIGNED16, R/W)** on the socket's slave address.
- This only works if **"Allow 1- and 3-phase charging" is ticked in SIA** (see `configuration.md`). Otherwise the write is ignored.
- Phase switching mid-session depends on vehicle support and may interrupt charging briefly.

## Reading charging state

- **Mode 3 state @1201–1205 (STRING)** — the IEC 61851 state. Only `C2` / `D2` mean current is flowing. See the Mode 3 listing in `register-table.md`.
- **Availability @1200** — 1 = operative, 0 = inoperative.
- **Real Power Sum @344–345** and **Current Sum @326–327** — live power/current draw.
- **Real Energy Delivered Sum @374–377 (FLOAT64, Wh)** — cumulative energy delivered to vehicles (the session/lifetime meter).

## Phase rotation gotcha

Phase names (L1/L2/L3) in the registers are **relative to the station's incoming phase wiring**, not absolute grid phases. Two stations wired L1-L2-L3 vs L2-L3-L1 will report the same physical grid phase under different register names. When a master aggregates multiple stations, account for each station's wiring order — don't assume "L1 register = grid L1" across stations.
