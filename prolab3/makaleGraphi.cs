using System;
using System.Collections.Generic;
using System.Linq;

namespace prolab3;

public class MakaleGrafi
{
    public Dictionary<string, Makale> Makaleler { get; private set; }
    public List<Makale> MakaleListesi { get; private set; }
    
    // Analiz Sonuçları
    public Dictionary<string, double> BetweennessScores { get; private set; }

    public MakaleGrafi()
    {
        Makaleler = new Dictionary<string, Makale>();
        MakaleListesi = new List<Makale>();
        BetweennessScores = new Dictionary<string, double>();
    }

    public void GrafiOlustur(List<Makale> hamListe)
    {
        Makaleler.Clear();
        MakaleListesi.Clear();

        // 1. Düğümleri Ekle
        foreach (var makale in hamListe)
        {
            if (!string.IsNullOrEmpty(makale.Id) && !Makaleler.ContainsKey(makale.Id))
            {
                Makaleler.Add(makale.Id, makale);
                MakaleListesi.Add(makale);
            }
        }

        // 2. Yönlü Kenarları Kur (Atıflar)
        foreach (var kaynak in MakaleListesi)
        {
            if (kaynak.ReferencedWorks == null) continue;

            foreach (string hedefId in kaynak.ReferencedWorks)
            {
                if (Makaleler.ContainsKey(hedefId))
                {
                    Makale hedef = Makaleler[hedefId];
                    hedef.CitationCount++;
                    hedef.CitedBy.AddLast(kaynak);
                }
            }
        }
        
        // 3. H-Metrikleri Hesapla
        MetrikleriHesapla();
    }

    private void MetrikleriHesapla()
    {
        foreach (var m in MakaleListesi)
        {
            // H-Index için sıralama (Büyükten küçüğe)
            var refs = m.CitedBy.OrderByDescending(x => x.CitationCount).ToList();
            int h = 0;
            for (int i = 0; i < refs.Count; i++)
            {
                if (refs[i].CitationCount >= i + 1) h = i + 1; else break;
            }
            m.HIndex = h;
            m.HCore = refs.Take(h).ToList();
            
            // H-Median
            if (h > 0)
            {
                var scores = m.HCore.Select(x => x.CitationCount).OrderBy(x => x).ToList();
                int mid = scores.Count / 2;
                m.HMedian = (scores.Count % 2 != 0) ? scores[mid] : (scores[mid - 1] + scores[mid]) / 2.0;
            }
            else m.HMedian = 0;
        }
    }

    // --- ANALİZ METODLARI (PDF İsterleri) ---

    // Yardımcı: Bir makalenin YÖNSÜZ (Undirected) komşularını getirir
    // PDF: "Yönlü kenarlar yönsüz kenarlarla değiştirilerek..."
    private List<Makale> GetYonsuzKomsular(Makale m)
    {
        HashSet<Makale> komsular = new HashSet<Makale>();
        
        // Gidenler
        if (m.ReferencedWorks != null)
            foreach (var id in m.ReferencedWorks)
                if (Makaleler.ContainsKey(id)) komsular.Add(Makaleler[id]);
        
        // Gelenler
        foreach (var citing in m.CitedBy)
            komsular.Add(citing);

        return komsular.ToList();
    }

    // Betweenness Centrality (Brandes Algoritması - Unweighted)
    public void CalculateBetweenness()
    {
        BetweennessScores.Clear();
        foreach (var m in MakaleListesi) BetweennessScores[m.Id] = 0;

        foreach (var s in MakaleListesi)
        {
            Stack<Makale> S = new Stack<Makale>();
            Dictionary<string, List<string>> P = new Dictionary<string, List<string>>();
            Dictionary<string, int> sigma = new Dictionary<string, int>();
            Dictionary<string, int> d = new Dictionary<string, int>();

            foreach (var t in MakaleListesi)
            {
                P[t.Id] = new List<string>();
                sigma[t.Id] = 0;
                d[t.Id] = -1;
            }

            sigma[s.Id] = 1;
            d[s.Id] = 0;

            Queue<Makale> Q = new Queue<Makale>();
            Q.Enqueue(s);

            while (Q.Count > 0)
            {
                var v = Q.Dequeue();
                S.Push(v);

                foreach (var w in GetYonsuzKomsular(v))
                {
                    if (d[w.Id] < 0)
                    {
                        Q.Enqueue(w);
                        d[w.Id] = d[v.Id] + 1;
                    }
                    if (d[w.Id] == d[v.Id] + 1)
                    {
                        sigma[w.Id] += sigma[v.Id];
                        P[w.Id].Add(v.Id);
                    }
                }
            }

            Dictionary<string, double> delta = new Dictionary<string, double>();
            foreach (var t in MakaleListesi) delta[t.Id] = 0;

            while (S.Count > 0)
            {
                var w = S.Pop();
                foreach (var vId in P[w.Id])
                {
                    if (sigma[w.Id] > 0)
                        delta[vId] += ((double)sigma[vId] / sigma[w.Id]) * (1 + delta[w.Id]);
                }
                if (w != s)
                {
                    BetweennessScores[w.Id] += delta[w.Id];
                }
            }
        }

        // Yönsüz graf olduğu için 2'ye bölüyoruz
        foreach (var key in BetweennessScores.Keys.ToList())
            BetweennessScores[key] /= 2.0;
    }

    // K-Core Decomposition (Yönsüz Dereceye Göre)
    public HashSet<Makale> GetKCoreList(int k)
    {
        // 1. Dereceleri Hesapla
        Dictionary<string, int> degrees = new Dictionary<string, int>();
        Dictionary<string, bool> removed = new Dictionary<string, bool>();
        
        foreach (var m in MakaleListesi)
        {
            degrees[m.Id] = GetYonsuzKomsular(m).Count;
            removed[m.Id] = false;
        }

        // 2. Yinelemeli Silme
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var m in MakaleListesi)
            {
                if (!removed[m.Id] && degrees[m.Id] < k)
                {
                    removed[m.Id] = true;
                    changed = true;
                    
                    // Komşuların derecesini düşür
                    foreach (var n in GetYonsuzKomsular(m))
                    {
                        if (!removed[n.Id])
                            degrees[n.Id]--;
                    }
                }
            }
        }

        // 3. Kalanları Döndür
        HashSet<Makale> core = new HashSet<Makale>();
        foreach (var m in MakaleListesi)
        {
            if (!removed[m.Id]) core.Add(m);
        }
        return core;
    }

    // Konumlandırma
    public void RastgeleKonumlandir(int w, int h)
    {
        Random r = new Random();
        foreach (var m in MakaleListesi)
        {
            m.X = r.Next(50, w - 50);
            m.Y = r.Next(50, h - 50);
        }
    }
    // ... (Mevcut kodların devamına sınıfın içine ekle) ...

    // YENİ: Sadece verilen listedeki (ekrandaki) makaleler arasında K-Core hesaplar
    public HashSet<Makale> GetKCoreFromSubset(HashSet<Makale> subset, int k)
    {
        // 1. Sadece bu küme içindeki yerel dereceleri hesapla
        Dictionary<string, int> localDegrees = new Dictionary<string, int>();
        Dictionary<string, bool> removed = new Dictionary<string, bool>();

        // Başlangıç: Herkesin derecesini 0 yap
        foreach (var m in subset)
        {
            localDegrees[m.Id] = 0;
            removed[m.Id] = false;
        }

        // Kenarları say (Sadece iki ucu da subset içindeyse)
        foreach (var m in subset)
        {
            // Yönsüz kabul ettiğimiz için hem giden hem gelenlere bakıyoruz
            List<string> baglantilar = new List<string>();
            
            if (m.ReferencedWorks != null) baglantilar.AddRange(m.ReferencedWorks); // Gidenler
            foreach (var citing in m.CitedBy) baglantilar.Add(citing.Id); // Gelenler

            // Tekrarları önle (A->B ve B<-A aynı kenardır)
            baglantilar = baglantilar.Distinct().ToList();

            foreach (var refId in baglantilar)
            {
                // Eğer karşı taraf da ekrandaysa dereceyi artır
                // (Burada her kenarı 2 kez sayacağız ama sorun değil, çünkü her iki düğüm için de işlem yapıyoruz)
                if (localDegrees.ContainsKey(refId))
                {
                    localDegrees[m.Id]++;
                }
            }
        }

        // 2. Yinelemeli Silme (Peeling)
        bool changed = true;
        while (changed)
        {
            changed = false;
            
            // Derecesi K'dan küçük olanları bul ve silinmiş olarak işaretle
            List<Makale> toRemove = new List<Makale>();
            
            foreach (var m in subset)
            {
                if (!removed[m.Id] && localDegrees[m.Id] < k)
                {
                    toRemove.Add(m);
                }
            }

            if (toRemove.Count > 0)
            {
                changed = true;
                foreach (var m in toRemove)
                {
                    removed[m.Id] = true;

                    // Bu düğüm silindiği için, ekrandaki komşularının derecesini düşür
                    List<string> baglantilar = new List<string>();
                    if (m.ReferencedWorks != null) baglantilar.AddRange(m.ReferencedWorks);
                    foreach (var citing in m.CitedBy) baglantilar.Add(citing.Id);
                    
                    foreach (var refId in baglantilar)
                    {
                        if (localDegrees.ContainsKey(refId) && !removed[refId])
                        {
                            localDegrees[refId]--;
                        }
                    }
                }
            }
        }

        // 3. Silinmeyenleri (K-Core Kümesini) Döndür
        HashSet<Makale> coreNodes = new HashSet<Makale>();
        foreach (var m in subset)
        {
            if (!removed[m.Id]) coreNodes.Add(m);
        }
        
        return coreNodes;
    }
}