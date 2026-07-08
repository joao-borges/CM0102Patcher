#!/usr/bin/env python3
"""Upgrade a (UNCOMPRESSED) CM 01/02 save's embedded club table to the current GSLP
club renames, clearing the exe binder index warnings on load. Saves embed their own
database; the exe binds MLS clubs by LONG NAME at load, so a save created before the
renames mismatches the renamed exe strings. Edits the club.dat block records in place
(long-name field exact match only). usage: gslp_fixsave.py <save.sav> [...]"""
import struct, sys

RENAMES = {b"Tampa Bay Mutiny": b"Los Angeles FC",
           b"Kansas City Wizards": b"Sporting KC",
           b"NY/NJ Metrostars": b"New York Red Bulls",
           b"Miami Fusion FC": b"Philadelphia Union",
           b"Philadelphia": b"Philadelphia Union"}   # v16-era intermediate name

def fix(path):
    d = bytearray(open(path, "rb").read())
    if struct.unpack_from("<i", d, 0)[0] == 4:
        print(path, "is COMPRESSED - not supported"); return
    cnt = struct.unpack_from("<i", d, 8)[0]
    off, club = 12, None
    for _ in range(cnt):
        b = d[off:off+268]
        pos, size = struct.unpack_from("<ii", b, 0)
        if bytes(b[8:b.index(b"\x00", 8)]) == b"club.dat":
            club = (pos, size)
        off += 268
    if not club:
        print(path, "no club.dat block"); return
    pos, size = club
    fixed = []
    for r in range(size // 581):
        base = pos + r*581
        long = bytes(d[base+4:base+55]).split(b"\x00")[0]
        if long in RENAMES:
            d[base+4:base+55] = RENAMES[long].ljust(51, b"\x00")
            fixed.append(long.decode())
    if fixed:
        open(path, "wb").write(d)
    print(path, "fixed:", fixed or "already current")

for p in sys.argv[1:]:
    fix(p)
