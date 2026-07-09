# ⏩ CONTINUE HERE (session handoff, 2026-07-08 end)

## PRODUCT: COMPLETE & USER-CONFIRMED WORKING
The self-contained Starter Kit fork ships the finished product. ONLY install =
`~/Downloads/CM0102.Starter.Kit.Mac.v1.2.2/CM0102StarterKit.app` (+ CM0102.iso archival).
- 3 DBs: Patched 3.9.68 / "25/26 (2026)" / "26/27 (2027)" (+ save/load custom); UI
  simplified (no Nick's Patcher, no Android, no Play submenu); Play button shows the
  ACTIVE DB label; CM Explorer bundled (saves are uncompressed - GS exe default).
- FINAL exes: 2025 = dc3804f7, 2026 = d656dc9a (gslp-archive/a4/cm0102_gslp202{5,6}_final.exe
  = SK external/Files/cm0102_gslp202{5,6}.exe). SK exe 199 MB, installed. If the user's
  running app instance is stale it writes OLD embedded exes each Play - app restart fixes.
- Loader options: TRUE = HideNonPublicBids, UnCap20s, NoWorkPermits, ChangeTo1280x800
  (all site-verified vs loader source) + NoForeignRestrictionsForAll.patch. FALSE+locked =
  everything GS pre-baked (coloured attrs w/ his palette [user-confirmed rendering],
  9 subs, regen fixes, load-all-players, unprotected contracts) - blind re-apply of the
  GS-variant sites corrupts. HiddenAttributes = RUNTIME-INCOMPATIBLE (A/B-confirmed crash
  on save-load; byte-compat is NOT enough) - permanently removed, port = future RE.
- Saves in Game/: renamed "<name> (use 2526|2627|3968).sav" to match products. Save
  compat rules + in-save rename fixer = tools/gslp_fixsave.py (see 💾 section).
- Validated: WC-2026 (hardcoded field) plays out; WC-2030 quals schedule ALL zones
  (long-save safe); dead 2026 quals except Asia = one-season cosmetic (inherent).
  Non-Brazil league memberships = GSLP April-2023 lineups w/ May-2026 squads (only
  Brazil realigned - candidate future work per league).

## TOOLING / REBUILD (all paths durable)
- ~/workspace/gslp-archive/ = canonical inputs (a4 exe chain, gs_pristine, d1_data,
  a3_data25_trim, saves/). ⚠ gslp2023 pristine LOST (tmp decay; only fonts) - d1_data IS
  the GSLP-structure base now. Never use /tmp for anything durable.
- tools/gslp_newdb.sh <updateData> <out> [--zip f] = convert ANY stock-style DB update
  to the GSLP products (VALIDATED: reproduces shipped d1 from May-2026 input, 4 filler
  bytes delta). Review printed match stats (~10.1k good); HANDMATCH in gslp_d1.cs for
  casualties.
- Exe chain: bases in archive; tools/gslp_degs.py = idempotent one-shot (de-GS + SA-cups
  + WCC restore + renames + icon + Philly VA 0xDE7000); gslp_wc32.py = 2025 WC field.
- SK build recipe: reassemble Game.zip if needed; resgen into obj/x86/Release (NOT
  obj/Release!); xbuild Release x86; output bin/x86/Release; cp exe (+SharpZipLib) into
  bundle drive_c. Wine launches from tool shell: env -u NoDefaultCurrentDirectoryInExePath.
- ⚠ .rsrc breaks flat file==RVA mapping (raw 0x6da000 -> RVA 0x9e5000); loader patches
  address base+RVA and IGNORE .patch old-bytes.

## PARKED / NEXT
User: "I have more work for afterwards" (unspecified - ask). Parked: hidden-attr columns
port (RE of GS screens), per-country league realignment, init-banner colour, Cup.cpp:1278
(2026-start), 26 stale-long-name audit candidates (logs/rebuild_v16.log), README refresh
for the fork's new UI. History below is the full investigation log.

# GSLP × May-2026 — Session State (updated 2026-07-07, session 3)

## 🎛 PLAY-BUTTON DB INDICATOR (2026-07-08, SK 66b23a9)
User pain: no indication of the loaded DB -> save<->DB mismatch crashes on Play.
MainMenu.RefreshForm now labels the button "Play <db label>" (refreshes on menu return
and after each play). NB the user must RESTART the Starter Kit app to pick up new
builds - a stale app writes its OLD embedded exes on every Play (observed: 6ab596d7
on disk while dc3804f7/d656dc9a were current).

## 🔁 POST-MILESTONE ROUND (2026-07-08 evening): options corrected, converter shipped
- User report (hidden attrs/coloured/uncap "missing") exposed TWO wrong conclusions:
  (1) the loader IGNORES .patch old-bytes and writes value@base+RVA (CM0102Loader.cpp
  ApplyPatchFile: sscanf 3 parts, WriteByte(addr,part3)) -> the HiddenAttributes cave
  writes .data BSS at VA 0xADC000 (zeros at runtime), NOT GS resource data; all its code
  sites are GS==stock -> byte-compatible... BUT RUNTIME-INCOMPATIBLE (see next entry):
  briefly reinstated, then user A/B-confirmed it crashes save-load/squad screens on the
  GSLP exe (patch present = crash; DB-reload deleted it = same save loads). REVERTED for
  good (SK 66b23a9); hidden-attribute columns = NOT AVAILABLE on GSLP, porting needs RE
  of GS's screen changes. It was also (a/the) culprit of the original player-click crash.
  The original player-click crash re-attributed to the GS-variant overwrites (regen
  fixes / load-all-players / possibly coloured cave) - those stay locked FALSE.
  (2) .rsrc breaks the flat file==RVA mapping (raw 0x6da000 -> RVA 0x9e5000): the
  Philadelphia binder push had been repointed to 0x9e7000 (RVA, = garbage in .data);
  FIXED to VA 0xDE7000; degs accepts its own bad output as an old state. Exes:
  2025 = dc3804f7, 2026 = d656dc9a (staged bundle + SK assets, SK rebuilt+installed).
- Coloured attributes: GS pre-baked Nick's patch with a near-identical palette (cave
  0x9660e8 code identical, palette 1 colour off); USER CONFIRMED colours render ->
  ini stays false (baked). UnCap20s/1280x800/NoWorkPermits enabled earlier still stand.
- ⚠⚠ ARCHIVE HOLE: gslp2023 (pristine GSLP Abril-23 download) was ALREADY GUTTED by
  /tmp decay when archived - only fonts survived. NOT fatal: d1_data carries the whole
  club/comp/league side untouched and re-transplants idempotently. If pristine is ever
  needed, re-download the GSLP from the Brazilian community.
- **tools/gslp_newdb.sh** (commit 308b58c): generalized converter - any stock-style DB
  update -> GSLP-format Data (+--zip). Base = archive d1_data (GSLP_BASE overridable).
  VALIDATED: May-2026 input reproduces shipped d1 (staff/index/officials/histories
  IDENTICAL; club.dat differs by 4 pyramid-filler division bytes; match 10,154/11,546).

## 💾 SAVE↔DB COMPATIBILITY (2026-07-08) — analyzer + fixer built
Saves EMBED their DB tables; the ACTIVE Data Update only decides WHICH EXE launches.
Matching rule: exe start-year must match the save's product (2025-start saves -> "25/26
(2026)" button; 2026-start -> "26/27 (2027)"; wrong year = crash). Pre-rename saves threw
MLS binder index warnings (+ league.cpp complaint) against the renamed exe strings —
FIXED by patching the 4 long names inside each save's club.dat block:
tools/gslp_fixsave.py (uncompressed saves only; block table: hdr int==3, count@8,
268B entries name@+8 pos@+0 size@+4; club recs 581B, long name @+4).
World fingerprint by club count: 10722=GSLP d1, 11546=May-2026, 7012=stock 3.9.68.
Current Game/ saves: 2222+xxxx+xxxx2 (GSLP 2025, fixed, use 25/26), xxxx_2026 (GSLP
2026, fixed, use 26/27), lalala (current-gen 2025 career, Philadelphia Union upgraded),
Joao.sav (May-2026 squads @2001-12-20 = pre-year-patch era, stock engine -> use
Patched 3.9.68 button), test.sav (May-2026 25/26 world) + teste.sav (stock DB @2026) =
NOT playable with shipped exes (need removed cm0102_2025/cm0102_2026 — resurrectable
from git history if user wants). Pre-fix originals archived in gslp-archive/saves/.

## 🧹 DOWNLOADS CLEANUP (2026-07-08): GSTEST bundles DELETED
GSTEST25.app + GSTEST.app removed (user request) after salvaging ALL saves to
~/workspace/gslp-archive/saves/{gstest25,gstest26}/ (gstest25/xxxx.sav there is NEWER
than the copy in the main bundle Game/). Also deleted: cmexplorer zip+dir, patcher
2.25-2.27 tarballs, debug screenshots, cm0102.zip (dup of kept CM0102.iso). The ONLY
remaining install = main bundle ~/Downloads/CM0102.Starter.Kit.Mac.v1.2.2/. Future
in-game tests happen THERE (single-instance constraint now moot).

## 🏁 MILESTONE: WORKING VERSION SHIPPED (2026-07-08) — SK fce46fc / patcher 90c187d
User-confirmed: stock icon OK, both years initialize (Brazil) with proper WC draws,
CM Explorer working. Final round this session:
- ChangeTo1280x800 + UnCap20s + NoWorkPermits ENABLED: verified against the actual
  loader source (github nckstwrt/CM0102Loader, CM0102Loader.cpp): to1280x800 = 193 sites
  + tapanispacemaker (5 sites, PRE-APPLIED in GSLP exes = idempotent) + bkg/mbr files
  auto-extracted by the loader; uncap20s = 6x '9090' at 0x143624/0x1440B5/0x144357/
  0x1443E1/0x144471/0x40807C; noworkpermits = 'eb' at 0x4c75f1. ALL sites == stock bytes
  in both GSLP exes -> loader applies cleanly. (Uncap was NOT GS-baked after all.)
- Philadelphia Union: push imm32 at file 0x216148 (VA 0x616148) repointed 0x9e0dc4 ->
  0x9e7000 (file 0x6dc000 = head of GS's orphaned resource data, free after icon revert);
  string written there; club.dat rec 7231 long name matched. degs handles both, no assert
  on the orphaned area.
- FINAL BUILDS: 2025 = fe818a48 (cm0102_gslp2025_final.exe), 2026 = 6ab596d7
  (cm0102_gslp2026_final.exe), staged in GSTEST25/GSTEST/main bundle + SK assets;
  main bundle Data/club.dat + both GSTEST Data refreshed; gslp_data.zip rebuilt.
- SK repo PRUNED to the working product (era/May-2026 zips+exes, unused images, stale
  accessors all gone; exe 199 MB, 11 resources).
- Remaining niceties (all parked): pt-BR (user says FORGET), init-banner colour,
  Cup.cpp:1278 (2026-start), hidden-attribute columns port, 26 stale-long-name audit
  candidates. NEXT: user has new work planned post-milestone.

## 🖼 ICON/CURSOR REVERT + ⚠ SCRATCHPAD DECAY (2026-07-08)
- User report: game window icon = GSLP's. GS rebuilt .rsrc (2 big icons + modern cursors
  in his +0x28000 tail; stock = small icons/cursors in 0x2000 at file 0x6da000, same
  section VA 0x9e5000, same GROUP_ICON ids 102/104). degs step 7 copies stock's whole
  0x2000 resource block back (also restores STOCK MOUSE CURSORS - consistent de-GS).
- ⚠⚠ /private/tmp scratchpad (old session b09f8eb0) is being CLEANED by macOS: the 2026
  base exe (cm0102_gs_reyear2026_noHAND_heapfix.exe) VANISHED mid-session. Everything
  important ARCHIVED to ~/workspace/gslp-archive/ (a4 exes, gs_pristine_reconstructed.exe,
  d1_data, gslp2023 source, a3_data25_trim, gsre, logs). USE THE ARCHIVE from now on;
  gslp_rebuild_d1.sh paths still point at the old scratchpad - update before next rebuild.
- degs is now IDEMPOTENT (year asserts accept already-reverted exes) - the 2026 exe was
  rebuilt from its own previous output. Current builds: 2025 = 2a813573,
  2026 = 532821c4 (staged GSTEST25/GSTEST + SK assets, SK exe rebuilt+installed).

## 🎛 UI SIMPLIFICATION (2026-07-08, user-requested) — SK commit 2a56e64
Data Updates = Patched 3.9.68 + 25/26 + 26/27 + save/load custom only (Original +
May-2026 DBs and their resources removed; exe 278→199 MB). NickPatcherMenu, PlayMenu,
AndroidMenu forms DELETED — options are baked per-database, Play Game launches directly
(default ini path only). Legacy DB marker files cleaned on switch. CLAUDE.md updated.

## 📦 A5 PACKAGING DONE (session 3) — Starter Kit built with both GSLP products
- Starter Kit repo commits 5211197/7109a4f: two new built-in DBs "25/26 (2026)"
  (gslp_2526_database, cm0102_gslp2025.exe, Year 2025) and "26/27 (2027)"
  (gslp_2627_database, cm0102_gslp2026.exe, Year 2026), SHARED data/gslp_data.zip
  (d1 v16, 21 MB — SetupDatabase now writes the marker file since a shared zip can't).
- ConfigLines bake+lock: speed x4, coloured attrs, no unprotected contracts, hide bids,
  regen fixes, load all players, UnCap20s, no work permits, 1280x800 ("1200x800" label is
  an SK typo), currency 1.00 + Tapani-regen pinned false. 9 subs / hidden attrs / foreign
  limits = patch FILES: SetupDatabase copies them to Game/Patches (removes on switch-away),
  PatchFileDirectory forced to "Patches" for the Standard path (normalized back to "." for
  non-GSLP DBs). ⚠ UNTESTED: whether every loader runtime patch lands cleanly on the GS
  exe (GSTEST ran all-off + speed 4). First packaged-build session will tell.
- CM Explorer 1.2 embedded (external/cmexplorer.zip), extracted on first use to
  Game/CMExplorer, launched with Game/ as cwd; one-time note about uncompressed saves
  (no loader ini option exists; in-game "Compress Save Game Files" must be unticked —
  baking the default = game.cfg/exe RE, parked). NB CM Explorer is officially a CM 00/01
  tool — treat as experimental on 01/02 saves.
- Built with the Mono recipe (obj/x86/Release intermediates! — pre-generate resources
  THERE, not obj/Release), 278 MB exe, installed into the v1.2.2 dev bundle + SharpZipLib
  beside it. NOT launch-tested yet (user mid-holiday-test; Wineskin single-instance).
- CLUB RENAMES DONE end-to-end (commit 68b9c13): stock MLS binder = long-name strcmp table
  0x9e0ccc-0x9e0dd4 (NOT GS code); renamed in place (capacity = gap to next string):
  Tampa Bay Mutiny→Los Angeles FC, Kansas City Wizards→Sporting KC, NY/NJ Metrostars→
  New York Red Bulls, Miami Fusion FC→Philadelphia ("Philadelphia Union" 19B > 16B slot;
  upgrade would need repointing the push at 0x616148 to a free ≥19B pad — none found near
  the table). d1 v16 staleLong renames the club.dat side. Exes: 2025=939023f3,
  2026=071fdae5. NEW GAME required (DB change). 26 more stale-long-name audit candidates
  in rebuild log (logs/rebuild_v16.log).
- ⚠ GSTEST bundles NOT restaged (user mid-holiday-test with 96d4d109/1a7945eb = WCC fix,
  no renames). After the test: restage exes+Data from a4/ + d1_data, or rebuild via
  gslp_rebuild_d1.sh (its step 6 stages GSTEST — do not run mid-test).
- pt-BR hunt: d1 por.lng is byte-identical to stock (pt-PT) — the pt-BR fragments must
  come from language.ldb (May-2026, Brazilian community) or DB record names. BLOCKED on
  user in-game examples (de-GS inventory).

## 🔧 PLAYER-CLICK CRASH IN PACKAGED BUILD — FIXED (2026-07-08)
User repro: packaged Starter Kit, 25/26 DB, manage Sao Paulo, click any player -> crash.
Cause: the A5 "baked defaults" re-applied Nick patches on the GS exe. Verified against
Patcher.cs tables: GS PRE-BAKED colouredattributes (hooks = Nick's bytes, cave 0x5660e8 =
GS variant), disableunprotectedcontracts, 9-subs (IncreaseToNineSubs.patch bytes already
in exe at 0x8f32d), regenfixes + forceloadallplayers (GS variants). HiddenAttributes.patch
(= addadditionalcolumns) writes its cave at file 0x6dc000-0x6dd08a which in the GS exe is
LIVE extended data (stock EOF is 0x6dc000) -> loader corrupts it, player screen reads it,
crash. FIX (SK commit 10d59ba): GSLP ConfigLines now era-DB style (locked, written false);
kept true/applied: HideNonPublicBids (site==stock, verified) + NoForeignRestrictionsForAll
.patch (all old-bytes match). UnCap20s/NoWorkPermits/ChangeTo1280x800 = loader-INTERNAL
tables (no source in repo) -> left off, unverifiable; user checks in-game whether uncap/
permits behaviour is already GS-baked; if missing, RE the loader binary tables next.
Hidden-attribute COLUMNS not available on GSLP (cave collision); porting = find/reserve
free space in GSLP exes (e.g. appended section) + rebase the cave code = future work.
Live bundle fixed in place (patches removed, both inis corrected) - retest = click player.

## ✅ HOLIDAY TEST PASSED (2026-07-07): 2025 product is LONG-SAVE SAFE
User holidayed as Brazil NT to Sept 2026: WC-2026 played out (deeper-validation item
CLOSED) and WC-2030 qualifier draw dates appear for ALL zones in 2026 — the never-run
2026 qual comps roll over cleanly. The dead-2026-quals-except-Asia issue is confirmed a
one-season cosmetic blemish. Bundles RESTAGED after the test with the rename builds:
GSTEST25 = 939023f3, GSTEST = 071fdae5, both Data = d1 v16 (new game needed for renames).
REMAINING: packaged Starter Kit launch test (v1.2.2 dev bundle), de-GS inventory +
pt-BR examples with user in-game, optional Philadelphia Union repoint, parked
init-banner colour + Cup.cpp:1278 (2026-start).

## 🟠 KNOWN LIMITATION (2025 product, confirmed cosmetic-only — see above): WC-2026 qualifiers
User report (2026-07-07, in-game GSTEST25): WC-2026 qualifiers show no estimated draw
dates for all zones EXCEPT Asia. Diagnosis: NOT an SA-fix regression (reverted year-table
ranges reference only Americas club comps; zone globals 0x9cf76c-0x9cf780 untouched).
It is the inherent mid-cycle limitation: draws for UEFA (Dec 2024), CONMEBOL (league
since 2023), CONCACAF/CAF/OFC are in the PAST at a July-2025 start and the engine cannot
retro-generate them (the nullg1-round junk-draw evidence; stock 01/02 solved this by
shipping half-played WC-2002 quals IN THE DB — our d1 has no such seed state). Asia works
because its remaining rounds draw in autumn 2025 (future). Finals unaffected: field is
hardcoded (wc32), 27/12/25 draw validated.
GS cycle triggers use ABSOLUTE years ((Y-2003)%4 in cave2 0x966c8e, odd-year+2015 special
at 0x966c43 = Copa America/Centenario; callers = 7× VA 0x430fe9-family + 0x5e0697), so
WC-2030 (draws 2026-27, all future) is EXPECTED to schedule normally — unknown risk:
whether the never-run 2026 qual comps roll over cleanly to the 2030 cycle.
**PENDING USER HOLIDAY TEST (throwaway save, holiday continuously):**
1. June-July 2026: WC-2026 finals play out with hardcoded field (also closes the
   "deeper validation optional" item).
2. 2027: WC-2030 qual draw dates appear for ALL zones (not just Asia) → 2025 product
   safe for long saves; if still dateless → comps stuck, hunt the edition-rollover logic.
3. Bonus: Euro 2028 schedules (same %4 family).
Option if user wants 2026-cycle quals alive: DB-side seeding of pre-drawn groups
(stock-01/02 style) — exploratory, significant d1 data engineering. Parked.

## 🟡 AWAITING USER TEST: SA-cups crash FIXED (World Club Cup construction restored)
The 0x4C41AE world-gen crash is diagnosed and fixed; both bundles restaged with
SA-reverted exes (Libertadores/Sudamericana stock engines + Intercontinental Cup back).
USER TEST (new game required — comp objects live in the save): world-gen completes →
Copa Libertadores gets Feb fixtures, Sudamericana 2nd semester, Recopa still works,
Brazil domestic unaffected; bonus: Intercontinental Cup final (Liber winner vs European
champion) exists again.
- STAGED: GSTEST25 exe = 96d4d109... (nullg1+wc32+degs+SA, year 2025),
  GSTEST(2026) exe = 1a7945eb... (heapfix+degs+SA, year 2026).

### Root cause (session-2 assumption was WRONG)
[0x9cf6e4] is NOT a Mercosur global — the binder (strcmp chain, cases at 0x610d9a reached
by `je` from 0x60fd41/0x60fd57) binds it to **"World Club Cup" / "Intercontinental Cup"**.
The crashing reverted stock SA code (0x4c41a0-0x4c41eb) assigns the Libertadores winner
into the Intercontinental-Cup pairing ([obj+0xa7] slots, continent-checked vs [0x9cfa1c])
and vcalls [vtbl+0x5c] to schedule it. GS's intl-comp factory (fn ~0x82ff00-0x8312e0)
KILLS that comp: `je→jmp` at VA 0x8311e7 skips the construction block, so
compTable[[0x9cf6e4]] (table ptr at [0xadadfc]) stays NULL → esi==0 → fault at +0xA7.
Stock SA region references NO other killed comp globals (only 0x9cf6e4; scan confirmed).

### Factory map (VA, stock → GS) — decoded this session
- [0x9cf7bc] "FIFA Club World Championship": ctor 0x929140 → GS redirects ctor to
  0x5dc0e0 (his modern CWC engine). LIVE in GS — left untouched.
- [0x9cf6e4] "World Club Cup"/"Intercontinental Cup": ctor 0x92b4b0, obj size 0xb2 —
  GS skips construction (0x8311e7 je→jmp) + repoints the (dead) ctor call to 0x632080.
  **RESTORED to stock** (see fix).
- [0x9cf95c] "Inter-American Cup": stock ctor 0x632080 — GS skips inline (0x831253
  je→jmp) and diverts 0x831295→cave 0x966c19 (stores NULL, pushes his own date args
  29/10/2016, jmp back 0x8312a2). Left GS (stock SA code never reads 0x9cf95c).
- [0x9cf964] block repurposed by GS to construct [0x9cf788] with ctor 0x929140. Left GS.
- [0x9cf79c] = "FIFA World Cup", ctor 0x92bf50 — live in GS, shares the 0x92bxxx region;
  do NOT blanket-revert that region.
- Registration: generic pass at VA 0x838781 (GS-untouched) walks object lists
  0xb63d6c..0xb64744 (+0x48 stride) and fills compTable[recIdx] for every non-NULL obj —
  so restoring construction alone is sufficient; NULL slots are skipped.

### The fix (gslp_degs.py apply_sa_stock, 7 new ranges + 2 imm16 year fixes)
- file 0x4311e7: GS eb → stock 74 (re-enable construction block)
- file 0x431217-0x43121b: ctor rel32 → stock 0x92b4b0
- file 0x52b51a/0x52b528: 2 GS single-byte ctor field tweaks → stock
- file 0x52b82f-0x52b832, 0x52b84a-0x52b84d: WCC fixture date bytes → stock
- file 0x52b890-0x52badf: GS-rewritten ctor-helper fn 0x92b8b0 body → stock. Verified
  DEAD in GS: only callers/refs are WCC-internal (0x92b53e ctor, 0x92be50, 0x92bbd0/07);
  vtable 0x971250 only installed by the dead ctor. WCC vtable methods 0x92b680/0x92b770/
  0x92bda0/0x92c1b0 are otherwise byte-identical to stock. Shared vcall target 0x5223a0
  (vtbl+0x5c) has GS bounds/null-guard edits — LEFT GS (defensive superset, live for all
  comps).
- YEAR_FIX16_OFFS = [0x52b974, 0x52baa0]: `cmp word [rec+0x40], 2001` first-season checks
  inside the restored helper → re-armed to target year (2025/2026), same auto-detect as
  YEAR_FIX_OFF.
- ⚠ cave notes: 0x42d6af-0x42d7a0 is NOT safely orphaned even after the SA revert — live
  GS Recopa code at 0x6320b0 jumps to 0x42d777 and jump-table dwords at 0x5175xx point at
  0x42d6c0/0x42d715/0x42d74d/0x42d779. Never allocate there. Also: no int3/nop pad ≥30B
  exists anywhere in .text that is identical across stock+both re-year exes (scanned).
- If user test still crashes: re-run the same diagnosis — fault offset picks the field,
  scan stock SA region for the comp global, decode its binder name via the strcmp chain,
  check its factory block for je→jmp kills. All tooling patterns in this file.

## Working-state summary for a fresh session (2026-07-07 end)
- 25/26 product (GSTEST25): WC-2026 hardcoded field VALIDATED in-game; cosmetics DONE:
  stock art/splash/colour.dat/backgrounds (d1 pipeline), stock window title, stock
  scoreboard layout, squad-number dot, alphabetical (SHORT-name) club lists, club glitch
  fixes (Fluminense/América-RN/São Caetano pins + matcher upgrades, match rate 10,153).
  pt-PT confirmed OK. PENDING: SA-cups in-game validation (fix staged, see top), then
  club renames+binder, init-banner colour (minor, parked), "de-GS inventory" with user
  in-game.
- 2026 product (GSTEST): same Data + degs exe with the fixed SA revert (1a7945eb...).
- A5 packaging requirements recorded below (Nick's options baked via ConfigLines,
  CM Explorer 1.2 bundling + uncompressed saves).

## ✅✅ OPTION 3 VALIDATED IN-GAME (2026-07-07): user played GSTEST25 (wc32 exe + d1 v14):
no cpp asserts, no index warnings, 27/12/25 WC draw = exactly the 32 teams/groups below.
**25/26 core is DONE.** Remaining: (a) optional deeper validation — sim to June-July 2026,
WC actually plays out; (b) COSMETICS pass (inventory in "Known cosmetics" + de-GS section);
(c) A5 packaging of both final products into the Starter Kit.

## 🎨 COSMETICS PASS (2026-07-07, session 2) — user-approved scope + findings

User wants ALL of: (1) alphabetical club lists, (2) de-GS art/title (splash + main-menu
banner/background + stock window title), (3) modern club renames, (4) Cup.cpp:1278 (2026-start
only — did NOT fire at 2025-start), (5) match-screen score boxes back to stock right-aligned
layout (GS centred them — see ~/Downloads/game.png vs scoreright.jpg), (6) pt-BR translation
fragments → pt-PT everywhere (stock had only pt-PT; source of pt-BR strings not yet located).

### Round 3: init-screen title bar was yellow/green, stock = white-on-red. FIXED (staged):
- The bar is a STOCK widget; GS changed ONE byte in its setup (VA 0x5d8c8c:
  `mov byte [esi+0x49], 6→7` = bg colour red→yellow; green text = contrast over yellow).
  Reverted to 6 = gslp_wc32.py step 4. Nearby diffs 0x5d76ce/0x5d85e0 (call 0x43f7f0→0x43f717)
  are GS's CURRENCY hook (trampoline in stock nop-pad → cave 0x966a00; bisect: do NOT cut).
- GS colour.dat ≈ May-2026 colour.dat (near-identical modern palette) — both non-stock;
  stock palette already staged in round 2.
- GS root .fnt files = stock (SAME); his Fonts/*.ttf extras are irrelevant to banner.
- GS exe tail (+0x28000 vs stock) is data tables, NOT an embedded banner image; no cave
  refs to the title string — banner is drawn, not blitted.

### Round 7: LIBERTADORES FIX — stock SA cup engines restored (user-approved) — STAGED BOTH
- Bug: GS's rewritten Libertadores/Sudamericana never schedules at re-yeared starts.
  User accepts STOCK behaviour (Liber 1st semester, Mercosur-engine 2nd semester).
- GS rewrote conmebol_liber/merc/seeding.cpp engine BODIES IN PLACE (file 0xc0c34-0xc694f,
  52 clusters) + Americas comp-year-table logic (file 0x4317xx-0x431b2x) + a code block in
  former padding at 0x42d6af-0x42d7a0 reached via cave2 (0x831711→0x966ebb→0x42d6af/700).
  Call graph = stock-identical (bodies swapped only). His repurposed INTER-AMERICAN CUP slot
  (0x632480 ctor, pairs Liber+Sudamericana records) = his Recopa; left intact. Also left:
  ger_lge_cup/intertoto/ire_* repurposed engines (unknown GS features, not SA-scheduling).
- **gslp_degs.py step 5 `apply_sa_stock`**: copies 60 byte-ranges from the repo stock exe
  (external/Files/cm0102.exe), skips re-yeared year singles (e9/d1), re-applies target year
  at the one in-range year imm (push 2001 @0x4318ad), year auto-detected from imm16
  @0x431608 (2025/2026 ✓ both). Binder safe: GS renamed lookup strings IN PLACE
  ("Copa Libertadores" @0x9dbae8, "Copa Sudamericana" @0x9db7dc = old Mercosur slot) so
  stock globals ([0x9cf63c] Liber, [0x9cf6e4] Merc) bind his DB records.
- ⚠⚠ CAVE LIST CORRECTION: 0x42d739/0x42d7a0 "verified-dead" caves are WRONG — GS code
  LIVES at 0x42d6af-0x42d7a0 (now orphaned by this revert, but never allocate there
  on non-SA-reverted builds). Safe caves actually used so far: 0x401b81 (heapfix),
  0x411c9f (nullguard). WC32 uses immediates, no caves.
- STAGED: GSTEST25 = 970f97af..., GSTEST(2026) = 45b52e50... TEST (new game not needed for
  exe-only change… but Libertadores state is in the save's comp objects → NEW GAME to see
  the fix): world-gen → check Copa Libertadores gets fixtures Feb 2026, Sudamericana 2nd
  半 semester, Recopa still works, and Brazil domestic unaffected.

### Round 6: SHORT-NAME sort + squad-number dot + de-GS split into shared tool — STAGED BOTH
- Long-name sort LOOKED unsorted in-game (list screens display SHORT names; the pre-season
  league table = record order). gslp_sortclubs.cs now sorts by SHORT name (deaccented,
  long-name tiebreak). Verified: Série A = Athletico Paranaense..Vasco alphabetical.
- "Missing dot" (user shot no_dot.png): GS edited the player-header format strings at file
  0x6856b3/0x6856eb: stock '<%d - squad number>. <%s - player>' — he hid the dot inside the
  token comment. Reverted.
- **NEW tools/gslp_degs.py** — ALL de-GS cosmetic patches (title string, title-bar colour
  byte, 19-cluster scoreboard revert + MLS checks, squad-number dot) with per-site asserts;
  applies cleanly to ANY GS re-year variant (23 patches OK on both 2025-nullg1-wc32 and
  2026-heapfix). gslp_wc32.py = WC-field only now; chain: nullg1 -> wc32 -> degs.
- STAGED: GSTEST25 exe = nullg1_wc32_degs (c5621183...), GSTEST(2026) exe =
  reyear2026_noHAND_heapfix_degs (e2050925...) — 2026 build now ALSO has scoreboard/title/
  dot fixes. Both Data = d1 v15 + short-name sort. New games only for DB changes; exe
  cosmetics apply to the running 2026 save immediately.

### Round 5 (d1 v15): CLUB SORT + matcher fixes + user club glitches — REBUILT & STAGED BOTH
- User-reported (playing 2026): Fluminense divisionless (São Bento wrongly in Série A),
  Azuriz filler in pyramid, AD São Caetano + América-RN missing from pyramid.
- gslp_d1.cs fixes: (a) HANDMATCH dict ('Fluminense Football Club'→'Fluminense',
  'América FC'→'América Futebol Clube (RN)'); (b) GENERAL: adopt state from ShortName when
  long name stateless (fixed the whole América (XX) class — match rate 9,760→10,153,
  injectivity drops 164→97); (c) U20/senior never cross-contain in containment fallback;
  (d) PIN dict: 'AD São Caetano (SP)'→Série D even with empty squad (engine grey-gens;
  evicted unmatched filler Retrô-PE; div 270 now has exactly 1 empty-squad club — WATCH
  world-gen on next new game).
- **tools/gslp_sortclubs.cs (NEW, pipeline step 4.6)**: physical alphabetical club.dat sort
  (deaccented, empty names last), ID=index re-stamped, remaps Rival1-3, staff.ClubJob,
  prefs Fav/DisClubs×6, staff_history.ClubID, club_comp_history Winners/RunnersUp/Third/Host.
  nat_club untouched. Safe because GS binds clubs BY LONG NAME. Verified: lists alphabetical.
- Verified post-rebuild: Fluminense Série A squad 42; América-RN Série C squad 37;
  São Caetano Série D; Azuriz back in state league; A/B/C/D = 20/20/20/36.
- STAGED: GSTEST (2026, via pipeline; exe untouched) + GSTEST25 (rsync; wc32 exe kept).
  NB fixes apply to NEW games only (running saves embed the old DB).

### Round 4: SCOREBOARD REVERTED (staged); banner parked as minor; hook maps complete.
- Init banner still yellow/green after byte revert — user says MINOR, parked. (Byte 0x5d8c8c
  kept at stock 6; colour must come from elsewhere.)
- **Match-screen scoreboard → stock: DONE** (gslp_wc32.py step 5, 19 clusters). Anchor:
  "HT <%d..>" fmt @0xa16714 ref 0x71ca5b; GS had moved score-box x-coords, cave-hooked two
  element builds (0x71b954/0x71ba44 → cave2 0x966ae3ff), and nopped the 4 MLS special-case
  checks (cmp ecx,[0x9cf590]='American Major League' → cmp ecx,-1). All reverted to stock
  bytes. Nick year site 0x31B3E1 verified NOT in any cluster. NEEDS USER TEST (play a match).
- **Complete GS hook maps built** (this session): cave2 0x966800: 18 entries with stock hook
  sites (incl. 0x966c31 ←7× 0x43xxxx comp-year family; 0x966c8e = %4 phase code with
  `sub 0x7d3` — GS's own year constant, re-year never touches caves). cave1 0x601a00:
  ~55 entries; notable externals: 0x601d40←0x84571f/0x84621d, 0x601ff0←0x689c6e/0x7ebe32/
  0x7ec138, 0x602208←0x8ca890, 0x602700←0x7acfa0 (regen/attr code), 0x602b42←0x7abced,
  0x602de4←0x8426c8/f7, 0x602e38←0x7c6bda, 0x602e90←0x5d58a0/0x6bd831/0x6e1117.
- "7." prefix: NOT a shared fmt string ('%d. ' refs identical), NOT obvious in caves.
  Need user screenshot of the exact screen to anchor. NEXT.

### Round 2 (after user feedback "not original look"): pt-PT confirmed OK, title OK.
- Splash/art: May-2026 RGNs are champman0102.net update splashes — ALSO not original.
  Swapped to TRUE stock 3.9.68 art from repo data/patched_data.zip (pipeline step 5.5 now
  unzips stock art incl. colour.dat into $W/stock_art).
- **COLOUR SCHEME ROOT CAUSE: colour.dat.** May-2026's palette rewrites 26/34 named colours
  (all UI Blues/Purples etc). Reverted to STOCK colour.dat (pipeline no longer copies
  $OURS/colour.dat). GS's colour.dat also ≠ stock; stock now overwrites either way.
- game.mbr decoded: 90×600 16bpp dark sidebar strip (not the photo backgrounds).
  Photo backgrounds = Game/Pictures/*.RGN (stock in GSTEST25, identical to main install) +
  in-game "Background Changes" option. mbr string refs identical stock vs GS (unhooked).
- STAGED: stock art + stock colour.dat in GSTEST25 Data + d1_data; exe unchanged this round.
- If look STILL off after this → GS-drawn UI elements in exe (score boxes etc.) — get
  user screenshots and diff A2 display clusters.

### DONE (staged in GSTEST25, art also added to gslp_rebuild_d1.sh step 5.5):
- **Art mechanism decoded**: GS repainted the splash-family RGNs (logo/si/kio/savechip/eidos,
  all 960048 B = 800×600 RGB565 + 48 B header; PIL renders in scratchpad wc/*.png) and
  DELETED game.mbr/match.mbr → engine falls back to loose Data/*.rgn as menu/match backgrounds
  (his "fundo GSLP.rgn" goal-net photo + GSLP.rgn splash). His exe also has a GS-added
  club→background table (spfc.RGN etc. @file 0x58e034) pointing at an Escudos folder we don't
  ship. FIX (data-only): restore May-2026 RGNs + stock game.mbr/match.mbr, delete his 2 files.
- **Window title**: all ~30 window-creation pushes reference GS's string at VA 0xad9118 (his
  .data cave) — overwrote string content in place → "Championship Manager 01/02" fixes all
  refs at once. Now part of tools/gslp_wc32.py (step 3, asserts old bytes).
- NB if the yellow menu banner still shows after this art restore, it's drawn by his cave
  code — hunt refs into 0xab2000-0xada000 from the menu-draw path next.

### TODO (cosmetics, in rough order):
- Score-box layout revert (middle → right): find in A2 delta clusters near match-screen UI.
- "7." squad-number prefix revert (attribute screens).
- pt-BR strings: first LOCATE source — his exe caves? his .lng files? his events_por.cfg?
  (d1 keeps his eng.lng/por.lng etc + our language.ldb; user plays in Portuguese.)
- Alphabetical club lists: physical re-sort club.dat + full club-ref remap in gslp_d1.
- Modern club renames (LAFC/Sporting KC...): rename data + patch his LONG-NAME binder strings.
- Cup.cpp:1278: 2026-start product only.

## 📦 A5 PACKAGING REQUIREMENTS (user, 2026-07-07)
- **Nick's Patcher NOT needed working** for the two GSLP products. Both must ship with ALL of
  these enabled by default (via Database.ConfigLines force+lock, values applied by
  CM0102Loader from the ini): game speed x4, coloured attributes, resolution 1280x1024(?
  user said "1200" — confirm exact patcher option), regen fixes, remove work permits, hide
  non-public bids, 9 subs, disable unprotected contracts, remove foreign limits, load all
  players, uncap attributes + show hidden.
- **Bundle CM Explorer 1.2** (~/Downloads/cmexplorer1_2.zip; freeware, redistribution allowed;
  single PE32 exe + docs): launchable from a button (RunExternalProcess, like editor/CM Scout)
  to browse/edit saved games. ⚠ requires UNCOMPRESSED saves — ensure save-compression off by
  default in the loader ini if such a line exists.

## ⭐⭐ 2026-07-07 (session 2): OPTION 3 PATCH BUILT + STAGED — user test PASSED (see above)

**Breakthrough: the stock engine already contains a hardcoded-finals mechanism.** The WC pool
builder 0x92e940 (called only from draw fn 0x92e7f0, whose only caller is 0x92f20e) ends with
`cmp word [compObj+0x40], 2002` (imm16 at **0x92eb4a**): if the WC year == 2002 it OVERWRITES
all 32 pool slots with binder-resolved nation globals (the real 2002 field, in group order
A1..H4), then rewrites the comp team list [obj+0x14] itself: 6-byte entries (club_rec_ptr +
status 6), **groups assigned sequentially slot/4**, slot 0 = holders (entry status 2),
slots 12 & 28 = hosts (status 1; stock = Korea/Japan), team count [obj+0x36] = 32, ret.
Junk qualifying state is fully bypassed — no cave, no new code needed.

**Patch (built by `tools/gslp_wc32.py`, 129 bytes, all inside the dormant block):**
- imm16 0x7d2→0x7ea at 0x92eb4a (fires at 2026).
- each of the 32 `a1 imm32` (mov eax,[nation_global]) → `b8 imm32` (mov eax, nation_index),
  indices verified by name against d1 nation.dat (all 32 OK; nation rec = 290 B, name at +4,
  rec[0] = nat-club idx; 0x53b3d0(nation_rec) → club_rec ptr, 581 B stride; pool ref = [club_rec]).
- Slot order = FIXED GROUPS (user can reorder FIELD list in gslp_wc32.py and rebuild):
  A: Argentina(holders), Croatia, Bosnia, Egypt · B: Spain, Norway, Colombia, Iraq
  C: France, Austria, Ecuador, South Africa · D: **USA(host,slot12)**, Portugal, Paraguay, Ivory Coast
  E: Brazil, Holland, Saudi Arabia, Haiti · F: England, Switzerland, Morocco, Australia
  G: Germany, Sweden, South Korea, Canada · H: **Mexico(host,slot28)**, Belgium, Cape Verde, Japan
- Output: `a4/cm0102_gs_reyear2025_noHAND_heapfix_nullg1_wc32.exe` (base = nullg1, unchanged
  elsewhere). NOTE: builder ran from new scratchpad `.../01d18f8b-*/scratchpad/wc/` (nullg1.exe,
  gsdis.py disasm helper, build_wc32.py); committed copy = tools/gslp_wc32.py.

**STAGED in GSTEST25**: cm0102.exe = wc32 exe (md5 c20559a80d24eba4cdf515c816af6ae9) +
Data restaged to d1 v14 (rsync --delete; port-G leftovers removed), log truncated.
**USER TEST**: close all other Wineskin apps → GSTEST25 → new game Brazil → run to 27/12/25
draw → WC field must be exactly the 32 above with those groups; hosts USA/Mexico, holders
Argentina. Then continue to June 2026: WC must schedule and play. If crash: grep
LastRunWine.log for "page fault".
- Stdlib-shadowing gotcha struck again: I named the disasm helper `dis.py` (shadows stdlib
  `dis`, breaks capstone import) — renamed `gsdis.py`. NEVER stdlib names in scratchpad.

## Option 3 background (session 1): decision + RE that led here

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
