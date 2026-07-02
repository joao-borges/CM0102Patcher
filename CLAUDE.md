# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

**CM0102Patcher** (aka Nick's Patcher, by nckstwrt) — the community tool for patching *Championship Manager 01/02* (`cm0102.exe`, v3.9.68 base). It changes the game's start year, applies hex patches (competition fixes, gameplay tweaks), renames competitions to real-world names, edits data files (histories, club divisions), changes resolution, and more. Upstream README covers the user-facing feature list.

This checkout is **João's fork** (`origin` = `joao-borges/CM0102Patcher`; Nick's original is the `upstream` remote). It carries small local modifications to support **headless operation under Mono on macOS**, used to build pre-patched game exes/data for the Mac port of the CM0102 Starter Kit (sibling repo `../CM0102-Starter-Kit` — see its CLAUDE.md).

## Fork changes vs upstream

- `Patcher.cs`: every `PatcherForm.updatingForm.SetUpdateText(...)` call is null-guarded, and the `catch` falls back to `Console.Error` when no GUI exists. Reason: Mono's WinForms (Carbon backend) is broken on 64-bit macOS, so any GUI touch crashes headless runs. Behavior under the real Windows GUI is unchanged (`updatingForm` is always non-null there).
- `tools/harness.cs`: small console driver around the year-change/patch pipeline (see below). Not part of upstream.

## Architecture (what matters when editing)

- **`YearChanger.cs`** — the start-year change. Lists of exe offsets receiving `year+k` int16 values (`startYear`, `startYearMinus19` … `startYearPlus9`), plus special-case blocks gated on `year % 4` (Euro on `%4==3`, World Cup/Oceania on `%4==0`) and a large `year < 2001` retro section. `%4==1` years (2001, 2025…) need no specials; `%4==2` (2022, 2026) is NOT handled here — that gap is covered by per-year patch bundles instead. Also contains data-file updaters (`UpdateStaff`, `UpdateHistoryFile`, …) used by the *old* (pre-2001/ODB) method only.
- **`Patcher.cs`** — `patches` dictionary of named `HexPatch` sets (offset + hex string), `ApplyPatch`/`UnApplyPatch`, and the text `.patch` file format loader. `.patch` files are lines of `OFFSET: OLD NEW` plus commands (`APPLYMISCPATCH: "<path in MiscPatches.zip>"`, `TAPANISPACEPATCH`, `EXPANDEXE`, `CHANGECLUBDIVISION`, `RENAMECLUB`, …). Commands past the first six require a `HistoryLoader` over `Data/` beside the target exe.
- **`MiscPatches.zip`** (embedded resource, opened via `MiscFunctions.OpenZip`) — the patch library, including the year-specific bundles `"{year} Patches/All Tested {year} + Saturn Patches.patch"` for 2020–2026 (2026 is a sparse work-in-progress stub; 2025 is mature). These bundles chain `Baseline.patch` (Tapani space-maker + `EXPANDEXE` + ~40 community patches) and also edit **data files** (competition renames via `NamePatcher`, history rewrites) — so applying them requires a **full `Data/` folder** (incl. `eng.lng`) next to the exe.
- **`PatcherForm.cs`** — the GUI; the canonical "new method" (year ≥ 2001) flow is: `ApplyYearChangeToExe` → `datecalcpatch` + `datecalcpatchjumps` + `comphistory_datecalcpatch` → optionally `APPLYMISCPATCH "{year} Patches/All Tested {year}…"`. The harness mirrors exactly this.
- Other tools: `NamePatcher` (club_comp/lng renames), `NoCDPatch`, `ResolutionChanger` (rewrites hardcoded resolution constants — no scaling logic), `FixtureScheduler` (EPL fixture dates at hex offsets), History Editor, RGN converters.

## Build

### Windows (upstream's toolchain)
Visual Studio, .NET Framework 3.5 (`CM0102Patcher.csproj`, WinExe). The lone NuGet ref (LinqBridge) is unused; no restore needed.

### macOS (this fork's workflow)
```sh
brew install mono mono-libgdiplus
export PATH="/opt/homebrew/bin:$PATH"
export DYLD_FALLBACK_LIBRARY_PATH="/opt/homebrew/lib"   # libgdiplus for resgen
xbuild /p:Configuration=Release /verbosity:minimal CM0102Patcher.csproj
# output: bin/Release/CM0102Patcher.exe (GUI won't run on macOS — build target only)
```

### Headless harness (macOS)
```sh
cd bin/Release
csc -nologo -r:CM0102Patcher.exe -r:System.Windows.Forms.dll ../../tools/harness.cs -out:harness.exe
mono harness.exe year <cm0102.exe>                     # read current start year
mono harness.exe changeyear <cm0102.exe> <year> [--yearpatch] [--nocd]
```
- `--yearpatch` applies `"{year} Patches/All Tested {year} + Saturn Patches.patch"` — run it **from a directory containing a full `Data/`** (base game data + any update overlaid), because the bundle edits data files too. A partial `Data/` fails on missing `eng.lng`/`club_comp.dat`.
- `--nocd` bakes the NoCD crack (only for exes launched directly; the Starter Kit's loader applies NoCD at runtime via its ini, so omit it for exes embedded there).
- Start from a **clean 3.9.68 exe** (year reads 2001); never re-patch an already year-shifted exe.

## Gotchas

- **No tests/CI.** Changes are validated by patching an exe and running the game (on this Mac: inside the Starter Kit's Wineskin bottle — and when launching Wine from a Claude Code shell, always `env -u NoDefaultCurrentDirectoryInExePath`, see the Starter Kit CLAUDE.md/memory).
- `bin/` is gitignored — anything valuable (like the harness source) must live outside it (`tools/`).
- xbuild is deprecated but works; Mono's `msbuild` is not shipped by Homebrew's mono.
- `.patch` hex format is `fc /b`-style (`OFFSET: OLDBYTE NEWBYTE` per line); patches can nest via `APPLYMISCPATCH`.
