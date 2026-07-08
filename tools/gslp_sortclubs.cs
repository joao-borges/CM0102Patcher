using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CM0102Patcher;

// Cosmetics: physically re-sort club.dat alphabetically (the game lists clubs in record
// order; GS's table is in append order). GS's engine binds clubs BY LONG NAME at startup
// (proven by the LAFC/Sporting-KC rename breakage), so a permutation is transparent to his
// hooks. All club-index references inside the DB are remapped with the permutation:
//   club.ID (=index), club.Rival1-3, staff.ClubJob, preferences Fav/DisClubs x6,
//   staff_history.ClubID, club_comp_history Winners/RunnersUp/ThirdPlace/Host.
// nat_club is a separate table (untouched). New-game only; not save-compatible.
// usage: mono gslp_sortclubs.exe <dataDir>
class GslpSortClubs {
    static Encoding L = Encoding.GetEncoding("ISO-8859-1");
    static string S(byte[] b){ int n=Array.IndexOf(b,(byte)0); if(n<0)n=b.Length; return L.GetString(b,0,n); }
    static string Deaccent(string s){
        var sb=new StringBuilder();
        foreach(var ch in s.ToLowerInvariant()){
            var c=ch;
            if("áàâãä".IndexOf(c)>=0)c='a'; else if("éèêë".IndexOf(c)>=0)c='e'; else if("íìîï".IndexOf(c)>=0)c='i';
            else if("óòôõö".IndexOf(c)>=0)c='o'; else if("úùûü".IndexOf(c)>=0)c='u'; else if(c=='ç')c='c';
            else if("ñ".IndexOf(c)>=0)c='n';
            sb.Append(c);
        }
        return sb.ToString();
    }

    static void Main(string[] args){
        var dir = args[0];
        var h = new HistoryLoader(); h.Load(Path.Combine(dir, "index.dat"), false);
        int n = h.club.Count;
        Console.WriteLine("clubs: " + n);

        // sanity: ID == index throughout (the loader/refs rely on it)
        for(int i=0;i<n;i++) if(h.club[i].ID != i) { Console.WriteLine("FATAL: club["+i+"].ID="+h.club[i].ID); return; }

        // sort: alphabetical by SHORT name — that's what list screens display (league tables,
        // search results); long name is the tiebreak, empty-named clubs go last
        Func<int,string> sortKey = i => {
            var sn = S(h.club[i].ShortName).Trim();
            return Deaccent(sn.Length>0 ? sn : S(h.club[i].Name));
        };
        var order = Enumerable.Range(0,n)
            .OrderBy(i => (S(h.club[i].ShortName).Trim().Length==0 && S(h.club[i].Name).Trim().Length==0) ? 1 : 0)
            .ThenBy(sortKey, StringComparer.Ordinal)
            .ThenBy(i => Deaccent(S(h.club[i].Name)), StringComparer.Ordinal)
            .ThenBy(i => i)
            .ToList();
        var perm = new int[n];                       // old index -> new index
        for(int newIdx=0;newIdx<n;newIdx++) perm[order[newIdx]] = newIdx;

        Func<int,int> M = v => (v>=0 && v<n) ? perm[v] : v;

        var sorted = order.Select(i => h.club[i]).ToList();
        for(int i=0;i<n;i++){
            sorted[i].ID = i;
            sorted[i].Rival1 = M(sorted[i].Rival1);
            sorted[i].Rival2 = M(sorted[i].Rival2);
            sorted[i].Rival3 = M(sorted[i].Rival3);
        }
        h.club = sorted;

        int c1=0;
        foreach(var st in h.staff) if(st.ClubJob>=0 && st.ClubJob<n){ st.ClubJob=perm[st.ClubJob]; c1++; }
        Console.WriteLine("staff.ClubJob remapped: " + c1);

        int c2=0;
        foreach(var pr in h.preferences){
            int b=c2;
            if(pr.StaffFavouriteClubs1>=0 && pr.StaffFavouriteClubs1<n){ pr.StaffFavouriteClubs1=perm[pr.StaffFavouriteClubs1]; c2++; }
            if(pr.StaffFavouriteClubs2>=0 && pr.StaffFavouriteClubs2<n){ pr.StaffFavouriteClubs2=perm[pr.StaffFavouriteClubs2]; c2++; }
            if(pr.StaffFavouriteClubs3>=0 && pr.StaffFavouriteClubs3<n){ pr.StaffFavouriteClubs3=perm[pr.StaffFavouriteClubs3]; c2++; }
            if(pr.StaffDislikedClubs1>=0 && pr.StaffDislikedClubs1<n){ pr.StaffDislikedClubs1=perm[pr.StaffDislikedClubs1]; c2++; }
            if(pr.StaffDislikedClubs2>=0 && pr.StaffDislikedClubs2<n){ pr.StaffDislikedClubs2=perm[pr.StaffDislikedClubs2]; c2++; }
            if(pr.StaffDislikedClubs3>=0 && pr.StaffDislikedClubs3<n){ pr.StaffDislikedClubs3=perm[pr.StaffDislikedClubs3]; c2++; }
        }
        Console.WriteLine("preferences club refs remapped: " + c2);

        int c3=0;
        foreach(var sh in h.staff_history) if(sh.ClubID>=0 && sh.ClubID<n){ sh.ClubID=perm[sh.ClubID]; c3++; }
        Console.WriteLine("staff_history.ClubID remapped: " + c3);

        int c4=0;
        foreach(var ch in h.club_comp_history){
            if(ch.Winners>=0 && ch.Winners<n){ ch.Winners=perm[ch.Winners]; c4++; }
            if(ch.RunnersUp>=0 && ch.RunnersUp<n){ ch.RunnersUp=perm[ch.RunnersUp]; c4++; }
            if(ch.ThirdPlace>=0 && ch.ThirdPlace<n){ ch.ThirdPlace=perm[ch.ThirdPlace]; c4++; }
            if(ch.Host>=0 && ch.Host<n){ ch.Host=perm[ch.Host]; c4++; }
        }
        Console.WriteLine("club_comp_history refs remapped: " + c4);

        h.Save(Path.Combine(dir, "index.dat"), true, true, true);
        Console.WriteLine("saved. first clubs now: " +
            string.Join(" | ", h.club.Take(5).Select(c => S(c.Name))));
    }
}
