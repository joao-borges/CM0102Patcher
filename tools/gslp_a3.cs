using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CM0102Patcher;

// A3: apply GSLP 2023's competition STRUCTURE to a May-2026 database copy.
// - 14 club_comp redefinitions (names/reputations) from the A1 decode
// - Brazilian national pyramid rebuilt from CURRENT memberships by reputation cascade
//   (A 20, B 20, C 20, D 36 on comp #270), per user rule: current clubs, no curation
// - nation tweaks (SoD, Australia->AFC, Yugoslavia off, Uruguay region)
// - name-keyed replay of slow-drift moves (Americas/state leagues) from moves.tsv
// usage: mono gslp_a3.exe <DataDir> <moves.tsv>
class GslpA3 {
    static Encoding L = Encoding.GetEncoding("ISO-8859-1");
    static string S(byte[] b){ int n=Array.IndexOf(b,(byte)0); if(n<0)n=b.Length; return L.GetString(b,0,n); }
    static void SetB(byte[] dst, string s){ Array.Clear(dst,0,dst.Length); var x=L.GetBytes(s); Array.Copy(x,dst,Math.Min(x.Length,dst.Length-1)); }

    static int Main(string[] args) {
        string dataDir = args[0];
        var hl = new HistoryLoader(); hl.Load(Path.Combine(dataDir,"index.dat"), false);

        // ---- 1. competition redefinitions (A1 decode; absolute target values) ----
        Action<int,string,string,string,int> comp = (i,name,shortName,tla,rep) => {
            var c = hl.club_comp[i];
            if (name!=null) SetB(c.Name,name);
            if (shortName!=null) SetB(c.ShortName,shortName);
            if (tla!=null) SetB(c.ThreeLetterName,tla);
            if (rep>=0) c.ClubCompReputation=(short)rep;
        };
        comp(58,"Copa Libertadores",null,null,17);
        comp(61,null,null,null,12);                                  // Recopa Sudamericana
        comp(65,null,null,null,14);                                  // Serie A
        comp(67,null,null,null,11);                                  // Paulista
        comp(68,null,null,null,13);                                  // Copa do Brasil
        comp(85,"Argentine Cup","Argentine Cup",null,-1);
        comp(92,"Copa Sudamericana","Copa Sudamericana",null,12);
        comp(104,null,null,null,17);                                 // Club World Cup
        comp(218,"Mexican Cup","Mexican Cup",null,-1);
        comp(270,"Brazilian 4th Division","National Fourth Division","ND4",-1);
        comp(274,"Brazilian Supercup","Supercup",null,-1);
        comp(326,"UEFA Champions League","Champions League",null,-1);
        comp(328,"EURO League","Europa League",null,-1);
        comp(330,"UEFA Conference","Conference League",null,-1);
        Console.WriteLine("comp redefinitions applied: 14");

        // Brazilian context + viability helper (used by moves filter and pyramid)
        int brazil = hl.nation.First(n => S(n.Name)=="Brazil").ID;
        Func<TClub,bool> viable = c => c.Squad.Count(x => x>0) >= 16;

        // ---- 2. name-keyed slow-drift replay (viable movers only) ----
        int applied=0; var missing=new List<string>();
        foreach (var line in File.ReadAllLines(args[1])) {
            var p=line.Split('\t'); if(p.Length<2) continue;
            var c=hl.club.FirstOrDefault(x=>S(x.Name)==p[0]);
            if (c==null){ missing.Add(p[0]); continue; }
            if (!viable(c)){ missing.Add(p[0]+" (skipped: no squad)"); continue; }
            c.Division=int.Parse(p[1]); applied++;
        }
        Console.WriteLine("name-keyed moves: "+applied+" applied, "+missing.Count+" missing");
        foreach(var m in missing) Console.WriteLine("  missing: "+m);

        // ---- 3. Brazilian pyramid: reputation cascade over CURRENT memberships ----
        var br = hl.club.Where(c => c.Nation==brazil).ToList();
        Func<int,List<TClub>> inDiv = d => br.Where(c => c.Division==d && viable(c)).ToList();
        var demoted = br.Where(c => (c.Division==65||c.Division==79||c.Division==80) && !viable(c)).ToList();
        var reserveComps = new HashSet<int>();
        for (int i=0;i<hl.club_comp.Count;i++) if (S(hl.club_comp[i].Name).Contains("Reserve")) reserveComps.Add(i);

        var poolA = inDiv(65).OrderByDescending(c=>c.Reputation).ToList();
        var keepA = poolA.Take(20).ToList(); var overA = poolA.Skip(20).ToList();
        var poolB = inDiv(79).Concat(overA).OrderByDescending(c=>c.Reputation).ToList();
        var keepB = poolB.Take(20).ToList(); var overB = poolB.Skip(20).ToList();
        var poolC = inDiv(80).Concat(overB).OrderByDescending(c=>c.Reputation).ToList();
        var keepC = poolC.Take(20).ToList(); var overC = poolC.Skip(20).ToList();
        var unassigned = br.Where(c => c.Division<0 && viable(c)).OrderByDescending(c=>c.Reputation).ToList();
        var poolD = overC.Concat(unassigned).ToList();     // cascade first, then best of the rest
        var keepD = poolD.Take(36).ToList();
        Action<List<TClub>,int> assign = (list,div) => { foreach(var c in list) c.Division=div; };
        assign(keepA,65); assign(keepB,79); assign(keepC,80); assign(keepD,270);
        foreach (var c in poolD.Skip(36)) if (c.Division==65||c.Division==79||c.Division==80) c.Division=-1;
        foreach (var c in demoted) c.Division=-1;
        Console.WriteLine("non-viable clubs demoted to unassigned: "+demoted.Count);
        Console.WriteLine("pyramid: A="+keepA.Count+" B="+keepB.Count+" C="+keepC.Count+" D="+keepD.Count);
        Console.WriteLine("  D from cascade: "+keepD.Count(c=>overC.Contains(c))+", from unassigned: "+keepD.Count(c=>unassigned.Contains(c)));

        // ---- 3. nation tweaks ----
        Action<string,Action<TNation>> nat = (name,fix) => { var n=hl.nation.FirstOrDefault(x=>S(x.Name)==name); if(n!=null) fix(n); };
        nat("Argentina", n=>n.StateOfDevelopment=2);
        nat("Brazil",    n=>n.StateOfDevelopment=2);
        nat("Chile",     n=>n.StateOfDevelopment=2);
        nat("Uruguay",   n=>{n.StateOfDevelopment=2; n.Region=4;});
        nat("Australia", n=>n.Continent=1);
        nat("Yugoslavia",n=>n.Continent=-1);
        Console.WriteLine("nation tweaks applied");

        // ---- 4b. state comps keep their ORIGINAL viable members (regional cups populate
        // from them — emptying them nulls the bra_reg competitions); only EMPTY-squad clubs
        // leave scheduled divisions; unassigned clubs stay unassigned (stock convention) ----
        var pyramidOnly = new HashSet<int>{65,79,80,270};
        int hollowed=0;
        foreach (var c in br.Where(c => pyramidOnly.Contains(c.Division) && c.Squad.Count(x=>x>0)==0)) { c.Division=-1; hollowed++; }
        Console.WriteLine("empty-squad clubs removed from PYRAMID divisions only: "+hollowed+" (state comps keep all original members - stock engine tolerates shells)");
        // pad Serie D to GS's exact 36 with best remaining unassigned (allow thinner squads >=11)
        var dNow = br.Count(c=>c.Division==270);
        if (dNow<36) {
            var pad = br.Where(c=>c.Division<0 && c.Squad.Count(x=>x>0)>=11).OrderByDescending(c=>c.Reputation).Take(36-dNow).ToList();
            foreach (var c in pad) c.Division=270;
            Console.WriteLine("Serie D padded: +"+pad.Count+" -> "+br.Count(c=>c.Division==270));
        }
        // binder name alignments (GS engine looks these up by name)
        Action<string,string> rn = (from,to) => { var c=hl.club.FirstOrDefault(x=>S(x.Name)==from); if(c!=null){ SetB(c.Name,to); Console.WriteLine("club renamed: "+from+" -> "+to);} };
        rn("Criciúma EC","Criciúma Esporte Clube");
        rn("Avaí Futebol Clube (SC)","Avaí Futebol Clube");
        for(int i=0;i<hl.club_comp.Count;i++){ var n=S(hl.club_comp[i].Name); if(n=="Wales Premier League"||n=="Cymru Premier"){ SetB(hl.club_comp[i].Name,"Welsh Premier Division"); Console.WriteLine("comp renamed: "+n+" -> Welsh Premier Division"); } }

        // ---- save ----
        hl.Save(Path.Combine(dataDir,"index.dat"), true, false, true);
        Console.WriteLine("saved");
        return 0;
    }
}
