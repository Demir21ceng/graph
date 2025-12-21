using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace prolab3;

public class JsonParser
{
    public List<Makale> Parse(string jsonText)
    {
        var makaleler = new List<Makale>();

        jsonText = jsonText.Trim();
        if (jsonText.StartsWith("[")) jsonText = jsonText.Substring(1);
        if (jsonText.EndsWith("]")) jsonText = jsonText.Substring(0, jsonText.Length - 1);

        List<string> nesneBloklari = NesneleriAyir(jsonText);

        foreach (var blok in nesneBloklari)
        {
            Makale m = TekMakaleParse(blok);
            if (m != null && !string.IsNullOrEmpty(m.Id))
            {
                makaleler.Add(m);
            }
        }
        return makaleler;
    }

    private List<string> NesneleriAyir(string json)
    {
        List<string> list = new List<string>();
        int parantez = 0;
        int baslangic = 0;
        bool tirnak = false;

        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            if (c == '"' && (i == 0 || json[i - 1] != '\\')) tirnak = !tirnak;

            if (!tirnak)
            {
                if (c == '{')
                {
                    if (parantez == 0) baslangic = i;
                    parantez++;
                }
                else if (c == '}')
                {
                    parantez--;
                    if (parantez == 0) list.Add(json.Substring(baslangic, i - baslangic + 1));
                }
            }
        }
        return list;
    }

    private Makale TekMakaleParse(string json)
    {
        Makale m = new Makale();
        m.Id = DegerBul(json, "\"id\":");
        m.Title = DegerBul(json, "\"title\":");
        m.Venue = DegerBul(json, "\"venue\":");
        m.Doi = DegerBul(json, "\"doi\":");
        
        string yearStr = DegerBul(json, "\"year\":", false);
        if (int.TryParse(yearStr, out int y)) m.Year = y;

        // Yazarlar (List)
        List<string> authorsList = ListeBul(json, "\"authors\":");
        m.Authors = authorsList;

        // Referanslar (LinkedList'e çeviriyoruz)
        List<string> refList = ListeBul(json, "\"referenced_works\":");
        foreach(var r in refList)
        {
            m.ReferencedWorks.AddLast(r); // Sona ekleme
        }

        return m;
    }

    private string DegerBul(string json, string key, bool isString = true)
    {
        int idx = json.IndexOf(key);
        if (idx == -1) return "";
        int start = idx + key.Length;
        
        if (isString)
        {
            int s = json.IndexOf("\"", start);
            if (s == -1) return "";
            s++;
            int e = json.IndexOf("\"", s);
            return Regex.Unescape(json.Substring(s, e - s));
        }
        else
        {
            int s = start;
            while (s < json.Length && !char.IsDigit(json[s])) s++;
            int e = s;
            while (e < json.Length && char.IsDigit(json[e])) e++;
            return s < json.Length ? json.Substring(s, e - s) : "";
        }
    }

    // Helper olarak List dönüyor, yukarıda LinkedList'e çeviriyoruz
    private List<string> ListeBul(string json, string key)
    {
        var list = new List<string>();
        int idx = json.IndexOf(key);
        if (idx == -1) return list;

        int open = json.IndexOf("[", idx);
        int close = json.IndexOf("]", open);
        if (open == -1 || close == -1) return list;

        string content = json.Substring(open + 1, close - open - 1);
        var matches = Regex.Matches(content, "\"(.*?)\"");
        foreach (Match m in matches) list.Add(m.Groups[1].Value);
        
        return list;
    }
}