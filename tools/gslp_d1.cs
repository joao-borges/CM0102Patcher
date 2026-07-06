using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CM0102Patcher;

// Direction D: transplant May-2026 players INTO GS's Abril-23 database.
// GS's engine is coupled to HIS club table (hand tables reference his club indices),
// so we keep his club/comp/nation/history tables and bring OUR staff side over:
//   staff.dat (whole file, raw-copied beforehand) + names + staff histories,
//   staff.ClubJob remapped our-club-idx -> his-club-idx by (normalized) name,
//   his clubs' Squad/Coaches/Scouts/Physios/Directors arrays replaced with the
//   name-matched May-2026 club's arrays (indices point into OUR staff = the file on disk),
//   Brazilian A/B/C/D divisions realigned to the May-2026 lineups by name.
// usage: mono gslp_d1.exe <outDir=copy of HIS Data with our staff-side files+index pre-swapped> <ourDataDir>
class GslpD1 {
    static Encoding L = Encoding.GetEncoding("ISO-8859-1");
    static string S(byte[] b){ int n=Array.IndexOf(b,(byte)0); if(n<0)n=b.Length; return L.GetString(b,0,n); }
    static string Deaccent(string s){
        var sb=new StringBuilder();
        foreach(var ch in s.ToLowerInvariant()){
            var c=ch;
            if("áàâãä".IndexOf(c)>=0)c='a'; else if("éèêë".IndexOf(c)>=0)c='e'; else if("íìîï".IndexOf(c)>=0)c='i';
            else if("óòôõö".IndexOf(c)>=0)c='o'; else if("úùûü".IndexOf(c)>=0)c='u'; else if(c=='ç')c='c';
            sb.Append(c);
        }
        return sb.ToString();
    }
    static readonly Dictionary<string,string> ABBR=new Dictionary<string,string>{
        {"ec","esporte clube"},{"fc","futebol clube"},{"ac","atletico clube"},{"aa","associacao atletica"},
        {"ad","associacao desportiva"},{"sd","sociedade desportiva"},{"se","sociedade esportiva"},
        {"cs","club sportivo"},{"sc","sport club"},{"ca","clube atletico"},{"cr","clube regatas"}};   // NB expansions must be filler-free ("de" is dropped from real names)
    static readonly HashSet<string> FILLER=new HashSet<string>{"de","do","da","das","dos","e"};
    // returns (key, state) — state from a trailing "(XX)" if present
    static Tuple<string,string> Key(string name){
        string state="";
        var m=System.Text.RegularExpressions.Regex.Match(name,@"\(([A-Z]{2})\)\s*$");
        if(m.Success){ state=m.Groups[1].Value; name=name.Substring(0,m.Index); }
        var toks=System.Text.RegularExpressions.Regex.Split(Deaccent(name),@"[^a-z0-9]+")
            .Where(t=>t.Length>0 && !FILLER.Contains(t))
            .Select(t=>ABBR.ContainsKey(t)?ABBR[t]:t);
        return Tuple.Create(string.Join(" ",toks),state);   // space-joined: containment respects token boundaries
    }

    static void CopyStaffRefs(TClub src, TClub dst){
        Array.Copy(src.Squad,dst.Squad,dst.Squad.Length);
        Array.Copy(src.Coaches,dst.Coaches,dst.Coaches.Length);
        Array.Copy(src.Scouts,dst.Scouts,dst.Scouts.Length);
        Array.Copy(src.Physios,dst.Physios,dst.Physios.Length);
        Array.Copy(src.Directors,dst.Directors,dst.Directors.Length);
        dst.Chairman=src.Chairman; dst.Manager=src.Manager; dst.AssistantManager=src.AssistantManager;
        dst.FavStaff1=src.FavStaff1; dst.FavStaff2=src.FavStaff2; dst.FavStaff3=src.FavStaff3;
        dst.DisStaff1=src.DisStaff1; dst.DisStaff2=src.DisStaff2; dst.DisStaff3=src.DisStaff3;
    }
    static void ClearStaffRefs(TClub c){
        for(int k=0;k<c.Squad.Length;k++)c.Squad[k]=-1;
        for(int k=0;k<c.Coaches.Length;k++)c.Coaches[k]=-1;
        for(int k=0;k<c.Scouts.Length;k++)c.Scouts[k]=-1;
        for(int k=0;k<c.Physios.Length;k++)c.Physios[k]=-1;
        for(int k=0;k<c.Directors.Length;k++)c.Directors[k]=-1;
        c.Chairman=-1; c.Manager=-1; c.AssistantManager=-1;
        c.FavStaff1=-1; c.FavStaff2=-1; c.FavStaff3=-1;
        c.DisStaff1=-1; c.DisStaff2=-1; c.DisStaff3=-1;
    }

    static int Main(string[] args) {
        string outDir=args[0], ourDir=args[1];
        var his=new HistoryLoader(); his.Load(Path.Combine(outDir,"index.dat"), false);   // club side = his; staff side = ours (files pre-swapped)
        var ours=new HistoryLoader(); ours.Load(Path.Combine(ourDir,"index.dat"), false);

        // sanity: nation tables must align (staff.Nation indices cross over)
        int natMismatch=0;
        for(int i=0;i<Math.Min(his.nation.Count,ours.nation.Count);i++)
            if(S(his.nation[i].Name)!=S(ours.nation[i].Name)) natMismatch++;
        Console.WriteLine("nation table: his="+his.nation.Count+" ours="+ours.nation.Count+" name mismatches="+natMismatch);

        // club name maps (expanded keys + state awareness)
        var hisKeys=new List<Tuple<string,string>>();
        var hisByKey=new Dictionary<string,List<int>>();
        var hisShortKeys=new List<string>();
        var hisByShort=new Dictionary<string,List<int>>();
        for(int i=0;i<his.club.Count;i++){
            var kk=Key(S(his.club[i].Name)); hisKeys.Add(kk);
            if(!hisByKey.ContainsKey(kk.Item1)) hisByKey[kk.Item1]=new List<int>();
            hisByKey[kk.Item1].Add(i);
            var sk=Key(S(his.club[i].ShortName)).Item1; hisShortKeys.Add(sk);
            if(sk.Length>0){
                if(!hisByShort.ContainsKey(sk)) hisByShort[sk]=new List<int>();
                hisByShort[sk].Add(i);
            }
        }
        var ourToHis=new int[ours.club.Count];
        int matched=0, contained=0, shortFixed=0, shortMatched=0;
        for(int i=0;i<ours.club.Count;i++){
            ourToHis[i]=-1;
            var ok=Key(S(ours.club[i].Name));
            var osk=Key(S(ours.club[i].ShortName)).Item1;
            // nation guard: nation tables are aligned 1:1 (verified above), so a match is only
            // valid within the same nation — kills cross-nation crossings (Al-Ahli SYR vs QAT etc.).
            // Our DB deactivates dead clubs with Nation=-1; only enforce when BOTH sides are valid.
            int nat=ours.club[i].Nation;
            Func<int,bool> natOK = c => nat<0 || his.club[c].Nation<0 || his.club[c].Nation==nat;
            // unique short-name hit (used both as recycled-record tiebreak and as fallback):
            // update-makers reliably update SHORT names on recycled records; long names go stale
            // (e.g. Inter Miami living in a record still long-named 'Miami Fusion FC').
            int shortHit=-1;
            List<int> scands;
            if(osk.Length>0 && hisByShort.TryGetValue(osk,out scands)){
                var sc=scands.Where(c=>natOK(c)).ToList();
                if(sc.Count==1) shortHit=sc[0];
            }
            List<int> cands;
            if(hisByKey.TryGetValue(ok.Item1,out cands)){
                // prefer state-consistent; rank: exact state match > stateless > cross-state
                // (fixes Santos FC matching Santos (AP): stateless ours must prefer stateless his)
                var pick=cands.Where(c=>natOK(c))
                              .Where(c=>ok.Item2==""||hisKeys[c].Item2==""||hisKeys[c].Item2==ok.Item2)
                              .OrderByDescending(c=>hisKeys[c].Item2==ok.Item2?2:(hisKeys[c].Item2==""?1:0))
                              .ToList();
                if(pick.Count>0){
                    int p=pick[0];
                    // recycled-record override: long names agree but short names point elsewhere
                    if(shortHit>=0 && shortHit!=p && osk.Length>0 && hisShortKeys[p]!=osk){
                        Console.WriteLine("  short-name override: '"+S(ours.club[i].Name)+"' [short '"+S(ours.club[i].ShortName)+"'] -> his '"+S(his.club[shortHit].Name)+"' (long-name pick was '"+S(his.club[p].Name)+"' short '"+S(his.club[p].ShortName)+"')");
                        p=shortHit; shortFixed++;
                    }
                    ourToHis[i]=p; matched++; continue;
                }
            }
            // short-name fallback: no long-name match, but a unique short-name match exists
            if(shortHit>=0){ ourToHis[i]=shortHit; matched++; shortMatched++; continue; }
            // containment fallback: unique his key that contains/is contained by ours, state-consistent, len>=8
            if(ok.Item1.Length>=8){
                int hit=-1, hits=0;
                for(int c=0;c<hisKeys.Count;c++){
                    if(!natOK(c)) continue;
                    var hk=hisKeys[c];
                    if(ok.Item2!="" && hk.Item2!="" && hk.Item2!=ok.Item2) continue;
                    if(hk.Item1.Length>=8 && ((" "+hk.Item1+" ").Contains(" "+ok.Item1+" ")||(" "+ok.Item1+" ").Contains(" "+hk.Item1+" "))){ hit=c; hits++; if(hits>1)break; }
                }
                if(hits==1){ ourToHis[i]=hit; matched++; contained++; }
            }
        }
        Console.WriteLine("club match: "+matched+"/"+ours.club.Count+" ("+contained+" via containment, "+shortMatched+" via short-name, "+shortFixed+" recycled-record overrides)");
        // audit: matched pairs whose short names disagree AND both sides sit in a division —
        // candidates for further recycled-record crossings; review manually.
        int audits=0;
        for(int i=0;i<ours.club.Count;i++){
            int t=ourToHis[i]; if(t<0) continue;
            var osk=Key(S(ours.club[i].ShortName)).Item1;
            if(osk.Length==0||hisShortKeys[t].Length==0) continue;
            if(osk!=hisShortKeys[t] && !osk.Contains(hisShortKeys[t]) && !hisShortKeys[t].Contains(osk)
               && ours.club[i].Division>=0 && his.club[t].Division>=0){
                if(audits<25) Console.WriteLine("  AUDIT short-name mismatch: ours '"+S(ours.club[i].Name)+"'/'"+S(ours.club[i].ShortName)+"' (div "+ours.club[i].Division+") -> his '"+S(his.club[t].Name)+"'/'"+S(his.club[t].ShortName)+"' (div "+his.club[t].Division+")");
                audits++;
            }
        }
        Console.WriteLine("audit: "+audits+" matched pairs with disagreeing short names (both in divisions)");
        // enforce INJECTIVITY: the game asserts staff<->club slot consistency (Database.cpp:1583),
        // so two our-clubs must never map to the same his-club. First claimant wins.
        var claimed=new int[his.club.Count]; for(int k=0;k<claimed.Length;k++) claimed[k]=-1;
        int deduped=0;
        for(int i=0;i<ours.club.Count;i++){
            int t=ourToHis[i];
            if(t<0) continue;
            if(claimed[t]<0) claimed[t]=i;
            else { ourToHis[i]=-1; deduped++; }
        }
        Console.WriteLine("injectivity: "+deduped+" duplicate mappings dropped");

        // 1. staff side: HIS loader already loaded OUR staff (files were pre-swapped). Remap ClubJob.
        int remapped=0, freed=0;
        for(int i=0;i<his.staff.Count;i++){
            var st=his.staff[i];
            if(st.ClubJob>=0 && st.ClubJob<ourToHis.Length){
                int t=ourToHis[st.ClubJob];
                if(t>=0){ st.ClubJob=t; remapped++; } else { st.ClubJob=-1; freed++; }
            } else if (st.ClubJob>=0) { st.ClubJob=-1; freed++; }
        }
        Console.WriteLine("staff.ClubJob: remapped="+remapped+" freed(no his-club)="+freed);

        // 1b. preferences: favourite/disliked CLUB indices must remap to his table too
        int prefRemap=0, prefClear=0;
        Func<int,int> mapClub = v => {
            if(v<0 || v>=ourToHis.Length) return -1;
            return ourToHis[v];
        };
        foreach(var pr in his.preferences){
            int[] before={pr.StaffFavouriteClubs1,pr.StaffFavouriteClubs2,pr.StaffFavouriteClubs3,pr.StaffDislikedClubs1,pr.StaffDislikedClubs2,pr.StaffDislikedClubs3};
            pr.StaffFavouriteClubs1 = before[0]>=0 ? mapClub(before[0]) : before[0];
            pr.StaffFavouriteClubs2 = before[1]>=0 ? mapClub(before[1]) : before[1];
            pr.StaffFavouriteClubs3 = before[2]>=0 ? mapClub(before[2]) : before[2];
            pr.StaffDislikedClubs1  = before[3]>=0 ? mapClub(before[3]) : before[3];
            pr.StaffDislikedClubs2  = before[4]>=0 ? mapClub(before[4]) : before[4];
            pr.StaffDislikedClubs3  = before[5]>=0 ? mapClub(before[5]) : before[5];
            for(int k=0;k<6;k++) if(before[k]>=0){ if(mapClub(before[k])>=0) prefRemap++; else prefClear++; }
        }
        Console.WriteLine("preferences club refs: remapped="+prefRemap+" cleared="+prefClear);

        // 2. staff_history.ClubID remap (cosmetic but keeps history screens sane)
        int shRemap=0, shClear=0;
        foreach(var h in his.staff_history){
            if(h.ClubID>=0 && h.ClubID<ourToHis.Length){
                int t=ourToHis[h.ClubID];
                if(t>=0){ h.ClubID=t; shRemap++; } else { h.ClubID=-1; shClear++; }
            } else if(h.ClubID>=0) { h.ClubID=-1; shClear++; }
        }
        Console.WriteLine("staff_history.ClubID: remapped="+shRemap+" cleared="+shClear);

        // 3. club person arrays: his club gets the name-matched our-club's arrays (valid vs OUR staff file);
        //    unmatched his clubs get cleared arrays (their old indices point into a staff table that no longer exists)
        var hisToOur=new int[his.club.Count]; for(int i=0;i<hisToOur.Length;i++) hisToOur[i]=-1;
        for(int i=0;i<ours.club.Count;i++) if(ourToHis[i]>=0 && hisToOur[ourToHis[i]]<0) hisToOur[ourToHis[i]]=i;
        int copied=0, cleared=0;
        for(int i=0;i<his.club.Count;i++){
            var hc=his.club[i]; int oi=hisToOur[i];
            if(oi>=0){
                var oc=ours.club[oi];
                CopyStaffRefs(oc,hc);
                copied++;
            } else {
                ClearStaffRefs(hc);
                cleared++;
            }
        }
        Console.WriteLine("club arrays: copied from May-2026 for "+copied+" clubs, cleared for "+cleared+" unmatched his-clubs");

        // 3b. national teams: same order both DBs (verified 462/462 name match) —
        //     take OUR staff-referencing fields (squads valid vs our staff file), keep his stadiums etc.
        for(int i=0;i<Math.Min(his.nat_club.Count,ours.nat_club.Count);i++){
            CopyStaffRefs(ours.nat_club[i],his.nat_club[i]);
            // cosmetics: May-2026 kit colours + names (colour.dat is swapped to ours; tables align 1:1)
            var hn=his.nat_club[i]; var on=ours.nat_club[i];
            Array.Copy(on.Name,hn.Name,hn.Name.Length); hn.GenderName=on.GenderName;
            Array.Copy(on.ShortName,hn.ShortName,hn.ShortName.Length); hn.ShortGenderName=on.ShortGenderName;
            hn.ForeColour1=on.ForeColour1; hn.BackColour1=on.BackColour1;
            hn.ForeColour2=on.ForeColour2; hn.BackColour2=on.BackColour2;
            hn.ForeColour3=on.ForeColour3; hn.BackColour3=on.BackColour3;
        }
        Console.WriteLine("nat_club: staff refs + names + kit colours copied for "+Math.Min(his.nat_club.Count,ours.nat_club.Count)+" national teams");
        // stale recycled long names: BOTH databases recycled these dead-franchise records and
        // forgot the long name (short name is the maintained truth). Fix the display name.
        var staleLong=new Dictionary<string,string>{ {"Tampa Bay Mutiny|LAFC","Los Angeles FC"}, {"Kansas City Wizards|Sporting KC","Sporting Kansas City"} };
        for(int i=0;i<his.club.Count;i++){
            string k=S(his.club[i].Name)+"|"+S(his.club[i].ShortName);
            string fix;
            if(staleLong.TryGetValue(k,out fix)){
                var b=L.GetBytes(fix); Array.Clear(his.club[i].Name,0,his.club[i].Name.Length);
                Array.Copy(b,his.club[i].Name,Math.Min(b.Length,his.club[i].Name.Length-1));
                Console.WriteLine("  stale long name fixed: '"+k+"' -> '"+fix+"'");
            }
        }
        // audit: active matched clubs whose long and short names share NO token — candidates
        // for more stale recycled long names (hand-extend staleLong above as user reports them)
        int stale=0;
        for(int i=0;i<his.club.Count;i++){
            if(his.club[i].Division<0||his.club[i].Reputation<4000) continue;
            var lk=Key(S(his.club[i].Name)).Item1.Split(' ');
            var sk=Key(S(his.club[i].ShortName)).Item1.Split(' ');
            if(lk.Length==0||sk.Length==0||sk[0].Length==0) continue;
            if(!lk.Intersect(sk).Any()){
                if(stale<15) Console.WriteLine("  AUDIT stale-long-name candidate: '"+S(his.club[i].Name)+"' short '"+S(his.club[i].ShortName)+"' (div "+his.club[i].Division+" rep "+his.club[i].Reputation+")");
                stale++;
            }
        }
        Console.WriteLine("stale-long-name candidates (active, rep>=4000): "+stale);
        // club kit colours from May-2026 for matched clubs (indices into the swapped colour.dat)
        int kitCopied=0;
        for(int i=0;i<ours.club.Count;i++){
            int t=ourToHis[i]; if(t<0) continue;
            var hc=his.club[t]; var oc=ours.club[i];
            if(hc.ForeColour1!=oc.ForeColour1||hc.BackColour1!=oc.BackColour1||hc.ForeColour2!=oc.ForeColour2||
               hc.BackColour2!=oc.BackColour2||hc.ForeColour3!=oc.ForeColour3||hc.BackColour3!=oc.BackColour3){
                hc.ForeColour1=oc.ForeColour1; hc.BackColour1=oc.BackColour1;
                hc.ForeColour2=oc.ForeColour2; hc.BackColour2=oc.BackColour2;
                hc.ForeColour3=oc.ForeColour3; hc.BackColour3=oc.BackColour3;
                kitCopied++;
            }
        }
        Console.WriteLine("club kit colours updated from May-2026: "+kitCopied);

        // 4. Brazilian pyramid realignment to May-2026 lineups (ours already A3-shaped: 65/79/80/270)
        int brazilHis=his.nation.ToList().FindIndex(n=>S(n.Name)=="Brazil");
        int brazilOur=ours.nation.ToList().FindIndex(n=>S(n.Name)=="Brazil");
        var pyramid=new[]{65,79,80,270};
        int moved=0, missing=0;
        // first: any his-BR club currently in pyramid leaves it (down to his catch-all #357)
        foreach(var c in his.club) if(c.Nation==brazilHis && pyramid.Contains(c.Division)) c.Division=357;
        foreach(var div in pyramid){
            for(int i=0;i<ours.club.Count;i++){
                var oc=ours.club[i];
                if(oc.Nation!=brazilOur || oc.Division!=div) continue;
                int t=ourToHis[i];
                if(t>=0){ his.club[t].Division=div; moved++; }
                else missing++;
            }
        }
        Console.WriteLine("BR pyramid realigned: "+moved+" clubs placed, "+missing+" May-2026 pyramid clubs missing from his table");
        // fill C to 20 and D to 36 with best remaining his-BR clubs (transplanted squads preferred)
        var targets=new Dictionary<int,int>{{65,20},{79,20},{80,20},{270,36}};
        foreach(var div in pyramid){
            int have=his.club.Count(c=>c.Nation==brazilHis&&c.Division==div);
            int need=targets[div]-have;
            if(need<=0) continue;
            var pool=Enumerable.Range(0,his.club.Count)
                .Where(i=>his.club[i].Nation==brazilHis && !pyramid.Contains(his.club[i].Division))
                .OrderByDescending(i=>hisToOur[i]>=0?1:0)                    // matched (has 2026 squad) first
                .ThenByDescending(i=>his.club[i].Reputation)
                .Take(need).ToList();
            foreach(var i in pool) his.club[i].Division=div;
            Console.WriteLine("  div "+div+": filled +"+pool.Count);
        }
        // hard guarantee: every pyramid club has a non-empty squad — swap offenders for
        // matched clubs with real squads (empty-squad clubs in playable divisions crash world-gen)
        int swapped=0;
        foreach(var div in pyramid){
            for(int i=0;i<his.club.Count;i++){
                var c=his.club[i];
                if(c.Nation!=brazilHis || c.Division!=div || c.Squad.Any(x=>x>=0)) continue;
                var repl=Enumerable.Range(0,his.club.Count).Where(k=>
                    his.club[k].Nation==brazilHis && !pyramid.Contains(his.club[k].Division)
                    && his.club[k].Squad.Any(x=>x>=0))
                    .OrderByDescending(k=>his.club[k].Reputation).Cast<int?>().FirstOrDefault();
                if(repl.HasValue){
                    his.club[repl.Value].Division=div;
                    c.Division=357;
                    Console.WriteLine("  swapped empty-squad '"+S(c.Name)+"' out of div "+div+" for '"+S(his.club[repl.Value].Name)+"'");
                    swapped++;
                }
            }
        }
        Console.WriteLine("empty-squad pyramid swaps: "+swapped);
        foreach(var div in pyramid)
            Console.WriteLine("  div "+div+": "+his.club.Count(c=>c.Nation==brazilHis&&c.Division==div)+" clubs, empty-squad: "+his.club.Count(c=>c.Nation==brazilHis&&c.Division==div&&c.Squad.All(x=>x<0)));

        his.Save(Path.Combine(outDir,"index.dat"), true, true, true);
        Console.WriteLine("saved");
        return 0;
    }
}
