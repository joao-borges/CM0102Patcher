# GSLP 2023 Reconstruction — Binary Analysis & Porting Plan

*2026-07-02. Companion to `GSLP-2023-modifications.md` (the scraped feature list).*

GS (Giovani Santana) confirmed: no source code, no change log — everything was trial-and-error in
OllyDbg. This document reconstructs what his patch actually is at the byte level, and lays out how
to combine it with the May 2026 database / 2025 start year ("25/26") stack.

## Methodology

1. Diffed `GSLP 2023/cm0102.exe` against clean stock 3.9.68 (`57,367` differing bytes in-place,
   plus a `163,840`-byte appended section — the "cave").
2. **Attribution pass** (`tools/gslp-attribute.py`): classified every differing byte as
   year-change / built-in named patch (datecalc etc.) / any individual `.patch` from the
   v2.25–v2.27 (2023-era) `MiscPatches.zip` bundles / UNATTRIBUTED.
3. Fingerprinted his year-change tool by checking which known year offsets hold 2022-family values.
4. Measured overlap of the unattributed remainder against our year-2025 + All Tested 2025 exe.

## Findings

### His baseline is TAPANI-flavored, not Nick's All-Tested stack
- Year offsets: 132 sites hold clean 2022-family values, but offsets Nick added in 2020–2021
  (Ballon d'Or, season-ticket, UEFA coefficients) are UNPATCHED → his year tool predates Nick's
  2020 tool: almost certainly **Tapani's year changer** or a pre-patched community exe.
- The `file_id.diz` shipped in his folder is from the Placebo/Myth scene NoCD release → his base
  exe was the classic NoCD'd 3.9.68.
- Matched community patches (17, all Tapani-ecosystem or generic): Tapani_Relegation_Patch (1.1 KB),
  San Marino Euro qualifiers, GiveMoreOptionsInOfferDropDown, RemoveLoanProtection,
  TapaniHighlightPlayersInSearch, CONCACAF Tapani Parts, UpdateGoldenGoals, WalesPatch,
  IncreasePromotionToDivision3To2Places, Oceania OFC Tapani, PolishFACupRemove2ndLegs,
  Squad numbers, BrazilSubsTo9, 9and5Subs, NicksFitnessPatchV3, ShowPrivateBids, +1.
- **He did NOT apply Nick's "All Tested" year bundles** — no 2020/2021/2022 bundle content matched.
- datecalc trio present and byte-identical to HEAD's (needed for his 2022 start).

### His hand-work (the actual GSLP)
- **In-place: 52,571 bytes across ~2,137 clusters.** Content analysis shows x86 code rewrites
  (low printable ratio, push/call patterns) — competition logic edits, host/date/qualifier tables,
  team-count constants. Cluster map: `unattributed-clusters.txt`.
- **Cave: 163,840 bytes appended (Tapani spacemaker header matches exactly), 108,904 bytes
  non-zero in 194 clusters.** Early cave content is regularly strided (~0x100 records) —
  table-like structures (fixtures/competition definitions), consistent with Tapani-style
  data-driven machinery, plus injected code.
- The feature list explains the shape: nearly every "new" competition **repurposes an existing
  one** (Série A/B←Danish league, Sudamericana←CONCACAF CL, CWC←German League Cup, MLS←old
  Brazilian D1, …) — i.e., retargeted tables + redirected code, not novel engines.

### Portability onto our 25/26 stack (year 2025 + All Tested 2025)
- **89% of his in-place hand-work (46.7 KB) does not overlap our exe's changes** → portable as
  direct byte copies (same underlying stock code).
- **5,894 bytes in 293 clusters collide** with our stack (biggest: `0x3ca7a6` 1.3 KB, `0x2323fc`
  885 B, `0x1ca70c` 789 B, `0x1dc702` 729 B, `0x52b890` 591 B …) — mostly competition code both
  parties modified. Each needs manual reconciliation in a disassembler.
- **The cave is the hard part**: our exe's own 2.15 MB expansion occupies the same virtual
  addresses. His 109 KB of cave content would need relocation + rebasing of every absolute
  pointer into it (mechanical for 4-byte pointers, risky for computed addresses).

## Two integration directions

### Direction B — re-year HIS exe to 2025 (recommended first) — days
Keep GS's exe intact (no merging), shift its start year 2022→2025 with our harness logic:
write 2025-family values at the 132 verified year sites (+ the specials), **skip `0x1bb6ab` and
`0x43129a`** (he rewrote those), leave everything else alone. Pair with:
1. his own Abril-23 DB (works immediately — modern format, season 25/26, 2023 squads), then
2. optionally the DB project: transplant May-2026 squads into HIS DB structure.

Cheap, testable, no exe merging. Unknown to verify in-game: his custom competition calendar
(built around a Nov-2022 Qatar World Cup) — where does his WC land starting 2025? (Target: June
2026. His code may schedule a November WC.) Copa América on even years is calendar-anchored and
should stay correct (2026, 2028…).

### Direction A — port his hand-work onto OUR exe — weeks
1. Apply the 46.7 KB non-conflicting bytes onto our 2025 exe (scripted).
2. Reconcile the 293 collision clusters in a disassembler (our All Tested 2025 changes vs his).
3. Relocate his 109 KB cave into free space in our 2.15 MB cave; rebase pointers.
4. Rebuild the DB side: reproduce his division/comp repurposing on the May 2026 DB
   (Série B←Danish comp, Série C/D lists, 20-club memberships, qualifier tables) using the
   patcher's data commands, with his Abril-23 `Data/` vs the original champman April-2023 update
   as the reference diff (original update still needs downloading).
5. Verify competition-by-competition against the feature list.

## Data side (applies to both directions' end-game)
His DB edits can be extracted the same way the exe was: diff his shipped `Data/` against the
original champman April 2023 data update (to obtain: division membership changes, comp renames,
the Danish/Greek/Australian/N.Irish replacements, added cups' fixtures/history). That diff is the
recipe to replay on the May 2026 DB.

## Artifacts
- `tools/gslp-attribute.py` — the attribution pipeline (rerunnable).
- `unattributed-clusters.txt` — offset ranges of GS's in-place hand-work.
- Source binaries: `~/Downloads/GSLP 2023.rar` (extract with `bsdtar -xf`);
  2023-era patcher tarballs `~/Downloads/CM0102Patcher-2.2{5,6,7}.tar.gz`.
