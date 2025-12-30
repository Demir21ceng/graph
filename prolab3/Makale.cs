

namespace prolab3;

public class Makale
{
    
    public string Id { get; set; }
    public string Title { get; set; }
    public int Year { get; set; }
    public string Venue { get; set; }
    public string Doi { get; set; }
    
    public List<string> Authors { get; set; }
    
   
    public LinkedList<string> ReferencedWorks { get; set; }
    public LinkedList<Makale> CitedBy { get; set; } 

    
    public Makale Next { get; set; } 

    public int CitationCount { get; set; } = 0; 
    public int HIndex { get; set; } = 0;
    public List<Makale> HCore { get; set; }
    public double HMedian { get; set; } = 0;

    public float X { get; set; }
    public float Y { get; set; }

    public Makale()
    {
        Authors = new List<string>();
        ReferencedWorks = new LinkedList<string>();
        CitedBy = new LinkedList<Makale>();
        HCore = new List<Makale>();
    }

    public string YazarBasHarfleri
    {
        get
        {
            if (Authors == null || Authors.Count == 0) return "?";

           
            string ilkYazar = Authors[0];
            string[] parcalar = ilkYazar.Split(' ');
            string kisaAd = "";

            foreach (var p in parcalar)
            {
                if (!string.IsNullOrEmpty(p))
                {
                    kisaAd += p[0] + ".";
                }
            }

            return kisaAd;
        }
    }
}