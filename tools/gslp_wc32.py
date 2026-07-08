#!/usr/bin/env python3
"""Option-3 WC-2026 patch: re-arm the stock hardcoded-2002-finals branch for 2026.

Base: cm0102_gs_reyear2025_noHAND_heapfix_nullg1.exe
1. cmp word [edi+0x40], 2002  ->  cmp word [edi+0x40], 2026   (imm16 at 0x92eb4a)
2. each of the 32 'mov eax, [nation_global]' (a1 imm32) in the 2002 block
   -> 'mov eax, NATION_INDEX' (b8 imm32).  Slot order = groups: slot/4 = group,
   slot 0 = holders (status 2), slots 12/28 = hosts (status 1) per stock tail code.
"""
import struct
from capstone import Cs, CS_ARCH_X86, CS_MODE_32

BASE = 0x400000
SRC = 'nullg1.exe'
DST = 'nullg1_wc32.exe'

# group layout (slot order).  name -> d1 nation index (verified vs nation.dat)
FIELD = [
    # Group A
    ('Argentina', 8), ('Croatia', 48), ('Bosnia-Herzegovina', 25), ('Egypt', 59),
    # Group B
    ('Spain', 172), ('Norway', 139), ('Colombia', 44), ('Iraq', 92),
    # Group C
    ('France', 70), ('Austria', 12), ('Ecuador', 58), ('South Africa', 170),
    # Group D  (slot 12 = United States = HOST)
    ('United States', 196), ('Portugal', 150), ('Paraguay', 145), ('Ivory Coast', 96),
    # Group E
    ('Brazil', 27), ('Holland', 84), ('Saudi Arabia', 160), ('Haiti', 83),
    # Group F
    ('England', 61), ('Switzerland', 181), ('Morocco', 126), ('Australia', 11),
    # Group G
    ('Germany', 74), ('Sweden', 180), ('South Korea', 171), ('Canada', 36),
    # Group H  (slot 28 = Mexico = HOST)
    ('Mexico', 122), ('Belgium', 19), ('Cape Verde Islands', 37), ('Japan', 98),
]
assert len(FIELD) == 32

data = bytearray(open(SRC, 'rb').read())

# --- 1. year compare ---
off = 0x92eb46 - BASE
assert data[off:off+6] == bytes.fromhex('66817f40d207'), data[off:off+6].hex()
data[off+4:off+6] = struct.pack('<H', 2026)
print('year cmp 2002 -> 2026 @ 0x92eb46')

# --- 2. find the 32 index loads in block order and rewrite ---
md = Cs(CS_ARCH_X86, CS_MODE_32)
start, end = 0x92eb52, 0x92ef70
sites = []
for i in md.disasm(bytes(data[start-BASE:end-BASE]), start):
    if i.bytes[0] == 0xa1:
        g = struct.unpack('<I', i.bytes[1:5])[0]
        if 0x9cf200 <= g < 0x9cf500:          # nation globals; excludes 0xae23a8 table base
            sites.append((i.address, g))
assert len(sites) == 32, len(sites)

for k, ((va, g), (name, idx)) in enumerate(zip(sites, FIELD)):
    o = va - BASE
    assert data[o] == 0xa1
    data[o] = 0xb8                             # mov eax, imm32
    data[o+1:o+5] = struct.pack('<I', idx)
    grp = 'ABCDEFGH'[k // 4]
    print(f'slot {k:2d} grp {grp} @ {va:#x}: [{g:#x}] -> imm {idx:3d} ({name})')

open(DST, 'wb').write(data)
print('wrote', DST)
