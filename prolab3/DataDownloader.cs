using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions; // Regex için gerekli
using System.Threading.Tasks;
using System.Windows.Forms;

namespace prolab3;

public class VeriIndirici
{
    private HttpClient client;
    private CookieContainer cerezKutusu;
    private string oturumAnahtari = ""; // "sesskey" değerini burada saklayacağız

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

    // 1. GİRİŞ YAPMA VE İNDİRME
    public async Task<bool> GirisYapVeIndir(string kadi, string sifre, string jsonUrl, string dosyaAdi)
    {
        try
        {
            string loginUrl = "https://edestek2.kocaeli.edu.tr/login/index.php";

            // A. Giriş Sayfasını Getir (Token Almak İçin)
            string loginPageHtml = await client.GetStringAsync(loginUrl);
            string token = TokenBul(loginPageHtml);

            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Giriş sayfası okundu ama 'logintoken' bulunamadı.");
                return false;
            }

            // B. Giriş İsteği Gönder (POST)
            var girisVerileri = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", kadi),
                new KeyValuePair<string, string>("password", sifre),
                new KeyValuePair<string, string>("logintoken", token)
            });

            HttpResponseMessage response = await client.PostAsync(loginUrl, girisVerileri);
            string dashboardHtml = await response.Content.ReadAsStringAsync();

            // Giriş başarısızsa "Giriş yap" butonu hala vardır
            if (dashboardHtml.Contains("id=\"loginbtn\"") || dashboardHtml.Contains("action=\"https://edestek2.kocaeli.edu.tr/login/index.php\""))
            {
                MessageBox.Show("Giriş başarısız! Kullanıcı adı veya şifre hatalı.");
                return false;
            }

            // C. Başarılı Giriş Sonrası "sesskey"i bul (Çıkış yapmak için lazım olacak)
            // HTML içinde genelde: "sesskey":"abc123xyz" şeklinde geçer.
            oturumAnahtari = SessKeyBul(dashboardHtml);

            // D. JSON Dosyasını İndir
            string jsonVerisi = await client.GetStringAsync(jsonUrl);

            // Klasöre kaydet
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

    // 2. ÇIKIŞ YAPMA (LOGOUT)
    public async Task CikisYap()
    {
        try
        {
            // Eğer sesskey'i bulamadıysak çıkış linki oluşturamayız
            if (string.IsNullOrEmpty(oturumAnahtari)) return;

            // Moodle çıkış linki formatı:
            string logoutUrl = $"https://edestek2.kocaeli.edu.tr/login/logout.php?sesskey={oturumAnahtari}";

            // Çıkış isteği gönder
            await client.GetAsync(logoutUrl);
            
            // Çerezleri temizle (Garanti olsun)
            cerezKutusu = new CookieContainer(); 
        }
        catch
        {
            // Çıkış sırasında hata olsa bile programın akışını bozmaya gerek yok
        }
    }

    // YARDIMCI METOTLAR (Regex ile String Arama)
    
    private string TokenBul(string html)
    {
        // <input type="hidden" name="logintoken" value="xyz...">
        var match = Regex.Match(html, "name=\"logintoken\" value=\"([^\"]+)\"");
        return match.Success ? match.Groups[1].Value : "";
    }

    private string SessKeyBul(string html)
    {
        // "sesskey":"xyz..." şeklindeki JSON config verisini bulur
        var match = Regex.Match(html, "\"sesskey\":\"([^\"]+)\"");
        if (match.Success) return match.Groups[1].Value;

        // Alternatif: logout.php?sesskey=xyz... linkini bulur
        match = Regex.Match(html, "logout\\.php\\?sesskey=([a-zA-Z0-9]+)");
        return match.Success ? match.Groups[1].Value : "";
    }
}