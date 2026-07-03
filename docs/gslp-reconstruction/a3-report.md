# A3 report: GS structure applied to May 2026 database

Tool: `tools/gslp_a3.cs` (HistoryLoader load→mutate→Save). Input: May 2026 (25/26) Data copy.
All .dat sizes byte-identical after save; verified via brclubs/complister.

## Applied
- 14 club_comp redefinitions (Copa Libertadores, Copa Sudamericana, Brazilian 4th Division,
  Brazilian Supercup, Argentine Cup, Mexican Cup, UEFA CL/Europa/Conference renames, reputation
  boosts) — absolute target values from the A1 decode.
- Brazilian pyramid via reputation cascade over CURRENT May-2026 memberships (user rule:
  no curation): **A=20, B=20, C=20, D=36 (#270)**; D filled entirely from the B/C cascade
  overflow — the unassigned pool wasn't needed. 16 lowest-rep former C clubs → no division.
- Nation tweaks: AR/BR/CL/UY StateOfDevelopment=2, Uruguay Region=4, Australia→AFC,
  Yugoslavia continent off.
- 47/58 name-keyed slow-drift moves (MLS merge, Mexican Primera A, South Africa, state leagues).

## Deferred to A4-time polish
- 11 missing name-moves (renamed clubs: 6 South African, Inter Miami CF naming drift,
  4 Brazilian state clubs) — fuzzy-match or skip after in-game observation.
- GS's ReserveDivision edits (24), Squad/staff-array side effects (ignored by design),
  player_setup.cfg +162B, history-file touch-ups, eidos/kio branding RGNs.

## Note
The transformed Data is NOT standalone-playable: Série D lives on comp #270 whose FORMAT
(round-robin 36) exists only in GS's engine changes — the A4-ported exe is required. Data
staging regenerable in one command: `mono gslp_a3.exe <May2026DataCopy> a3-replay-moves.tsv`.
