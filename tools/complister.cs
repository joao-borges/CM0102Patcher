using System; using System.Linq; using System.Text; using CM0102Patcher;
class CompLister {
    static Encoding L = Encoding.GetEncoding("ISO-8859-1");
    static string S(byte[] b){int n=Array.IndexOf(b,(byte)0);if(n<0)n=b.Length;return L.GetString(b,0,n);}
    static int Main(string[] a){
        var hl=new HistoryLoader(); hl.Load(System.IO.Path.Combine(a[0],"index.dat"),false);
        if(a.Length>1 && a[1]=="nations"){ for(int i=0;i<hl.nation.Count;i++) Console.WriteLine(i+"\t"+S(hl.nation[i].Name)); }
        else { for(int i=0;i<hl.club_comp.Count;i++) Console.WriteLine(i+"\t"+S(hl.club_comp[i].Name)); }
        return 0;
    }
}
