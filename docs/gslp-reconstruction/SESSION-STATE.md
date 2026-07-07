# GSLP × May-2026 — Session State (updated 2026-07-07)

## ⭐ CURRENT WORK (start here): Option 3 — hardcoded WC-2026 finals field for 25/26

**Strategic decision (user, 2026-07-07):** all three exe architectures for a native 25/26 WC
dead-ended (details in the stockwc/bisect/port sections below). Agreed plan:
(3) HARDCODE the WC-2026 finals participants on the proven-stable 2025 build, user picked the
32 countries; → then (1) A5 packaging of the FINAL PRODUCTS; (2) port-manifest rebuild from
A2 connected groups = future work.

### The stable 25/26 base (everything applies to THIS build)
- exe: `scratchpad/a4/cm0102_gs_reyear2025_noHAND_heapfix_nullg1.exe`
  = GS exe re-yeared +3 (132 Nick sites, no hand anchors) + 11-site heapfix (cave 0x401b81)
  + null-group guard (site 0x91df0c → cave 0x411c9f). Boots, world-gens, plays for days.
  Only defect: WC-2026 draw (27/12/25 in-game) picks junk (GS's Euro+CONMEBOL qualifying
  can't retro-generate mid-cycle; nullguard lets the draw run on empty state).
- data: `scratchpad/d1_data` (transplant v14) staged via `tools/gslp_rebuild_d1.sh`.

### User's 32 WC-2026 countries → nation indices (aligned in his+our tables; resolved in d1)
UEFA(13): Portugal 150, Spain 172, France 70, Holland 84, Germany 74, England 61, Belgium 19,
  Norway 139, Sweden 180, Croatia 48, Austria 12, Switzerland 181, Bosnia-Herzegovina 25
CONMEBOL(5): Argentina 8, Brazil 27, Colombia 44, Ecuador 58, Paraguay 145
CONCACAF(4): United States 196, Mexico 122, Canada 36, Haiti 83
CAF(5): Cape Verde Islands 37, Morocco 126, Egypt 59, Ivory Coast 96, South Africa 170
AFC(4): Japan 98, South Korea 171, Iraq 92, Saudi Arabia 160
OFC(1): Australia 11

### RE progress toward the patch (in the nullg1 exe)
- Comp-index globals mapped via startup binder chain at VA ~0x610150 (name string pushed,
  then `mov [0x9cf7xx], edi`): **0x9cf79c = FIFA World Cup**; zones: 0x9cf76c Oceania,
  0x9cf770 CONCACAF, 0x9cf774 Asian, 0x9cf778 South American, 0x9cf77c African,
  0x9cf780 European. compTable ptr = [0xadadfc] (comp obj = [[0xadadfc] + idx*4]).
- **Finals reset/collect fn found at 0x92d330** (`this`=WC finals comp obj in ecx):
  zeroes [esi+0x4c], then for each of the 6 zone comps calls `vtable+8` (their
  collect/close), then `call 0x687970(0)`, then clears team count `[esi+0x3e]=0`,
  frees [esi+0xba], clears [esi+0x45]. Candidate injection point: right after the six
  vtable calls (0x92d3b1) or wherever the qualified teams are APPENDED to the finals list.
- Comp/group object layout (from crash decodes): team count = word [obj+0x3e];
  team list = [obj+0xa7], 6-byte entries (ref + flags, see 0x913df3: entry+5 = status byte);
  second list = [obj+0xb1]; groups array = [obj+0xc] with idx bytes at [obj+0x100/0x101].
- NEXT STEPS:
  1. Find where a zone winner is APPENDED into the finals comp's team list (follow the zone
     vtable+8 implementations, or refs to [0x9cf79c] at 0x920a5a/0x920a8a/0x920aa4 and
     0x91fb2e/0x91fb79 — the 0x920axx cluster looks like "add qualified team(s)").
     Understand the append format (6-byte entry: what ref? nation ID vs nat_club ptr/idx).
  2. Patch: after zone collection (or at the append site), replace harvested set with the
     32 hardcoded nation refs (table ~128B + loop ~40B; free VERIFIED-dead caves in this exe:
     0x42a7d7(28B), 0x42d739(24B), 0x42d7a0(155B), 0x42d86f(236B), 0x42dba1(160B)).
     Dedupe if engine force-adds holders (Argentina) / host.
  3. Stage in GSTEST25 (exe + d1 v14), user tests: world-gen → 27/12/25 draw → field = the 32.
  4. Validate the June-2026 WC actually schedules/plays, then A5 packaging.
- ⚠ tooling gotcha: scratchpad had `bisect.py` which SHADOWED Python's stdlib `bisect`
  (capstone imports it → scripts printed cluster lists and died). Renamed to `gsbisect.py`.
  Never name scratchpad files after stdlib modules.

## FINAL PRODUCT SHAPE (A5, after Option 3 lands)
- Original + Patched 3.9.68 DBs: keep forever. May-2026 25/26 daily: UNCHANGED.
- NEW "GSLP × May-2026" product: 2026-start = reyear2026_noHAND_heapfix + d1 v14 (VALIDATED);
  2025-start (25/26 + June-2026 WC) = nullg1 + WC-field patch + d1 v14 (Option 3, in progress).

# ——— original state doc (2026-07-03) + experiment log below ———

## ⚡ 2026-07-03 BREAKTHROUGH #2 — the 0x69BDB9/0x69F82E crash root cause (heap lie)
GS redirected the five world-gen table redimension reallocs (staff/nonplayer/player/prefs/club,
call sites 0x526003/0x52602b/0x526049/0x526065/0x52715c, stock target = CRT realloc 0x945667)
to his own wrapper at **0x601a38**: `HeapReAlloc(crtHeap[0xde4400], HEAP_REALLOC_IN_PLACE_ONLY,
ptr, size)` — and **on failure it returns the OLD pointer as if it succeeded**. Stock fatal-exits
if realloc moves (no rebase exists; all cross-pointers already fixed up), which is why he forced
in-place. With HIS data the heap layout lets in-place growth succeed; with any other data it can
fail → loader believes it has regen capacity → world-gen writes newgen staff past the block →
heap corruption → garbage NonPlayer pointers in the famous-staff sweep (0x69BDB9 / 0x69F82E,
different garbage per run). Likely explains the whole "engine coupled to his DB" mystery
(incl. earlier bra_reg asserts with A3 data — heap corruption manifests arbitrarily).
**FIX (built + staged, untested): `a4/cm0102_gs_reyear2026_noHAND_heapfix.exe`** — the 11
initial table mallocs (0x528b7d, 0x528d1b, 0x528d8d, 0x528dc4, 0x528df5, 0x529bad, 0x529df3,
0x529e02, 0x529e11, 0x529e20, 0x52cbb1) are detoured through a 13-byte cave at 0x401b81
(`add dword [esp+4], 0x200000; jmp malloc`) so every table is born with +2 MB slack → the
later in-place realloc is a shrink → always succeeds. GS wrapper left untouched.
NEXT TEST = GSTEST (heapfix exe + d1_data v8): Brazil → confirm. If game-data crash clears,
also RETEST the A3 direction (our full May-2026 data under his engine) — it may work now.

## 2026-07-03 (later) — HEAPFIX CONFIRMED IN-GAME; two follow-up bugs found & fixed (v9/v10)
- User verdict on heapfix + v8: boots, world-gen completes, squads good, leagues good,
  playable — the 0x69BDB9-class crash family is DEAD. Remaining GS baseline noise: Cup.cpp 1278.
- **v9 fix — Santos crossed match**: gslp_d1.cs:94 tie-break bug. Our stateless "Santos Futebol
  Clube" keyed identically to his "Santos Futebol Clube" AND "Santos Futebol Clube (AP)";
  pick[0] took the lower index = the (AP) club → real Santos lost squad+div. Fixed by ranking
  candidates exact-state > stateless > cross-state. Injectivity drops 164→158 (6 pairs uncrossed).
- **v10 fix — crash 0x52E175 after a few in-game days**: stock ptr→idx sweep over the OFFICIALS
  table (43-byte records; +4/+8 name ptrs deref TNames.ID@0x33; +22 nation; **+26 city ptr**,
  deref @0). Our officials.dat referenced OUR city table (6,725) but d1 keeps HIS city.dat
  (6,251): 22 refs ≥ 6251 → wild pointers. City tables are append-only aligned (0 name
  mismatches in shared 6,251), so in-range refs are fine; the 22 overflow refs → -1.
  Fix lives in the pipeline (tools/gslp_rebuild_d1.sh step 3.5).
- staff_comp_history Info fields checked: same packed-value shape in both DBs (idx<<16|0xFFFF
  chains), self-consistent, NOT a cross-table ref — no action.
- Pipeline is now a single script: `tools/gslp_rebuild_d1.sh` (rebuild d1_data from scratch,
  officials clamp, run gslp_d1, language.ldb, stage into GSTEST).
- Known cosmetics (deferred): club lists not alphabetical (his club table order — official-update
  appends; game lists in record order; fix = physical re-sort + full club-ref remap, polish);
  "7." squad-number prefix on attribute screens (likely GS exe display baseline);
  **Cup.cpp:1278 assert at world-gen (once, harmless)** — DECODED 2026-07-03: stock cup engine
  asks the fixture scheduler (0x5ac920) for a round date as day-of-year + season-year-offset;
  scheduler rejects day≥366 (cmp 0x16e @0x5ac98e) or offset≥3 → returns null → assert at
  0x51a646 (push 0x4fe), fixture skipped, game continues. Re-year artifact in GS's data-driven
  cup schedule tables (one South-American round's base+offset crosses the season boundary at
  2026; also fired with HIS data at 2026-start, so not transplant-related). Fix options:
  runtime break to identify the cup then nudge its date table entry, or suppress the dialog.

Goal: one product line with GS Leagues Patch competition structures (Brazil A/B/C 20 round-robin,
Série D 36, modern Libertadores/Sudamericana) + **May 2026 players**, playable at **2026-start**
(works, see below) and ultimately **2025-start (25/26, June-2026 WC)**. User's daily
May 2026 (25/26) install stays untouched; all testing happens in the cloned bundle
`~/Downloads/CM0102-GSTEST.app`.

## PROVEN FACTS (hard-won, do not re-derive)

### Exe side
1. **Clean re-year of GS's exe works**: shift ONLY the 132 Nick YearChanger list sites holding
   `2022+k` → `target+k`, skip his 2 rewrites (`0x1bb6ab`, `0x43129a`), plus mod-4 special
   `0x18B387` (formula value; =2026 for both 2025 and 2026 starts). **NO other shifts.**
2. **The old 13-site "hand anchor" set was poison**: 5 of 13 were bytes inside `call`/`loop`
   instructions (0xc34a4, 0x150e54, 0x202c36, 0x23fbcc, 0x1e069c). Shifting them corrupted the
   exe. Every pre-2026-07-03 "year-lock"/"%4-phase at +4" negative result was contaminated.
3. **GS engine at 2026-start: WORKS in-game** (`scratchpad/a4/cm0102_gs_reyear2026_noHAND.exe` +
   his own Abril-23 Data). December 2026 start → season 2027. Known non-fatal: Cup.cpp 1278.
4. **GS engine at 2025-start: still blocked** — crash `0x91DF0E` (his qualifier scheduler,
   %4-phase real). 13-byte scalpel revert near the crash didn't fix; broad reverts of his intl
   zones break his domestic code (crashes `0x2827e3` / `0x6827E3`) because club-continental code
   interleaves. Open problem; candidate: finer per-cluster triage of 0x50F–0x530.
5. **Porting his engine ONTO our exes (variants A/C/E/F)**: region-coherent variant F was the
   best (`a4/cm0102_gsport_2026F.exe`, boots+world-gen far) but ties to his DATA anyway — the
   engine binds his tables to the DB at load. Direction abandoned in favour of D.
6. PE headers are never portable; byte-granular merges split instructions — only whole
   GS-delta regions (gap<0x40, pad 0x10) byte-identical to his exe are sound.
7. Tools: `tools/gslp_a4_port.py` (region port builder), YearChanger lists are ground truth.

### Data side
8. **His engine is coupled to HIS database beyond clubs** — every May-2026-based Data (A3 in all
   variants, renames-only, raw) fails under his engine (bra_reg_centro/north/rj asserts,
   dan_second, league crash 0x4A4DCE) while his Abril-23 Data is clean. Cause is NOT divisions,
   memberships, parking, comp records, or histories.
9. **May-2026 club_comp_history.dat is 95.8% null padding** (958,636 of 969,993 records have
   comp=0,year=0). Strip → 11,357. The game trusts index.dat counts; HistoryLoader.Load reads
   the whole file regardless. Stripped variant lives in `scratchpad/a3_data25_trim`.
10. **Direction D (current): transplant May-2026 players INTO his DB** — `tools/gslp_d1.cs`.
    Output: `scratchpad/d1_data` = his Data + our staff-side files
    (staff.dat, staff_history, staff_comp_history, first/second/common_names, officials,
    player_setup.cfg) + index.dat entries swapped (10 entries; index rec = 67 B:
    name[51] + type i32 + count i32 + offset i32; staff.dat sections type 6/9/10/22).
    Then gslp_d1: club match 9,760/11,546 (abbrev-expansion keys: ec/fc/ac/aa/ad/sd/se/cs/sc,
    filler-word drop, state-suffix "(XX)" enforcement, containment fallback), **injective**
    (164 dupes dropped), staff.ClubJob + staff_history.ClubID + TPreferences FavClubs/DisClubs
    remapped, club+nat_club staff-slot arrays copied (nat_club aligns 462/462), BR pyramid
    realigned by name to May-2026 lineups → 20/20/20/36, empty-squad pyramid clubs swapped out.
11. **Assert knowledge**: `Database.cpp:1583` = staff with JobForClub==8 (manager) must appear in
    its club's 5-slot array (runtime staff stride 0x6e; ClubJob ptr @0x39, JobForClub @0x3D,
    Player @0x61, Prefs @0x65, NonPlayer @0x69). Fixed via injectivity. Officials.dat = name-table
    indices (no inline strings; must travel with names). City/stadium names inline (keep his).
12. HistoryLoader staff.dat Save round-trip is **byte-perfect** (verified).

## CURRENT BLOCKER (Direction D, v8 staged & untested)
Crash at `0x69BDB9` — stock per-nation famous-nonplayer sweep
(`cmp word [NonPlayerPtr+0xA], 3750` i.e. CurrentReputation ≥ 3750) hits a staff record whose
NonPlayer pointer is **different garbage each run** → uninitialized/overrun memory, likely the
world-gen player-pool build. Refuted so far: Save corruption, players/nonPlayers .ID backrefs,
GS tables baking staff indices, hardcoded table sizes, duplicate squad membership.
**v8 fix just staged (untested)**: two empty-squad clubs my fill stage had put into playable
divisions (Oeste-SP in Série A!, AD São Caetano in C) are now swapped for squad-bearing clubs.
→ NEXT USER TEST = v8: relaunch GSTEST → Brazil → confirm. If 0x69BDB9 persists, next candidates:
   (a) pad staff table to his 157,402-record shape (count-dependency test),
   (b) autonomous bisect of staff-side file swaps,
   (c) find what allocates the 79-byte-stride per-staff table at [0xae2d0c] used by the sweep.

## Test bench mechanics
- `~/Downloads/CM0102-GSTEST.app`: bundle ID com.CM0102GSTEST.wineskin; plist launches
  `Game/CM0102Loader.exe` with flags `CM0102LoaderDefault.ini` (Year=0, NoCD=true — loader
  NoCDs at runtime; exes stored WITHOUT NoCD). Debug Mode on → `Contents/Resources/Logs/LastRunWine.log`
  (grep "page fault"). **Old Wineskin is single-instance system-wide: main Starter Kit must be
  fully closed** (else silent no-launch). Kill leftovers: `pkill -9 -f GSTEST`.
- Staging = rsync Data variant into
  `.../drive_c/Program Files/Starter Kit v1.2.2/Game/Data/` (always ensure `language.ldb` present,
  extract from Starter Kit repo `data/patched_data.zip`), cp exe to `Game/cm0102.exe`, truncate log.
- Autonomous driving attempt: `cliclick` OK (Accessibility granted); `screencapture` BLOCKED —
  user must grant Screen Recording to the session host app for me to self-drive test rounds.

## Key artifacts (scratchpad = /private/tmp/claude-501/-Users-joaoborges-workspace-CM0102-Starter-Kit/b09f8eb0-85d8-45ff-86bb-56775b3c1587/scratchpad)
- Exes in `scratchpad/a4/`: `cm0102_gs_reyear2026_noHAND.exe` (WORKS), `cm0102_gs_reyear2025_noHAND.exe`
  (%4 crash), `cm0102_gs_2025_stockintl{,_v2,_v3}.exe` (failed hybrids), `cm0102_gsport_2026F.exe` (port).
- Data: `d1_data` (transplant v8), `a3_data25_trim` (A3 + null-history strip), `a3_src25` (raw
  May-2026 25/26 unzip), `gslp2023/GS Leagues Patch (DB Abril-23)/` (his pristine exe+Data).
- Tools (committed, CM0102Patcher repo): `tools/gslp_a3.cs`, `tools/gslp_a4_port.py`,
  `tools/gslp_d1.cs`, plus bin/Release probes (squadcheck, sq2, checkrefs2, histcheck, dupcheck,
  compdump, missdiag, backref — compiled from /tmp sources, recompile as needed).
- Analysis: `docs/gslp-reconstruction/*` (A1/A2 reports, port-manifest.json), memory file
  `gslp-brazil-format-evaluation.md` (condensed version of all of this).

## ✅ 2026-07-06 — 2026-START VALIDATED BY EXTENDED PLAY
User played Dec 2026 → April: São Paulo state league runs, save/reload works, board screens fine
(earlier transfer-funds crash did NOT reproduce on the v11/v12 build — likely another stale-ref
casualty fixed by the matcher overhaul). v12 adds May-2026 cosmetics: language.ldb (modern
nation display names — "Holland"→"Netherlands" comes from the ldb, both DBs store "Holland"),
colour.dat (updated RGB palette), nat_club names+kit colours (462), club kit colours (2,811).

## ▶ CURRENT FOCUS: 2025-start (user's PRIMARY goal — 25/26 season, June-2026 WC)
The old 0x91DF0E "qualifier scheduler %4" crash was diagnosed PRE-heap-fix and may have been
heap-lie corruption all along. Retest first before believing it:
- Built `a4/cm0102_gs_reyear2025_noHAND_heapfix.exe` (same 11-site alloc detour, cave 0x401b81).
- Staged in NEW parallel bundle `~/Downloads/CM0102-GSTEST25.app`
  (bundle id com.CM0102GSTEST25.wineskin; same d1_data; GSTEST untouched — user's 2026 save
  keeps playing there). NB: old Wineskin single-instance — only ONE of the bundles can run at
  a time (and main Starter Kit must be closed).
- Heapfix retest CONFIRMED 0x91DF0E is real (crash at ~70% league init, read [ebx+0x3e], ebx=0).
- **0x91DF0E DECODED (2026-07-06)**: fault is in a STOCK helper 0x91def0 (no GS delta ±0x400):
  given a comp object, it takes group ptrs [this+0xc][ [this+0x100] ] and [[this+0x101]] and
  finds the team present in both lists ([grp+0xb1] vs [grp2+0xa7], counts at [grp+0x3e]).
  Caller 0x913e42 (wc_europe_league.cpp neighborhood) fetches the OTHER comp via
  compTable[*0x9cf780]. At 2025-start the game is MID-WC-CYCLE (WC June 2026) and world-gen
  retro-generates the ongoing European qualifying — GS's rewritten qualifying never created
  those groups outside his start phase (his 2022 = +4 = 2026 keeps phase; +3 breaks it).
- **Fix strategy = null-guard whack-a-mole**: extend the helper's existing "count<=0 → return 0"
  path to also fire on NULL group ptrs (missing group == empty group; stock has fallbacks).
  Patch v1 (`a4/..._heapfix_nullg1.exe`, staged in GSTEST25): site 0x91df0c (xor edi,edi +
  cmp [ebx+0x3e],di) → jmp cave 0x411c9f {xor edi,edi; test ebx,ebx; je 0x91df65; test eax,eax;
  je 0x91df65; cmp; jmp 0x91df12}. Expect possible further null-group crash sites downstream —
  fix each the same way until world-gen completes, then validate WC-2026 shape in-game.
- ⚠ CAVE SAFETY: nop runs are NOT all dead — 0x40e5b3 (224×0x90) is a LIVE fall-through sled
  (GS nopped stock code out). Only use runs directly preceded by ret/jmp/int3: 0x411c9f (46),
  0x42a7d7 (28), 0x42d739 (24), 0x42dba1 (160, after ret imm). 0x401b81 (15) holds the heapfix
  detour. Heapfix cave/site addresses identical in 2025 and 2026 exes.
- 2025 test round 2 (nullg1): game PLAYS (world-gen ok, days of gameplay) but WC-2026 draw
  picked junk: NO European teams, CONMEBOL only Argentina (= holders auto-slot), other zones
  random (stock simulated). GS's rewritten Euro+CONMEBOL qualifying can't retro-generate
  mid-cycle; nullguard let the draw run on nothing. v3 scalpel insufficient.
- Long-name renames (LAFC/Sporting KC) BROKE GS's binder (TAMPA_BAY_MUTINY /
  KANSAS_CITY_WIZARDS index warnings): his engine resolves clubs BY LONG NAME. Renames
  disabled (v14); proper fix = rename data + patch his binder strings (cosmetics pass).
- **stockwc experiment log (2026-07-06/07):**
  - stockwc (intl zone file 0x50F000–0x530000 → stock, +heapfix): crash 0x6827E3 — which turns
    out to be a STOCK function reading [group+0xb1] with group==NULL: the "domestic breakage"
    of the old broad-revert experiments was ALWAYS this same null-group family. NO interleaving.
  - stockwc2 (+ dispatcher calendar constants VA 0x5ca598–0x5ccd41, 243 B → stock): same crash,
    earlier. Dispatcher constants not the gate.
  - Data ruled out: his nation_comp.dat ≈ ours (both modernized identically vs 2001 stock,
    except slot [11]: his=CONCACAF Gold Cup, ours=Asian Cup Qualifying — explains the eternal
    ASIAN_CUP_QUALIFYING binder warning). nation_comp NOT the cause.
  - **KEY STRUCTURAL FINDING**: GS's exe has NO code section of its own — his added code lives
    in two .text caves (0x601a00–0x603000 next to his heap wrapper; 0x966800–0x967000 at .text
    end) + data tables in .data slack (VA 0xab2000–0xada000, file 0x6b2000–0x6da000).
    173 call/jmp hooks total; ~45 are external (stock code → cave). His cave code contains year
    constants NICK'S LISTS DON'T COVER (e.g. sub edx,2003 = 2022−19 pattern) — the re-year
    never touches GS's own code. At +4 phase parity masks it; at +3 it breaks.
  - Hook 0x83129d = file 0x43129d ≈ Nick site 0x43129a (one of the 2 "GS rewrites" the re-year
    skips): stock pushes season date (20/5/startYear-ish); GS cave pushes (29/10/2016?+flag).
  - **stockwc3 (STAGED in GSTEST25)**: stockwc2 + revert 4 external hooks to stock:
    0x83129d/0x831711/0x833046 (comp-year-table region, file 0x43xxxx) + 0x902cd7 (wc-adjacent),
    140 bytes. Theory: his comp-year hooks answer "does comp X run in year Y" — wrong at 2025
    phase → groups never created → all the null-group crashes + junk WC draw.
  - stockwc4–7 + bisect rounds (2026-07-07/08): hook-cutting is a dead end — many GS hooks are
    DATA-coupled (0x43f717/0x43f7f0 = his currency system; cutting → startup crash 0x43F775).
    Bisect over reverts: clusters 0-10 alone pass game data (but comp_stats 1664 spam);
    clusters 11-22 (hook cuts 0x4c6-0x585) break game data; intl-zone revert breaks game data
    at 0x6827E3 with ANY data (his/pure/A3) even with inbound-edge callers severed (stockwc7).
    CONCLUSION: reverting GS's intl zone from HIS exe is not viable by byte-surgery from the
    GS side — too many unmapped dependencies.
  - **PIVOT (current test): PORT direction resurrected.** Old port-F was DOUBLY corrupt:
    (a) built with the poisoned 13-site HAND anchor set (5 bogus call-displacement shifts),
    (b) inherits GS's heap-lie wrapper (port copies the 0x526003→0x601a38 redirects).
    Its old failures (squad_manager 1200 etc.) are explained; direction never had a fair trial.
    Rebuilt clean: `gs_pristine_reconstructed.exe` (reyear2025_noHAND unshifted −3, mod4→2022;
    132 sites) → /tmp/port_nohand.py (gslp_a4_port.py with HAND=∅) → `a4/cm0102_gsport_2025G.exe`
    → +11-site heapfix → `a4/cm0102_gsport_2025G_heapfix.exe` (STAGED in GSTEST25 + A3 data).
    Port architecture: stock 2025 base exe (no %4 problem, stock intl), GS INCLUDE clusters
    for club comps, REVIEW (intl %4) clusters never copied.
  - Remaining external hooks if needed: 0x8426c8/f7, 0x84571f, 0x84621d, 0x8ca890, 0x40b138,
    0x40bcb4, 0x40d225, 0x40dba3, 0x430fe9-family (7×→0x966c31), 0x43f717, 0x43f7f0, 0x4c61a9,
    0x4ea210/5, 0x539ae9, 0x5523fb, 0x553a4b, 0x553ca9, 0x56fb15, 0x57c698, 0x57d9dc, 0x57f60c,
    0x5856d6, 0x58b36f, 0x5d58a0, 0x5e0697, 0x689c6e, 0x6b5cd7, 0x6bd831, 0x6e1117, 0x71b957,
    0x71ba4a, 0x78a3f2/7, 0x7abced, 0x7acfa0, 0x7c6bda, 0x7ebe32/3d, 0x7ec138, 0x84571f,
    0x8ca890. Keep: 0x526003/2b/49/65+0x52715c (heap wrapper, benign with heapfix).

## 2026-07-07 — stockwc line DEAD-ENDED; pivot to port-F + heapfix
- stockwc2-7 + bisect (186 clusters) established: reverting the intl zone breaks world-gen at
  0x6827E3 DATA-INDEPENDENTLY (his DB, pure May-2026, A3 all crash); severing the 13 inbound
  GS edges into the zone (stockwc7) did NOT cure it — GS's zone coupling is denser than its
  call graph (data tables/vtables/mid-function links). Also: hook-cut reverts 11-22
0. **De-GS cosmetics pass (user explicitly wants this)**: GS's exe carries several cosmetic
   changes the user hates (known so far: "7." squad-number prefix on attribute screens; inventory
   the rest with the user in-game). Approach: use the A2 cluster map of the GS-vs-stock delta,
   classify display-only clusters, revert them to stock bytes while keeping his competition
   engine — same region-coherent revert technique as the re-year work. Do AFTER core stability
   is proven (extended play + season rollover), since every revert needs a retest.
1. v8 test → iterate 0x69BDB9 until 2026-start transplant plays. Then in-game checklist:
   squads = 2026 players, A/B/C/D counts, Libertadores/Sudamericana, Cup.cpp 1278 triage.
2. 25/26: solve his %4 qualifier scheduler at 2025 (per-cluster triage of his 0x50F–0x530 delta;
   goal = stock June-2026 WC with his domestic Brazil intact) OR accept 2026-start only.
3. Polish: binder name aligns (South_American_Recopa, German_Super_Cup, Brazilian_Nat._4th_Div.,
   South-Minas key), his shipped warnings (COLO_COLO etc.) are baseline — ignore.
4. A5 packaging: new DB zip + exe into Starter Kit (Helper.cs Database entry, resources,
   rebuild exe via Mac pipeline, ship to bottle), docs, push.
