#!/bin/bash
# Rebuild d1_data (Direction D transplant) from scratch and stage into GSTEST.
set -e
W="/private/tmp/claude-501/-Users-joaoborges-workspace-CM0102-Starter-Kit/b09f8eb0-85d8-45ff-86bb-56775b3c1587/scratchpad"
HIS="$W/gslp2023/GS Leagues Patch (DB Abril-23)/Data"
OURS="$W/a3_data25_trim"
P=/Users/joaoborges/workspace/CM0102Patcher
GAME="$HOME/Downloads/CM0102-GSTEST.app/Contents/Resources/drive_c/Program Files/Starter Kit v1.2.2/Game"

echo "== 1. fresh copy of his Data"
rm -rf "$W/d1_data"
cp -R "$HIS" "$W/d1_data"

echo "== 2. swap in our staff-side files"
for f in staff.dat staff_history.dat staff_comp_history.dat first_names.dat second_names.dat common_names.dat officials.dat player_setup.cfg; do
  cp "$OURS/$f" "$W/d1_data/$f"
done

echo "== 3. swap index.dat entries (10)"
python3 - "$W/d1_data/index.dat" "$OURS/index.dat" <<'EOF'
import struct, sys
dstf, srcf = sys.argv[1], sys.argv[2]
SWAP = {("staff.dat",6),("staff.dat",9),("staff.dat",10),("staff.dat",22),
        ("staff_history.dat",17),("staff_comp_history.dat",18),
        ("first_names.dat",13),("second_names.dat",14),("common_names.dat",15),
        ("officials.dat",7)}
def recs(raw):
    out=[]; off=8
    while off+67<=len(raw):
        name=raw[off:off+51].split(b'\0')[0].decode('latin-1')
        ft,cnt,o=struct.unpack_from("<iii",raw,off+51)
        out.append((off,name,ft,cnt,o)); off+=67
    return out
dst=bytearray(open(dstf,'rb').read()); src=open(srcf,'rb').read()
srcmap={(n.lower(),ft):(cnt,o) for _,n,ft,cnt,o in recs(src)}
n=0
for off,name,ft,cnt,o in recs(bytes(dst)):
    k=(name.lower(),ft)
    if k in SWAP:
        scnt,soff=srcmap[k]
        struct.pack_into("<iii",dst,off+51,ft,scnt,soff); n+=1
open(dstf,'wb').write(dst)
print("index entries swapped:",n); assert n==10
EOF

echo "== 3.5 officials.dat: clamp city refs beyond his city table (crash 0x52E175 fix)"
python3 - "$W/d1_data/officials.dat" "$W/d1_data/city.dat" <<'EOF'
import struct, sys
off = bytearray(open(sys.argv[1],'rb').read())
ncity = len(open(sys.argv[2],'rb').read())//56
n=0
for i in range(len(off)//43):
    c = struct.unpack_from("<i", off, i*43+26)[0]
    if c >= ncity:
        struct.pack_into("<i", off, i*43+26, -1); n+=1
open(sys.argv[1],'wb').write(off)
print("officials city refs clamped to -1:", n, "(city table size", ncity, ")")
EOF

echo "== 4. compile + run gslp_d1"
mcs "$P/tools/gslp_d1.cs" -r:"$P/bin/Release/CM0102Patcher.exe" -out:"$W/gslp_d1.exe" 2>&1 | grep -v warning || true
MONO_PATH="$P/bin/Release" mono "$W/gslp_d1.exe" "$W/d1_data" "$OURS"

echo "== 5. language.ldb"
if [ ! -f "$W/d1_data/language.ldb" ]; then
  unzip -p /Users/joaoborges/workspace/CM0102-Starter-Kit/data/patched_data.zip "*language.ldb" > "$W/d1_data/language.ldb"
fi

echo "== 6. stage into GSTEST"
rsync -a --delete "$W/d1_data/" "$GAME/Data/"
: > "$HOME/Downloads/CM0102-GSTEST.app/Contents/Resources/Logs/LastRunWine.log" || true
echo "staged. exe in place: $(md5 -q "$GAME/cm0102.exe")"
