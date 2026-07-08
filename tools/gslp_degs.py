#!/usr/bin/env python3
"""De-GS cosmetic patches for GS-Leagues-Patch-derived exes (any re-year variant).
Restores stock 3.9.68 appearance:
  1. window/app title string  -> "Championship Manager 01/02"
  2. init/menu title-bar background colour byte 7 -> 6 (stock red)
  3. match-screen scoreboard layout + MLS special-case checks -> stock
  4. player-header squad-number dot: "<%d - squad number>. <%s - player>" restored
Each patch asserts the exact GS bytes before writing, so a mis-matched base fails loudly.
usage: gslp_degs.py <src_exe> <dst_exe>
"""
import struct, sys

# (file_offset, gs_bytes_hex, stock_bytes_hex)
PATCHES = [
    # -- 3. scoreboard layout (match-overview builder; anchor "HT <%d..>" fmt, ref 0x71ca5b) --
    (0x3190c9, '066a00516a306a466887', '076a01516a306a4668bc'),
    (0x319157, '076a0c516a306a4668bb0100006a0a6887', '066a0c516a306a4168b70100006a0f688f'),
    (0x319205, '066a00', '076a01'),
    (0x319215, 'f5', 'be'),
    (0x31929b, '076a0c516a306a4668f50100006a0a68c101', '066a0c516a306a4168110300006a0f68e902'),
    (0x31931e, '1103', 'e402'),
    (0x3193ba, '03', '01'),
    (0x3193c7, '54', '4e'),
    (0x31940d, '06', '05'),
    (0x319449, '54', '4e'),
    (0x319d97, '83f9ff909090', '3b0d90f59c00'),
    (0x319de1, 'baffffffff90', '8b1590f59c00'),
    (0x319e07, 'b8ffffffff', 'a190f59c00'),
    (0x319e1b, 'b9ffffffff90', '8b0d90f59c00'),
    (0x31b954, '066a00e98ab1240090909090', '0753516a306a4668bc010000'),
    (0x31b9e0, '076a0c516a306a4668bb0100006a0a6887', '066a0c516a306a4168b70100006a0f688f'),
    (0x31ba44, '066a00526a30e9a7b02400906a0a68f5', '0753526a306a4668160300006a0a68be'),
    (0x31badd, '076a0c506a306a4668f50100006a0a68c101', '066a0c506a306a4168110300006a0f68e902'),
    (0x31bb31, '1103', 'e402'),
    # -- 2. init/menu title-bar bg colour --
    (0x1d8c89, 'c6464907', 'c6464906'),
    # -- 4. squad-number dot in player header format strings --
    (0x6856b3, '203c2573202e20706c617965723e20287b7d3c2573202d20636c75623e7b7d2900',
               '2e203c2573202d20706c617965723e20287b7d3c2573202d20636c75623e7b7d29'),
    (0x6856eb, '20', '2e'),
]

TITLE_OFF = 0x6d9118            # file offset of GS's title string (VA 0xad9118)
TITLE_GS  = b'CM 01/02 GS Leagues Patch - DB Abril 2023\0'
TITLE_ST  = b'Championship Manager 01/02\0'

# ---- 6. modern MLS club renames: the STOCK MLS engine binds clubs by LONG NAME via a
# string table at VA 0x9e0ccc-0x9e0dd4 (one code ref each, strcmp chain ~0x6161xx).
# The DB recycled these dead-franchise records for modern clubs (short names updated,
# long names kept to satisfy the binder). Rename BOTH sides: these exe strings (in place,
# capacity = distance to next string) and the club.dat long names (gslp_d1.cs staleLong).
# (offset, old_bytes, new_bytes) padded to full capacity so leftover chars are cleared.
def _cap(s, n):
    b = s.encode()
    assert len(b) < n, s
    return b.ljust(n, b'\0')

CLUB_RENAME_PATCHES = [
    (0x5e0cf8, _cap('Kansas City Wizards', 20), _cap('Sporting KC', 20)),
    (0x5e0d44, _cap('NY/NJ Metrostars', 20),    _cap('New York Red Bulls', 20)),
    (0x5e0d98, _cap('Tampa Bay Mutiny', 20),    _cap('Los Angeles FC', 20)),
    # 'Philadelphia Union' (19B) exceeds the 16B 'Miami Fusion FC' slot, so the binder's
    # push imm32 (VA 0x616148, the only ref) is repointed to VA 0x9e7000 = file 0x6dc000,
    # the head of GS's orphaned resource data (unreferenced after the step-7 icon revert).
    (0x216148, bytes.fromhex('c40d9e00'), bytes.fromhex('00709e00')),
]
PHILLY_OFF = 0x6dc000
PHILLY = b'Philadelphia Union\0'

# ---- 7. stock window icon + mouse cursors: GS rebuilt the .rsrc section (big modern
# icon/cursors, data in his exe-tail extension). Stock's whole resource block (directory +
# icon/cursor data, 0x2000 bytes at file 0x6da000, same section VA 0x9e5000 and same group
# IDs 102/104) is self-contained, so copying it over the section start restores everything;
# GS's oversized resource data beyond it becomes orphaned (never referenced by the tree).
RSRC_OFF, RSRC_LEN = 0x6da000, 0x2000

# ---- 5. stock South American cups (user-approved: stock Libertadores 1st semester +
# stock Mercosur engine driving his "Copa Sudamericana" record 2nd semester) ----
# GS rewrote the conmebol_liber/merc/seeding engine bodies IN PLACE (file 0xc0c34-0xc694f)
# plus the Americas comp-year-table logic (file 0x4317xx-0x431b2x); his re-yeared code never
# schedules the cups at non-2022 starts ("Libertadores never gets dates or draw").
# Call graph is stock-identical, no Nick year sites inside region A, binder lookups stay
# GS's (they bind his DB names: "Copa Libertadores"/"Copa Sudamericana" -> stock globals).
# Region B excludes the re-yeared year immediates (single e9/d1 bytes); the one year imm
# INSIDE a reverted range (push year @0x4318ad) is re-written with the exe's target year,
# auto-detected from the Nick year site imm16 at file 0x431608.
STOCK_EXE_DEFAULT = '/Users/joaoborges/workspace/CM0102-Starter-Kit/external/Files/cm0102.exe'
SA_STOCK_RANGES = [
    (0x0c0c34, 0x0c0c40), (0x0c0c8e, 0x0c0cad), (0x0c0fbb, 0x0c1191), (0x0c11bc, 0x0c11be),
    (0x0c1207, 0x0c1209), (0x0c1238, 0x0c1263), (0x0c1288, 0x0c1289), (0x0c138c, 0x0c1392),
    (0x0c13da, 0x0c13dc), (0x0c1744, 0x0c1745), (0x0c1873, 0x0c187a), (0x0c19f6, 0x0c19fe),
    (0x0c1a23, 0x0c1a26), (0x0c1a4d, 0x0c1a50), (0x0c1a77, 0x0c1a7a), (0x0c1a9c, 0x0c1aa4),
    (0x0c1ac6, 0x0c1ace), (0x0c1b0d, 0x0c1b63), (0x0c1ba1, 0x0c1cb8), (0x0c1d25, 0x0c1d27),
    (0x0c1ec4, 0x0c1ecc), (0x0c1f38, 0x0c1f39), (0x0c2135, 0x0c2138), (0x0c257d, 0x0c2583),
    (0x0c2652, 0x0c2654), (0x0c2686, 0x0c2688), (0x0c26b0, 0x0c26b2), (0x0c26d9, 0x0c26db),
    (0x0c270b, 0x0c270f), (0x0c2898, 0x0c289c), (0x0c28e6, 0x0c28ea), (0x0c2b1e, 0x0c2b21),
    (0x0c2f9d, 0x0c2fa5), (0x0c325e, 0x0c325f), (0x0c328a, 0x0c3309), (0x0c332e, 0x0c3331),
    (0x0c3410, 0x0c3436), (0x0c346d, 0x0c3676), (0x0c38e2, 0x0c38e3), (0x0c391b, 0x0c3973),
    (0x0c41a0, 0x0c41eb), (0x0c429e, 0x0c42a1), (0x0c585b, 0x0c5865), (0x0c5b1e, 0x0c5b21),
    (0x0c60f5, 0x0c6107), (0x0c613b, 0x0c617b), (0x0c61a9, 0x0c61af), (0x0c6354, 0x0c6356),
    (0x0c6505, 0x0c6510), (0x0c653e, 0x0c6544), (0x0c662c, 0x0c662d), (0x0c689f, 0x0c694f),
    (0x431711, 0x43171f), (0x43172f, 0x431735), (0x431802, 0x431808), (0x43185a, 0x43185b),
    (0x4318ad, 0x4318b4), (0x4319c4, 0x4319d2), (0x4319e4, 0x4319e7), (0x431b25, 0x431b28),
    # -- World Club Cup (Intercontinental Cup) restore. The reverted stock SA code assigns
    # the Libertadores winner into this comp (deref compTable[[0x9cf6e4]] at VA 0x4c41ae);
    # GS never constructs the object (je->jmp at VA 0x8311e7 skips the factory block) so
    # world-gen page-faults on the NULL entry. Restore: construction block in the intl-comp
    # factory (jcc byte + ctor rel32 back to stock 0x92b4b0) + the GS-rewritten ctor-helper
    # cluster 0x92b890-0x92badf (dead code in GS: only WCC-internal callers). The generic
    # registration pass at VA 0x838781 (GS-untouched) then fills compTable. GS's killed
    # Inter-American block (VA 0x831253) and his live FIFA-CWC/Recopa blocks stay GS.
    (0x4311e7, 0x4311e8), (0x431217, 0x43121b),   # factory: construct WCC again, stock ctor
    (0x52b51a, 0x52b51b), (0x52b528, 0x52b529),   # ctor: GS 2-byte field tweaks -> stock
    (0x52b82f, 0x52b832), (0x52b84a, 0x52b84d),   # WCC fixture date bytes -> stock
    (0x52b890, 0x52badf),                          # ctor-helper fn 0x92b8b0 body -> stock
]
YEAR_PROBE_OFF = 0x431608       # imm16 low byte of a Nick-re-yeared site (0x7e9/0x7ea)
YEAR_FIX_OFF   = 0x4318ad       # stock 'push 2001' imm32 inside a reverted range
YEAR_FIX16_OFFS = [0x52b974, 0x52baa0]  # 'cmp word [rec+0x40],2001' first-season checks
                                        # inside the restored WCC helper -> target year

def apply_sa_stock(data, stock):
    yr = struct.unpack_from('<H', data, YEAR_PROBE_OFF)[0]
    assert 2000 < yr < 2100, hex(yr)
    n = 0
    for a, b in SA_STOCK_RANGES:
        if bytes(data[a:b]) != stock[a:b]:
            data[a:b] = stock[a:b]; n += 1
    # accept 2001 (fresh revert) or yr (re-running on an already-reverted exe)
    assert struct.unpack_from('<I', data, YEAR_FIX_OFF)[0] in (2001, yr)
    struct.pack_into('<I', data, YEAR_FIX_OFF, yr)
    for off in YEAR_FIX16_OFFS:
        assert struct.unpack_from('<H', data, off)[0] in (2001, yr), hex(off)
        struct.pack_into('<H', data, off, yr)
    print(f'stock SA cups: {n} ranges reverted, year re-applied = {yr}')

def main(src, dst, sa=True):
    data = bytearray(open(src, 'rb').read())
    stock = open(STOCK_EXE_DEFAULT, 'rb').read()
    applied = skipped = 0
    for off, gsb, stb in PATCHES:
        old, new = bytes.fromhex(gsb), bytes.fromhex(stb)
        cur = data[off:off+len(old)]
        if cur == new:
            skipped += 1; continue                    # already stock
        assert cur == old, f'unexpected bytes @ {hex(off)}: {cur.hex()}'
        data[off:off+len(new)] = new
        applied += 1
    cur = data[TITLE_OFF:TITLE_OFF+len(TITLE_GS)]
    if cur[:len(TITLE_ST)] == TITLE_ST:
        skipped += 1
    else:
        assert cur == TITLE_GS, f'unexpected title bytes: {cur!r}'
        data[TITLE_OFF:TITLE_OFF+len(TITLE_GS)] = TITLE_ST.ljust(len(TITLE_GS), b'\0')
        applied += 1
    for off, old, new in CLUB_RENAME_PATCHES:
        cur = data[off:off+len(old)]
        if cur == new:
            skipped += 1; continue
        assert cur == old, f'unexpected club string @ {hex(off)}: {cur!r}'
        data[off:off+len(new)] = new
        applied += 1
    if bytes(data[RSRC_OFF:RSRC_OFF+RSRC_LEN]) == stock[RSRC_OFF:RSRC_OFF+RSRC_LEN]:
        skipped += 1
    else:
        data[RSRC_OFF:RSRC_OFF+RSRC_LEN] = stock[RSRC_OFF:RSRC_OFF+RSRC_LEN]
        applied += 1
        print('stock icon/cursor resources restored')
    if bytes(data[PHILLY_OFF:PHILLY_OFF+len(PHILLY)]) == PHILLY:
        skipped += 1
    else:
        # no old-bytes assert: the target is orphaned GS resource data (contents vary)
        data[PHILLY_OFF:PHILLY_OFF+len(PHILLY)] = PHILLY
        applied += 1
    if sa:
        apply_sa_stock(data, stock)
    else:
        print('stock SA cups: SKIPPED (--no-sa)')
    open(dst, 'wb').write(data)
    print(f'de-GS: {applied} patches applied, {skipped} already stock -> {dst}')

if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], sa='--no-sa' not in sys.argv[3:])
