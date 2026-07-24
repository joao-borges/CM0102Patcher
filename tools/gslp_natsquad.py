#!/usr/bin/env python3
"""Raise the 26-player national-squad cap (human squad selection) on CM0102 exes.

RE findings (SESSION-STATE 2026-07-23, this fork's docs/gslp-reconstruction):
The single authority for national squad size is GetNationalSquadSizeLimits at
VA 0x76d430 (national_teams.cpp ~line 2010): WC-family comps -> max=min=23,
squad types 0/2 (full/U21) -> max=26 min=22, other types -> max=24 min=22.
It writes into a per-nation per-type struct (5 slots x 20 bytes; max byte +0x42,
min byte +0x43), and EVERY enforcement reads that field - there is no other 26
literal in national_teams / national_teams_screens / squad_manager code.

The one shared add-player gate is CanAddPlayerToNationalSquad at VA 0x76db00
(national_teams.cpp ~line 2510). Callers: 3x the AI pick function (silent),
2x national_teams_screens.cpp (human UI, with message target). Its fullness
check:

  0x76dd48  lea edi, [esi + edx*4 + 0x42]   ; edi = &maxField
  0x76dd4c  call 0x53c590                   ; al = current squad count (50-slot
                                            ;      club-array scan, no 26 bound)
  0x76dd51  mov cl, byte [edi]              ; cl = max            <-- PATCH 1
  0x76dd56  cmp al, cl / jl ok              ; count < max ?
  ...failure message ("squad full"):
  0x76dd7f  movsx edx, byte [edi]           ; %d in message       <-- PATCH 2

We patch ONLY the validator's max read (and the message's) to NEW_MAX, leaving
the limits function and the AI pick machinery untouched. Why not raise the
limits-function constant instead: the AI pick function consumes the max field
as its selection count AND loop bound over a position template that only has
26/24/23-entry variants (chosen by cmp field,26/24 at 0x76c767/0x76c859) -
a raised field would run the AI off the template into garbage stack words, and
pinning the AI reads to 26 would break WC-family comps (field 23 -> 3 failed
adds -> assert dialogs at national_teams.cpp:1864). This way AI squads stay
byte-for-byte stock (26 full/U21, 24 B-types, 23 WC) and only the HUMAN
manager may add beyond 26, up to NEW_MAX.

Bounds: the underlying squad storage is the 50-slot club player array (both
the insertion free-slot scan at 0x76d09b and the count function 0x53c590
iterate exactly 0x32 slots), so NEW_MAX must be <= 50. At 50 the validator
blocks further adds exactly when the array is full.

Known accepted side effect: a human at a WC-family comp may register more than
the stock 23 (the AI never does).

Both patch sites are byte-verified identical in stock 3.9.68, both live GSLP
exes and gs_pristine (file offset == VA - 0x400000 in .text for all of them).

Idempotent: already-patched exes are detected and left unchanged.

Usage: gslp_natsquad.py <src.exe> <dst.exe> [new_max (2-50, default 50)]
"""
import sys

BASE = 0x400000
SITES = [
    # (file_off, old_bytes, new_bytes(max), what)
    (0x36dd51, bytes.fromhex("8a0f"),
     lambda m: bytes([0xB1, m]),              # mov cl, imm8
     "validator max read (VA 0x76dd51)"),
    (0x36dd7f, bytes.fromhex("0fbe17"),
     lambda m: bytes([0x6A, m, 0x5A]),        # push imm8 / pop edx
     "squad-full message max (VA 0x76dd7f)"),
]


def main():
    if len(sys.argv) not in (3, 4):
        sys.exit(__doc__)
    src, dst = sys.argv[1], sys.argv[2]
    new_max = int(sys.argv[3]) if len(sys.argv) == 4 else 50
    if not 2 <= new_max <= 50:
        sys.exit(f"new_max {new_max} out of range 2-50 (squad array is 50 slots)")

    data = bytearray(open(src, "rb").read())

    for off, old, newf, what in SITES:
        new = newf(new_max)
        assert len(new) == len(old), what
        cur = bytes(data[off:off + len(old)])
        if cur == new:
            print(f"already patched: {what}")
        elif cur == old:
            data[off:off + len(old)] = new
            print(f"patched {what}: {old.hex()} -> {new.hex()}")
        else:
            sys.exit(f"UNEXPECTED bytes at {off:#x} ({what}): {cur.hex()} "
                     f"(want {old.hex()} or {new.hex()}) - wrong exe?")

    open(dst, "wb").write(data)
    print(f"wrote {dst} (national squad cap for human adds = {new_max})")


if __name__ == "__main__":
    main()
