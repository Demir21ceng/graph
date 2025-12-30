
using System.Net;

using System.Text.RegularExpressions; 


namespace prolab3;

public class VeriIndirici
{
    private HttpClient client;
    private CookieContainer cerezKutusu;
    private string oturumAnahtari = ""; 

    public VeriIndirici()
    {
        cerezKutusu = new CookieContainer();
        HttpClientHandler handler = new HttpClientHandler();
        handler.CookieContainer = cerezKutusu;
        handler.UseCookies = true;
        handler.AllowAutoRedirect = true;

        client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    
    public async Task<bool> GirisYapVeIndir(string kadi, string sifre, string jsonUrl, string dosyaAdi)
    {
        try
        {
            string loginUrl = "https://edestek2.kocaeli.edu.tr/login/index.php";

           
            string loginPageHtml = await client.GetStringAsync(loginUrl);
            string token = TokenBul(loginPageHtml);

            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Giriş sayfası okundu ama 'logintoken' bulunamadı.");
                return false;
            }

            
            var girisVerileri = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", kadi),
                new KeyValuePair<string, string>("password", sifre),
                new KeyValuePair<string, string>("logintoken", token)
            });

            HttpResponseMessage response = await client.PostAsync(loginUrl, girisVerileri);
            string dashboardHtml = await response.Content.ReadAsStringAsync();

            
            if (dashboardHtml.Contains("id=\"loginbtn\"") || dashboardHtml.Contains("action=\"https://edestek2.kocaeli.edu.tr/login/index.php\""))
            {
                MessageBox.Show("Giriş başarısız! Kullanıcı adı veya şifre hatalı.");
                return false;
            }

           
            oturumAnahtari = SessKeyBul(dashboardHtml);

          
            string jsonVerisi = await client.GetStringAsync(jsonUrl);

            
            string klasorYolu = Path.Combine(Application.StartupPath, "Veriler");
            if (!Directory.Exists(klasorYolu)) Directory.CreateDirectory(klasorYolu);

            string tamYol = Path.Combine(klasorYolu, dosyaAdi);
            File.WriteAllText(tamYol, jsonVerisi);

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show("İndirme Hatası: " + ex.Message);
            return false;
        }
    }

    public async Task CikisYap()
    {
        try
        {
            
            if (string.IsNullOrEmpty(oturumAnahtari)) return;

            
            string logoutUrl = $"https://edestek2.kocaeli.edu.tr/login/logout.php?sesskey={oturumAnahtari}";

            
            await client.GetAsync(logoutUrl);
            
            
            cerezKutusu = new CookieContainer(); 
        }
        catch
        {
           
        }
    }

    
    
    private string TokenBul(string html)
    {
        
        var match = Regex.Match(html, "name=\"logintoken\" value=\"([^\"]+)\"");
        return match.Success ? match.Groups[1].Value : "";
    }

    private string SessKeyBul(string html)
    {
       
        var match = Regex.Match(html, "\"sesskey\":\"([^\"]+)\"");
        if (match.Success) return match.Groups[1].Value;

        
        match = Regex.Match(html, "logout\\.php\\?sesskey=([a-zA-Z0-9]+)");
        return match.Success ? match.Groups[1].Value : "";
    }
}