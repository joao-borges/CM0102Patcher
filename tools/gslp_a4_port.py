#!/usr/bin/env python3
# A4: port GSLP 2023's competition engine onto a May-2026 stack exe.
# usage: gslp_a4_port.py <gs_exe> <clean_3968_exe> <base_stack_exe> <port_manifest.json> <yearchanger_cs> <shift(3|4)> <out_exe>
# Model: GS bytes win across all INCLUDE clusters; Nick year-list sites keep the base exe's
# values; verified GS hand-anchors get GS_value+shift; extension delta (file 0x6b2000-0x6da000)
# copied additively. REVIEW clusters (international %4 cycle zones) are never copied.
import struct, re, json, sys

gs = open(sys.argv[1],'rb').read()
clean = open(sys.argv[2],'rb').read()
base = open(sys.argv[3],'rb').read()
man = json.load(open(sys.argv[4]))
ycsrc = open(sys.argv[5]).read()
SHIFT = int(sys.argv[6])

HAND = {0x2d782,0x2d795,0xc0c91,0xc34a4,0x150e54,0x18b30a,0x1e069c,0x1e06e6,
        0x1fa425,0x202c36,0x23fbcc,0x566c1f,0x566c5b}   # verified GS year anchors (Direction B)
nick = {0x18B387}
for name in ['startYear','startYearMinus19','startYearMinus3','startYearMinus2','startYearMinus1',
             'startYearPlus1','startYearPlus2','startYearPlus3','startYearPlus9']:
    m = re.search(name+r'\s*=\s*new List<int>\s*\{(.*?)\};', ycsrc, re.S)
    nick |= {int(x,16) for x in re.findall(r'0x[0-9A-Fa-f]+', re.sub(r'/\*.*?\*/','',m.group(1),flags=re.S))}

d = bytearray(base)
for c in man["clusters"]:
    if c["bucket"] != "INCLUDE": continue
    a,b = c["start"], c["end"]
    i = a
    while i <= b:
        if i in nick or i+1 in nick:
            o = i if i in nick else i+1
            d[o:o+2] = base[o:o+2]; i = o+2; continue
        if i in HAND:
            struct.pack_into('<H', d, i, struct.unpack_from('<H',gs,i)[0] + SHIFT); i += 2; continue
        d[i] = gs[i]; i += 1
# extension delta, additive
for i in range(0x6b2000, 0x6da000):
    if gs[i] != 0 and d[i] != gs[i]: d[i] = gs[i]

open(sys.argv[7],'wb').write(d)
print("start-year:", struct.unpack_from('<H', d, 0x13386)[0], "->", sys.argv[7])
