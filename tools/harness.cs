using System;
using System.Collections.Generic;
using CM0102Patcher;

// Headless driver around CM0102Patcher's year-change logic, mirroring the
// "NEW VERSION" path in PatcherForm.cs (year >= 2001) so we can iterate on
// the 2026 crash without the WinForms GUI.
class Harness {
    static int Main(string[] args) {
        try {
            if (args.Length < 1) { Usage(); return 2; }
            // PatcherForm.updatingForm stays null headless; all its SetUpdateText call
            // sites in Patcher.cs are null-guarded so the GUI progress hooks no-op.
            var yc = new YearChanger();
            switch (args[0]) {
                case "year": {                       // year <exe>
                    Console.WriteLine(yc.GetCurrentExeYear(args[1]));
                    return 0;
                }
                case "changeyear": {                 // changeyear <exe> <year> [--yearpatch]
                    string exe = args[1];
                    int year = int.Parse(args[2]);
                    bool yearPatch = Array.IndexOf(args, "--yearpatch") >= 0;
                    bool noCd = Array.IndexOf(args, "--nocd") >= 0;
                    int cur = yc.GetCurrentExeYear(exe);
                    Console.WriteLine("Current exe year: " + cur + "  -> target: " + year);

                    yc.ApplyYearChangeToExe(exe, year);
                    var p = new Patcher();
                    p.ApplyPatch(exe, p.patches["datecalcpatch"]);
                    p.ApplyPatch(exe, p.patches["datecalcpatchjumps"]);
                    p.ApplyPatch(exe, p.patches["comphistory_datecalcpatch"]);
                    Console.WriteLine("Applied year change + datecalc patches.");

                    // NoCD only when --nocd given (for standalone direct-launch). For an exe
                    // embedded in the Starter Kit, omit it: the loader applies NoCD at runtime.
                    if (noCd) {
                        NoCDPatch.PatchEXEFile(exe);
                        Console.WriteLine("Applied NoCD crack.");
                    }

                    if (yearPatch) {
                        string patchName = string.Format(
                            "{0} Patches/All Tested {0} + Saturn Patches.patch", year);
                        Console.WriteLine("Applying year-specific patch: " + patchName);
                        p.ApplyPatch(exe, new List<Patcher.HexPatch> {
                            new Patcher.HexPatch("APPLYMISCPATCH", patchName, null) });
                    }
                    Console.WriteLine("Done. New exe year reads: " + yc.GetCurrentExeYear(exe));
                    return 0;
                }
                default:
                    Usage(); return 2;
            }
        } catch (Exception ex) {
            Console.Error.WriteLine("ERROR: " + ex.GetType().Name + ": " + ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static void Usage() {
        Console.Error.WriteLine("usage:");
        Console.Error.WriteLine("  year       <exe>");
        Console.Error.WriteLine("  changeyear <exe> <year> [--yearpatch]");
    }
}
