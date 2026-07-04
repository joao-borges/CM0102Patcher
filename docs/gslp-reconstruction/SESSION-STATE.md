# GSLP × May-2026 — Session State (2026-07-03)

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

## Remaining roadmap
1. v8 test → iterate 0x69BDB9 until 2026-start transplant plays. Then in-game checklist:
   squads = 2026 players, A/B/C/D counts, Libertadores/Sudamericana, Cup.cpp 1278 triage.
2. 25/26: solve his %4 qualifier scheduler at 2025 (per-cluster triage of his 0x50F–0x530 delta;
   goal = stock June-2026 WC with his domestic Brazil intact) OR accept 2026-start only.
3. Polish: binder name aligns (South_American_Recopa, German_Super_Cup, Brazilian_Nat._4th_Div.,
   South-Minas key), his shipped warnings (COLO_COLO etc.) are baseline — ignore.
4. A5 packaging: new DB zip + exe into Starter Kit (Helper.cs Database entry, resources,
   rebuild exe via Mac pipeline, ship to bottle), docs, push.
