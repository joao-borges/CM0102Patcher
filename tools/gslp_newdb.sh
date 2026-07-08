#!/bin/bash
# Convert a stock-style CM 01/02 database (champman0102-style update, e.g. May 2026 or
# any future one) into the GSLP league format used by the Starter Kit's 25/26 + 26/27
# products. This is the generalized Direction-D transplant: the GSLP Abril-23 club/comp/
# league structure is kept, the update's PLAYERS/STAFF side is transplanted in by
# name-matching, and the Brazilian pyramid is realigned to the update's lineups.
#
# usage: gslp_newdb.sh <update_Data_dir> <output_Data_dir> [--zip <gslp_data.zip>]
#
# Input requirements (stock 3.9.68 layout): index.dat, staff.dat, staff_history.dat,
#   staff_comp_history.dat, first/second/common_names.dat, officials.dat,
#   player_setup.cfg, club.dat, nation.dat, language.ldb.
# After it finishes, REVIEW THE PRINTED STATS:
#   - "club match" should be in the ~9,500-10,200 range (of ~10.7k GSLP clubs);
#     a big drop means the update renamed clubs in a way the matcher can't bridge -
#     add pairs to HANDMATCH in gslp_d1.cs for user-visible casualties.
#   - "stale-long-name candidates" lists recycled records worth eyeballing.
#   - unmatched GSLP clubs get EMPTY squads (filled by regens in game) - fine for
#     small clubs, check the audit lines for anything prominent.
# To ship: replace CM0102-Starter-Kit/data/gslp_data.zip with the --zip output and
# rebuild the Starter Kit exe (resgen + xbuild x86 recipe), or for a quick in-place
# test rsync the output over the app bundle's Game/Data and re-create the database
# marker file (Data/gslp_2526_database.txt or gslp_2627_database.txt).
set -e

ARCHIVE="${GSLP_ARCHIVE:-$HOME/workspace/gslp-archive}"
P="${GSLP_PATCHER:-$HOME/workspace/CM0102Patcher}"
SK="${GSLP_SK:-$HOME/workspace/CM0102-Starter-Kit}"
# The GSLP structure base. The pristine GSLP Abril-23 download was lost to /tmp decay
# (only fonts survived into the archive); the shipped d1_data IS that structure - the
# transplant never touches the club/comp/league side, and the staff side gets replaced
# by every conversion anyway. Re-running over d1_data is idempotent by construction.
HIS="${GSLP_BASE:-$ARCHIVE/d1_data}"
IN="$1"; OUT="$2"
[ -n "$IN" ] && [ -n "$OUT" ] || { echo "usage: $0 <update_Data_dir> <output_Data_dir> [--zip <path>]"; exit 1; }
ZIP=""
[ "$3" = "--zip" ] && ZIP="$4"

echo "== 0. preflight"
for f in index.dat staff.dat staff_history.dat staff_comp_history.dat first_names.dat \
         second_names.dat common_names.dat officials.dat player_setup.cfg club.dat language.ldb; do
  [ -f "$IN/$f" ] || { echo "MISSING in update: $IN/$f (not a stock-layout Data folder?)"; exit 1; }
done
[ -f "$HIS/index.dat" ] || { echo "MISSING/incomplete GSLP base: $HIS (see gslp-archive)"; exit 1; }
[ -f "$P/bin/Release/CM0102Patcher.exe" ] || { echo "MISSING $P/bin/Release/CM0102Patcher.exe (build the patcher first)"; exit 1; }
[ -f "$SK/data/patched_data.zip" ] || { echo "MISSING $SK/data/patched_data.zip (stock art source)"; exit 1; }

echo "== 1. fresh copy of the GSLP Data"
rm -rf "$OUT"
cp -R "$HIS" "$OUT"

echo "== 2. swap in the update's staff-side files"
for f in staff.dat staff_history.dat staff_comp_history.dat first_names.dat second_names.dat common_names.dat officials.dat player_setup.cfg; do
  cp "$IN/$f" "$OUT/$f"
done

echo "== 3. swap index.dat entries (10)"
python3 - "$OUT/index.dat" "$IN/index.dat" <<'EOF'
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

echo "== 3.5 officials.dat: clamp city refs beyond the GSLP city table"
python3 - "$OUT/officials.dat" "$OUT/city.dat" <<'EOF'
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

echo "== 4. compile + run the transplant (club match, staff remap, BR realignment, renames)"
W="$(mktemp -d)"
mcs "$P/tools/gslp_d1.cs" -r:"$P/bin/Release/CM0102Patcher.exe" -out:"$W/gslp_d1.exe" 2>&1 | grep -v warning || true
MONO_PATH="$P/bin/Release" mono "$W/gslp_d1.exe" "$OUT" "$IN"

echo "== 4.5 alphabetical club sort (short-name order, full ref remap)"
mcs "$P/tools/gslp_sortclubs.cs" -r:"$P/bin/Release/CM0102Patcher.exe" -out:"$W/gslp_sortclubs.exe" 2>&1 | grep -v warning || true
MONO_PATH="$P/bin/Release" mono "$W/gslp_sortclubs.exe" "$OUT"

echo "== 5. language.ldb from the update (modern display names)"
cp "$IN/language.ldb" "$OUT/language.ldb"

echo "== 5.5 stock 3.9.68 art (splash/logo/backgrounds/palette)"
STOCKART="$W/stock_art"; mkdir -p "$STOCKART"
unzip -o -q -j "$SK/data/patched_data.zip" \
  logo.rgn si.rgn kio.rgn savechip.rgn eidos.rgn default_pic.rgn game.mbr match.mbr \
  colour.dat -d "$STOCKART"
cp "$STOCKART/"* "$OUT/"
rm -f "$OUT/GSLP.rgn" "$OUT/fundo GSLP.rgn"
rm -rf "$W"

if [ -n "$ZIP" ]; then
  echo "== 6. zip -> $ZIP (flat, no marker file - SetupDatabase writes it)"
  rm -f "$ZIP"
  (cd "$OUT" && zip -q -r "$ZIP" . -x ".*")
fi
echo "DONE: $OUT ready. Review match stats above; ship via data/gslp_data.zip + SK rebuild."
