using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CM0102Patcher;

// A3 feasibility probe: Brazilian club inventory of a database, grouped by division.
// usage: mono brclubs.exe <DataDir>
class BrClubs {
    static Encoding L = Encoding.GetEncoding("ISO-8859-1");
    static string S(byte[] b) { int n = Array.IndexOf(b, (byte)0); if (n < 0) n = b.Length; return L.GetString(b, 0, n); }
    static int Main(string[] args) {
        var hl = new HistoryLoader(); hl.Load(System.IO.Path.Combine(args[0], "index.dat"), false);
        var brazil = hl.nation.First(n => S(n.Name) == "Brazil");
        var brClubs = hl.club.Where(c => c.Nation == brazil.ID).ToList();
        Console.WriteLine("Brazilian clubs total: " + brClubs.Count);
        var byDiv = brClubs.GroupBy(c => c.Division).OrderByDescending(g => g.Count());
        foreach (var g in byDiv) {
            string dn = g.Key < 0 ? "(no division)" : (g.Key < hl.club_comp.Count ? S(hl.club_comp[g.Key].Name) : "comp#" + g.Key);
            Console.WriteLine(string.Format("  {0,-46} {1,3} clubs", dn, g.Count()));
        }
        // also show which Brazilian comps exist with club counts referencing them via LastDivision
        Console.WriteLine("\nUnassigned club examples:");
        foreach (var c in brClubs.Where(c => c.Division < 0).Take(12)) Console.WriteLine("  " + S(c.Name));
        return 0;
    }
}
