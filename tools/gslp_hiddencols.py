#!/usr/bin/env python3
"""Port Nick's HiddenAttributes (addadditionalcolumns) patch onto the GSLP exes.

Background (SESSION-STATE 2026-07-08): the loader-applied HiddenAttributes.patch is
RUNTIME-incompatible with the GSLP exes because CM0102Loader writes at base+RVA, which
puts the cave at VA 0xADC000 (.data BSS the GS engine uses at runtime) while the hook
jumps to VA 0xDE7000. This tool instead BAKES the patch at FILE offsets, the way Nick's
Patcher does on stock:

  file 0x6dc000 -> RVA 0x9e7000 -> VA 0xDE7000  (.rsrc raw 0x6da000 maps to RVA 0x9e5000)

which is exactly the VA every rel32/imm32 in the patch was authored for. On the GSLP
exes this region is GS's ORPHANED resource tail (dead since the degs icon revert;
verified: the only live ref into RVA 0x9e7000+ is our own Philadelphia Union string).

Steps:
  1. assert every code-site old byte matches the .patch old column (== stock);
  2. move the Philadelphia Union string 0x6dc000 -> 0x6dd0a0 (VA 0xDE80A0) and
     repoint the push imm32 at file 0x216148;
  3. zero file 0x6dc000-0x6dd100 and write the cave code+strings (gaps must be 00:
     they are imm32 high bytes / the runtime flag byte area);
  4. set .rsrc characteristics R -> RWX: the cave EXECUTES from .rsrc pages and
     SELF-WRITES its flag byte at VA 0xDE8065 (mov byte [0xDE8065] / mov [0xDE8065],al);
  5. capstone-verify the cave instruction stream and branch targets.

Idempotent: an already-patched exe is detected and left unchanged.

Usage: gslp_hiddencols.py <src.exe> <dst.exe> [HiddenAttributes.patch]
"""
import re
import struct
import sys

BASE = 0x400000
CAVE_FILE = 0x6dc000            # cave start, file offset
CAVE_VA = 0xDE7000              # == BASE + 0x9e7000 (RVA of file 0x6dc000)
CAVE_ZERO_END = 0x6dd100        # zero-fill range end (file)
PHILLY = b'Philadelphia Union\x00'
PHILLY_PUSH = 0x216147          # push imm32 opcode (file; imm32 at 0x216148)
PHILLY_OLD_VA = 0xDE7000
PHILLY_NEW_FILE = 0x6dd0a0
PHILLY_NEW_VA = 0xDE80A0        # CAVE_VA + (PHILLY_NEW_FILE - CAVE_FILE)
FLAG_VA = 0xDE8065              # runtime flag byte the cave writes to
RSRC_RAW = 0x6da000
RSRC_RVA = 0x9e5000

# The .patch old-byte column is stale at two sites ("expand number of items"):
# it says 2e/08 but stock 3.9.68 has 24/06 — confirmed by Patcher.cs's own
# original-bytes (unpatch) table: (0x47ACB7,"24"), (0x47ACC7,"FD06").  The loader
# ignores old bytes, so the shipped .patch was never corrected.
OLD_OVERRIDES = {0x47acb7: 0x24, 0x47acc8: 0x06}

DEFAULT_PATCH = ('/Users/joaoborges/Downloads/CM0102.Starter.Kit.Mac.v1.2.2/'
                 'CM0102StarterKit.app/Contents/Resources/drive_c/'
                 'Program Files/Starter Kit v1.2.2/Game/Patches/Optional/'
                 'HiddenAttributes.patch')
# stock 3.9.68 reference: used to discard instruction-byte false positives in the
# orphan-area reference scan (any offset/value pair present in stock is not a GS pointer)
DEFAULT_STOCK = '/Users/joaoborges/workspace/CM0102-Starter-Kit/external/Files/cm0102.exe'


def parse_patch(path):
    """-> {file_offset: (old, new)}"""
    out = {}
    for line in open(path):
        m = re.match(r'^([0-9A-Fa-f]{8}):\s*([0-9A-Fa-f]{2})\s+([0-9A-Fa-f]{2})\s*$',
                     line.strip())
        if m:
            a = int(m.group(1), 16)
            out[a] = (OLD_OVERRIDES.get(a, int(m.group(2), 16)), int(m.group(3), 16))
    assert len(out) > 400, 'suspiciously small patch file (%d lines)' % len(out)
    return out


def rsrc_header_off(data):
    pe = struct.unpack_from('<I', data, 0x3c)[0]
    nsec = struct.unpack_from('<H', data, pe + 6)[0]
    optsz = struct.unpack_from('<H', data, pe + 20)[0]
    off = pe + 24 + optsz
    for i in range(nsec):
        s = off + i * 40
        if data[s:s + 8].rstrip(b'\0') == b'.rsrc':
            rva, raw = struct.unpack_from('<I', data, s + 12)[0], \
                       struct.unpack_from('<I', data, s + 20)[0]
            assert (rva, raw) == (RSRC_RVA, RSRC_RAW), (hex(rva), hex(raw))
            return s
    raise AssertionError('.rsrc not found')


def scan_orphan_refs(data):
    """imm32 refs into RVA 0x9e7000-0x9e9100 that are unique to this exe's code/data."""
    refs = []
    for i in range(0, RSRC_RAW - 4):
        v = struct.unpack_from('<I', data, i)[0]
        if 0xDE7000 <= v < 0xDE9100:
            refs.append((i, v))
    return refs


def verify_cave(data):
    try:
        from capstone import Cs, CS_ARCH_X86, CS_MODE_32
    except ImportError:
        print('  (capstone not available - skipping disasm verification)')
        return
    md = Cs(CS_ARCH_X86, CS_MODE_32)
    code = bytes(data[CAVE_FILE:CAVE_FILE + 0x12b])
    n, end = 0, CAVE_VA
    for i in md.disasm(code, CAVE_VA):
        n += 1
        end = i.address + i.size
        # every rel32 branch must land in the hook function or inside the cave
        if i.bytes[0] in (0xe8, 0xe9) or (i.bytes[0] == 0x0f and 0x80 <= i.bytes[1] <= 0x8f):
            tgt = int(i.op_str, 16)
            assert (0x401000 <= tgt < 0x967000) or (CAVE_VA <= tgt < 0xDE8100), \
                'branch %s -> %s out of range' % (i.mnemonic, i.op_str)
    assert n > 50 and end >= CAVE_VA + 0x125, 'cave disasm too short (%d insns, end %x)' % (n, end)
    print('  cave disasm OK: %d instructions, %#x..%#x' % (n, CAVE_VA, end))


def main():
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    src, dst = sys.argv[1], sys.argv[2]
    patch = parse_patch(sys.argv[3] if len(sys.argv) > 3 else DEFAULT_PATCH)
    data = bytearray(open(src, 'rb').read())

    code_sites = {a: v for a, v in patch.items() if a < CAVE_FILE}
    cave_sites = {a: v for a, v in patch.items() if a >= CAVE_FILE}
    assert all(CAVE_FILE <= a < CAVE_ZERO_END for a in cave_sites)
    assert all(old == 0 for _, (old, _) in cave_sites.items())
    print('%s: %d code sites, %d cave bytes' % (src, len(code_sites), len(cave_sites)))

    # --- idempotency: already patched? ---
    if all(data[a] == new for a, (_, new) in patch.items()):
        pushed = struct.unpack_from('<I', data, PHILLY_PUSH + 1)[0]
        assert pushed == PHILLY_NEW_VA and \
            data[PHILLY_NEW_FILE:PHILLY_NEW_FILE + len(PHILLY)] == PHILLY
        print('already patched - writing unchanged copy')
        open(dst, 'wb').write(data)
        return

    # --- preconditions ---
    for a, (old, _) in sorted(code_sites.items()):
        assert data[a] == old, 'code site %#x: expected %02x, found %02x' % (a, old, data[a])
    assert data[PHILLY_PUSH] == 0x68 and \
        struct.unpack_from('<I', data, PHILLY_PUSH + 1)[0] == PHILLY_OLD_VA, \
        'Philadelphia push not at expected state'
    assert data[CAVE_FILE:CAVE_FILE + len(PHILLY)] == PHILLY, 'Philadelphia string not at 0x6dc000'
    stock_refs = set(scan_orphan_refs(open(DEFAULT_STOCK, 'rb').read()))
    live = [(i, v) for i, v in scan_orphan_refs(data)
            if (i, v) not in stock_refs
            and not (i == PHILLY_PUSH + 1 and v == PHILLY_OLD_VA)]
    assert not live, 'unexpected refs into orphan area: %s' % [(hex(i), hex(v)) for i, v in live[:5]]
    print('  preconditions OK (code sites == stock, orphan area clean)')

    # --- apply ---
    data[CAVE_FILE:CAVE_ZERO_END] = bytes(CAVE_ZERO_END - CAVE_FILE)
    for a, (_, new) in patch.items():
        data[a] = new
    data[PHILLY_NEW_FILE:PHILLY_NEW_FILE + len(PHILLY)] = PHILLY
    struct.pack_into('<I', data, PHILLY_PUSH + 1, PHILLY_NEW_VA)
    print('  cave written at file %#x (VA %#x); Philadelphia Union -> VA %#x'
          % (CAVE_FILE, CAVE_VA, PHILLY_NEW_VA))

    # --- .rsrc R -> RWX ---
    s = rsrc_header_off(data)
    chars = struct.unpack_from('<I', data, s + 36)[0]
    assert chars in (0x40000040, 0xE0000040), hex(chars)
    struct.pack_into('<I', data, s + 36, 0xE0000040)
    print('  .rsrc characteristics %08x -> e0000040 (RWX: cave executes + self-writes %#x)'
          % (chars, FLAG_VA))

    verify_cave(data)
    open(dst, 'wb').write(data)
    print('  wrote %s' % dst)


if __name__ == '__main__':
    main()
