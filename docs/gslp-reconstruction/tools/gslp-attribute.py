import re, os, sys, zipfile, struct
W = os.path.dirname(os.path.abspath(__file__))
CM26 = "/private/tmp/claude-501/-Users-joaoborges-workspace-CM0102-Starter-Kit/b09f8eb0-85d8-45ff-86bb-56775b3c1587/scratchpad/cm2026"
GSEXE = os.path.join(W, "..", "gslp2023", "GS Leagues Patch (DB Abril-23)", "cm0102.exe")
clean = open(os.path.join(CM26, "clean.exe"), 'rb').read()
gs = open(GSEXE, 'rb').read()
N = len(clean)
# ---- 1. delta map: every offset where GS differs from stock (within stock range) ----
delta = [i for i in range(N) if clean[i] != gs[i]]
attr = {}   # offset -> label
# ---- 2. year-change attribution (HEAD YearChanger lists, value == 2022+k) ----
src = open("/Users/joaoborges/workspace/CM0102Patcher/YearChanger.cs").read()
lists = {'startYear':0,'startYearMinus19':-19,'startYearMinus3':-3,'startYearMinus2':-2,
         'startYearMinus1':-1,'startYearPlus1':1,'startYearPlus2':2,'startYearPlus3':3,'startYearPlus9':9}
for name,k in lists.items():
    m = re.search(name+r'\s*=\s*new List<int>\s*\{(.*?)\};', src, re.S)
    body = re.sub(r'/\*.*?\*/','',m.group(1),flags=re.S)          # strip commented offsets
    for o in [int(x,16) for x in re.findall(r'0x[0-9A-Fa-f]+', body)]:
        if struct.unpack_from('<H', gs, o)[0] == 2022+k:
            attr[o]=attr.get(o,'year'); attr[o+1]=attr.get(o+1,'year')
# specials (fixed writes the year tools do)
for o,val in [(0x18B387,(2022).to_bytes(2,'little')), (0x1AEE52,b'\xb8'), (0x41e9ca,b'\x64'), (0x1F9DAD,b'\x30\xf4')]:
    if gs[o:o+len(val)]==val:
        for i in range(o,o+len(val)): attr.setdefault(i,'year-special')
# ---- 3. named hex patches from HEAD Patcher.cs (datecalc etc.) ----
psrc = open("/Users/joaoborges/workspace/CM0102Patcher/Patcher.cs").read()
for pname, pbody in re.findall(r'\{\s*"(\w+)",\s*new List<HexPatch>\s*\{(.*?)\}\s*\}', psrc, re.S):
    hits=tot=0; spans=[]
    for off,hexs in re.findall(r'new HexPatch\((\d+),\s*"([0-9a-fA-F]+)"', pbody):
        off=int(off); b=bytes.fromhex(hexs); tot+=len(b)
        if off+len(b)<=len(gs) and gs[off:off+len(b)]==b: hits+=len(b); spans.append((off,len(b)))
    if tot and hits/tot>=0.9:
        for off,l in spans:
            for i in range(off,off+l): attr.setdefault(i,f'builtin:{pname}')
# ---- 4. every .patch file from the three 2023-era MiscPatches.zip versions ----
def parse_patch(text):
    out=[]
    for line in text.splitlines():
        m=re.match(r'\s*([0-9A-Fa-f]{4,8}):\s*([0-9A-Fa-f]{2})\s+([0-9A-Fa-f]{2})', line)
        if m: out.append((int(m.group(1),16), int(m.group(3),16)))
    return out
matched_patches=[]
for ver in ['v2.25','v2.26','v2.27']:
    z = zipfile.ZipFile(os.path.join(W, ver, 'MiscPatches.zip'))
    for entry in z.namelist():
        if not entry.lower().endswith('.patch'): continue
        try: pb = parse_patch(z.read(entry).decode('latin-1'))
        except Exception: continue
        if len(pb)<3: continue
        hits=sum(1 for o,nv in pb if o<len(gs) and gs[o]==nv)
        if hits/len(pb)>=0.9:
            newly=0
            for o,nv in pb:
                if o<len(gs) and gs[o]==nv and o not in attr:
                    attr[o]=f'{ver}:{entry}'; newly+=1
            if newly: matched_patches.append((f'{ver}:{entry}', len(pb), newly))
# ---- 5. report ----
un = [o for o in delta if o not in attr]
print(f"GS delta bytes (within stock 7.19MB): {len(delta)}")
by={}
for o in delta:
    lab = attr.get(o,'UNATTRIBUTED'); lab = lab.split(':')[0] if ':' in lab and not lab.startswith('v2') else lab
    by[lab]=by.get(lab,0)+1
for lab,c in sorted(by.items(), key=lambda x:-x[1])[:12]: print(f"  {lab:34} {c}")
print(f"\nmatched community patches: {len(set(p for p,_,_ in matched_patches))}")
seen=set()
for p,t,newly in sorted(matched_patches,key=lambda x:-x[2]):
    n=p.split('/')[-1]
    if n in seen: continue
    seen.add(n); print(f"  {newly:5}B  {p}")
    if len(seen)>=18: break
# cluster unattributed
cl=[]; s=None; last=None
for o in un:
    if s is None: s=o
    elif o-last>32: cl.append((s,last)); s=o
    last=o
if s is not None: cl.append((s,last))
print(f"\nUNATTRIBUTED: {len(un)} bytes in {len(cl)} clusters (>=32B gap); largest:")
for a,b in sorted(cl,key=lambda r:-(r[1]-r[0]))[:15]: print(f"  0x{a:06x}-0x{b:06x} ({b-a+1}B)")
open(os.path.join(W,'unattributed.txt'),'w').write('\n'.join(f"0x{a:06x}-0x{b:06x}" for a,b in cl))
print(f"\ncave (beyond stock size): {len(gs)-N} bytes appended")
