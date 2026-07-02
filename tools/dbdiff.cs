using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CM0102Patcher;

// GSLP reconstruction (A1): record-level diff of two CM0102 databases.
// usage: mono dbdiff.exe <origDataDir> <modDataDir>
// Loads both via HistoryLoader, compares records by index, reports changed fields.
class DbDiff {
    static Encoding L = Encoding.GetEncoding("ISO-8859-1");
    static string S(byte[] b) {
        if (b == null) return "";
        int n = Array.IndexOf(b, (byte)0); if (n < 0) n = b.Length;
        return L.GetString(b, 0, n);
    }
    static string Val(object v) {
        if (v is byte[]) return "\"" + S((byte[])v) + "\"";
        if (v is int[]) return "[" + string.Join(",", ((int[])v).Select(x => x.ToString()).ToArray()) + "]";
        return v == null ? "null" : v.ToString();
    }
    static bool Eq(object a, object b) {
        if (a is byte[]) return ((byte[])a).SequenceEqual((byte[])b);
        if (a is int[]) return ((int[])a).SequenceEqual((int[])b);
        return Equals(a, b);
    }
    static void Compare(string label, IList o, IList m, Func<object,string> nameOf) {
        Console.WriteLine("\n## " + label + " (" + o.Count + " vs " + m.Count + " records)");
        int changed = 0;
        for (int i = 0; i < Math.Min(o.Count, m.Count); i++) {
            var fields = o[i].GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            var diffs = new List<string>();
            foreach (var f in fields) {
                object va = f.GetValue(o[i]), vb = f.GetValue(m[i]);
                if (!Eq(va, vb)) diffs.Add(f.Name + ": " + Val(va) + " -> " + Val(vb));
            }
            if (diffs.Count > 0) {
                changed++;
                Console.WriteLine("- **#" + i + " " + nameOf(o[i]) + "**");
                foreach (var d in diffs) Console.WriteLine("  - " + d);
            }
        }
        if (m.Count != o.Count) Console.WriteLine("- record count changed: " + o.Count + " -> " + m.Count);
        Console.WriteLine("(" + changed + " records changed)");
    }
    static int Main(string[] args) {
        var a = new HistoryLoader(); a.Load(System.IO.Path.Combine(args[0], "index.dat"), false);
        var b = new HistoryLoader(); b.Load(System.IO.Path.Combine(args[1], "index.dat"), false);
        Console.WriteLine("# GSLP 2023 database edits (vs original April 2023 update)");
        Compare("club_comp.dat", a.club_comp, b.club_comp, r => S(((TComp)r).Name));
        Compare("nation_comp.dat", a.nation_comp, b.nation_comp, r => S(((TComp)r).Name));
        Compare("club.dat (structure fields)", a.club, b.club, r => S(((TClub)r).Name));
        Compare("nation.dat", a.nation, b.nation, r => S(((TNation)r).Name));
        Compare("continent.dat", a.continent, b.continent, r => S(((TContinent)r).ContinentName));
        return 0;
    }
}
