using System.Drawing.Drawing2D;

namespace prolab3;

public partial class Form1 : Form
{
    // --- UI Kontrolleri ---
    TextBox txtKadi, txtSifre, txtKDegeri;
    Button btnGiris, btnReset, btnBetweenness, btnKCore;
    Label lblDurum, lblSeciliBilgi;
    
    // --- Veri ---
    MakaleGrafi anaGraf;
    HashSet<Makale> gorunurMakaleler = new HashSet<Makale>();
    HashSet<Makale> sonEklenenler = new HashSet<Makale>(); 
    
    // K-CORE İÇİN YENİ YAPILAR
    HashSet<Makale> kCoreDugumler = new HashSet<Makale>(); // K-Core içindeki düğümler
    List<Tuple<Makale, Makale>> kCoreKenarlar = new List<Tuple<Makale, Makale>>(); // Mavi yapılacak çizgiler
    
    Makale seciliMakale = null;
    Makale mouseUzerindekiMakale = null;
    
    // --- Modlar ---
    bool kCoreModuAktif = false; 

    // --- Kamera (Zoom/Pan) ---
    private float zoomFactor = 1.0f;
    private float viewX = 0;
    private float viewY = 0;
    private Point lastMousePos;
    private bool isDragging = false;

    // --- Görsel Ayarlar ---
    private const int PANEL_W = 260;
    private const int DUGUM_R = 30; 
    private Font fntNode = new Font("Arial", 7, FontStyle.Bold);
    private Font fntInfo = new Font("Calibri", 10);
    
    // Kalemler
    private Pen penNormalEdge = new Pen(Color.Gray, 2) { CustomEndCap = new AdjustableArrowCap(5, 5) };
    private Pen penKCoreEdge = new Pen(Color.Blue, 3); // Mavi ve Oksuz (Yönsüz)

    public Form1()
    {
        InitializeComponent();
        ArayuzuKur();
        
        this.DoubleBuffered = true;
        this.ResizeRedraw = true;
        
        this.MouseWheel += Form1_MouseWheel;
        this.MouseDown += Form1_MouseDown;
        this.MouseMove += Form1_MouseMove;
        this.MouseUp += Form1_MouseUp;
        this.MouseClick += Form1_MouseClick;
        
        anaGraf = new MakaleGrafi();
    }

    private void ArayuzuKur()
    {
        this.Size = new Size(1400, 900);
        this.Text = "Prolab 3 - Makale Graf Analizi";
        this.BackColor = Color.White;

        Panel pnl = new Panel() { 
            Dock = DockStyle.Left, 
            Width = 280, 
            BackColor = Color.WhiteSmoke, 
            BorderStyle = BorderStyle.FixedSingle 
        };
        this.Controls.Add(pnl);

        int y = 20; int margin = 10;

        Label lbl1 = new Label() { Text = "Okul No / Kullanıcı Adı:", Location = new Point(margin, y), AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold)};
        pnl.Controls.Add(lbl1);

        txtKadi = new TextBox() { Location = new Point(margin, y + 25), Width = 240, Font = new Font("Arial", 11)};
        pnl.Controls.Add(txtKadi);

        y += 60;
        Label lbl2 = new Label() { Text = "Şifre:", Location = new Point(margin, y), AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold)};
        pnl.Controls.Add(lbl2);

        txtSifre = new TextBox() { Location = new Point(margin, y + 25), Width = 240, PasswordChar = '*', Font = new Font("Arial", 11)};
        pnl.Controls.Add(txtSifre);

        y += 60;
        btnGiris = new Button() { Text = "Giriş Yap ve Verileri İndir", Location = new Point(margin, y), Size = new Size(240, 40), BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Arial", 10, FontStyle.Bold)};
        btnGiris.Click += BtnGiris_Click;
        pnl.Controls.Add(btnGiris);

        y += 50;
        btnReset = new Button() { Text = "🎥 Kamerayı Sıfırla", Location = new Point(margin, y), Size = new Size(240, 30), BackColor = Color.LightGray};
        btnReset.Click += (s, e) => { 
            viewX = 0; viewY = 0; zoomFactor = 1.0f; 
            kCoreModuAktif = false; // Resetleyince modu kapat
            this.Invalidate(); 
        };
        pnl.Controls.Add(btnReset);

        y += 50;
        Label lblAyir = new Label() { Text = "--- ANALİZ İŞLEMLERİ ---", Location = new Point(margin, y), AutoSize = true, Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.DarkRed};
        pnl.Controls.Add(lblAyir);
        
        y += 30;
        btnBetweenness = new Button() { Text = "2. Betweenness Hesapla", Location = new Point(margin, y), Size = new Size(240, 35), BackColor = Color.Bisque };
        btnBetweenness.Click += BtnBetweenness_Click;
        pnl.Controls.Add(btnBetweenness);

        y += 45;
        Label lblK = new Label() { Text = "K Değeri:", Location = new Point(margin, y + 5), AutoSize = true };
        pnl.Controls.Add(lblK);
        
        txtKDegeri = new TextBox() { Text = "", Location = new Point(margin + 70, y), Width = 50 };
        pnl.Controls.Add(txtKDegeri);
        
        btnKCore = new Button() { 
            Text = "3. K-Core (Ekran)", 
            Location = new Point(margin + 130, y - 2), 
            Size = new Size(110, 30), 
            BackColor = Color.Thistle 
        };
        btnKCore.Click += BtnKCore_Click;
        pnl.Controls.Add(btnKCore);

        y += 50;
        lblDurum = new Label() { Text = "Durum: Giriş bekleniyor...", Location = new Point(margin, y), AutoSize = true, ForeColor = Color.Red, MaximumSize = new Size(240, 0)};
        pnl.Controls.Add(lblDurum);

        y += 40;
        lblSeciliBilgi = new Label() { Text = "", Location = new Point(margin, y), AutoSize = true, MaximumSize = new Size(240, 600), Font = new Font("Consolas", 9) };
        pnl.Controls.Add(lblSeciliBilgi);
    }

    // --- VERİ İNDİRME ---
    private async void BtnGiris_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtKadi.Text) || string.IsNullOrEmpty(txtSifre.Text)) { MessageBox.Show("Giriş bilgilerini yazın."); return; }
        
        btnGiris.Enabled = false;
        lblDurum.Text = "İndiriliyor...";
        
        VeriIndirici indirici = new VeriIndirici();
        string url = "https://edestek2.kocaeli.edu.tr/pluginfile.php/111813/mod_resource/content/1/data.json";
        bool sonuc = await indirici.GirisYapVeIndir(txtKadi.Text, txtSifre.Text, url, "okul_verisi.json");

        if (sonuc)
        {
            await indirici.CikisYap();
            VerileriYukle();
            lblDurum.Text = "Veri Hazır.";
            lblDurum.ForeColor = Color.Green;
        }
        else
        {
            lblDurum.Text = "İndirme Hatası!";
        }
        btnGiris.Enabled = true;
    }

    private void VerileriYukle()
    {
        string yol = Path.Combine(Application.StartupPath, "Veriler", "okul_verisi.json");
        if (File.Exists(yol))
        {
            try
            {
                JsonParser parser = new JsonParser();
                var liste = parser.Parse(File.ReadAllText(yol));
                
                anaGraf.GrafiOlustur(liste);
                
                gorunurMakaleler.Clear();
                sonEklenenler.Clear();
                kCoreModuAktif = false;

                var kral = anaGraf.MakaleListesi.OrderByDescending(x => x.CitationCount).FirstOrDefault();
                if (kral != null)
                {
                    kral.X = this.ClientSize.Width / 2.0f;
                    kral.Y = this.ClientSize.Height / 2.0f;
                    gorunurMakaleler.Add(kral);
                    seciliMakale = kral;
                    PanelGuncelle(kral);
                }
                
                viewX = 0; viewY = 0; zoomFactor = 1.0f;
                this.Invalidate();
            }
            catch (Exception ex) { MessageBox.Show("Veri okuma hatası: " + ex.Message); }
        }
    }

    private async void BtnBetweenness_Click(object sender, EventArgs e)
    {
        if (anaGraf.MakaleListesi.Count == 0) return;
        lblDurum.Text = "Analiz yapılıyor...";
        btnBetweenness.Enabled = false;
        await Task.Run(() => anaGraf.CalculateBetweenness());
        lblDurum.Text = "Analiz Bitti.";
        btnBetweenness.Enabled = true;
        if (seciliMakale != null) PanelGuncelle(seciliMakale);
    }

    // --- YENİ K-CORE MANTIĞI ---
    private void BtnKCore_Click(object sender, EventArgs e)
    {
        if (anaGraf == null || gorunurMakaleler.Count == 0)
        {
            MessageBox.Show("Ekranda veri yok.");
            return;
        }

        if (int.TryParse(txtKDegeri.Text, out int k))
        {
            kCoreModuAktif = true; // Modu aç
            KCoreHesaplaVeGuncelle(k); // Hesabı yap
        }
        else
        {
            MessageBox.Show("Geçerli bir K sayısı girin.");
        }
    }

    // İsteğine özel yazılan K-Core Filtreleme Metodu
    private void KCoreHesaplaVeGuncelle(int kValue)
    {
        kCoreDugumler.Clear();
        kCoreKenarlar.Clear();

        // 1. Ekran Sınırlarını (Viewport) Bul
        // Zoom ve Pan değerlerini tersine çevirerek "Dünya Koordinatlarında" ekran karesini buluyoruz.
        float screenLeft = -viewX / zoomFactor;
        float screenTop = -viewY / zoomFactor;
        float screenRight = (this.Width - viewX) / zoomFactor;
        float screenBottom = (this.Height - viewY) / zoomFactor;

        // 2. Sadece ekranda görünen düğümleri tespit et
        var ekrandakiDugumler = gorunurMakaleler
            .Where(m => m.X >= screenLeft && m.X <= screenRight && 
                        m.Y >= screenTop && m.Y <= screenBottom)
            .ToList();

        var ekrandakiIdler = new HashSet<string>(ekrandakiDugumler.Select(m => m.Id));

        // 3. Sadece ekrandaki düğümler arasındaki kenarları bul
        // Geçici bir "Edge" listesi oluşturuyoruz
        var visibleEdges = new List<Tuple<string, string>>();
        
        foreach (var m in ekrandakiDugumler)
        {
            if (m.ReferencedWorks == null) continue;
            foreach (var refId in m.ReferencedWorks)
            {
                // Eğer hedef düğüm de ekrandaysa ve "gorunurMakaleler" içindeyse
                if (ekrandakiIdler.Contains(refId))
                {
                    // Yönsüz grafik mantığı: A->B ve B->A aynı sayılır derecede
                    visibleEdges.Add(new Tuple<string, string>(m.Id, refId));
                }
            }
        }

        // 4. Dereceleri Hesapla
        var degrees = new Dictionary<string, int>();
        foreach(var m in ekrandakiDugumler) degrees[m.Id] = 0;

        foreach(var edge in visibleEdges)
        {
            degrees[edge.Item1]++;
            degrees[edge.Item2]++;
        }

        // 5. Peeling Algoritması (K'dan küçükleri çıkar)
        var activeNodes = new HashSet<string>(ekrandakiIdler);
        bool changed = true;

        while(changed)
        {
            changed = false;
            var toRemove = activeNodes.Where(id => degrees[id] < kValue).ToList();

            if (toRemove.Count > 0)
            {
                changed = true;
                foreach(var remId in toRemove)
                {
                    activeNodes.Remove(remId);
                    
                    // Bu düğüme bağlı kenarları bulup komşuların derecesini düşür
                    var connected = visibleEdges.Where(e => (e.Item1 == remId || e.Item2 == remId));
                    foreach(var edge in connected)
                    {
                        string neighbor = (edge.Item1 == remId) ? edge.Item2 : edge.Item1;
                        if (activeNodes.Contains(neighbor))
                        {
                            degrees[neighbor]--;
                        }
                    }
                }
            }
        }

        // 6. Sonuçları Kaydet (Çizim için)
        foreach(var id in activeNodes)
        {
            // ID'den Makale nesnesini bul
            var m = ekrandakiDugumler.FirstOrDefault(x => x.Id == id);
            if(m != null) kCoreDugumler.Add(m);
        }

        // Sadece hayatta kalan düğümler arasındaki kenarları listeye ekle (Mavi çizmek için)
        foreach(var edge in visibleEdges)
        {
            if (activeNodes.Contains(edge.Item1) && activeNodes.Contains(edge.Item2))
            {
                var m1 = ekrandakiDugumler.FirstOrDefault(x => x.Id == edge.Item1);
                var m2 = ekrandakiDugumler.FirstOrDefault(x => x.Id == edge.Item2);
                if (m1 != null && m2 != null)
                {
                    kCoreKenarlar.Add(new Tuple<Makale, Makale>(m1, m2));
                }
            }
        }

        lblDurum.Text = $"K-Core: Ekrandaki {ekrandakiDugumler.Count} düğümden {kCoreDugumler.Count} tanesi kaldı.";
        this.Invalidate(); // Çizimi tetikle
    }


    // --- ÇİZİM VE ETKİLEŞİM ---
    private void PanelGuncelle(Makale m)
    {
        double bet = anaGraf.BetweennessScores.ContainsKey(m.Id) ? anaGraf.BetweennessScores[m.Id] : 0;
        lblSeciliBilgi.Text = $"ID: {m.Id.Replace("https://openalex.org/W", "")}\nBaşlık: {m.Title}\nYıl: {m.Year}\nAtıf: {m.CitationCount}\nH-Index: {m.HIndex}\nBetweenness: {bet:F2}";
    }

    private void Form1_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left && !isDragging && mouseUzerindekiMakale != null)
        {
            if (kCoreModuAktif)
            {
                seciliMakale = mouseUzerindekiMakale;
                PanelGuncelle(seciliMakale);
                return;
            }
            Genislet(mouseUzerindekiMakale);
        }
    }

    private void Genislet(Makale merkez)
{
    seciliMakale = merkez;
    sonEklenenler.Clear(); 
    PanelGuncelle(merkez);

    if (merkez.HCore == null) return;

    // --- SPİRAL YERLEŞİM AYARLARI ---
    float angle = 0;            // Başlangıç açısı
    float radius = 100;         // Başlangıç yarıçapı (Merkezden uzaklık)
    float angleStep = 0.6f;     // Her denemede açının ne kadar değişeceği (Radyan)
    float radiusStep = 2.0f;    // Her turda yarıçapın ne kadar büyüyeceği

    foreach (var core in merkez.HCore)
    {
        // Eğer zaten ekranda yoksa ekle
        if (!gorunurMakaleler.Contains(core))
        {
            // Boş yer bulana kadar döngüye gir
            int maxDeneme = 2000; // Sonsuz döngüyü engellemek için güvenlik limiti
            int deneme = 0;
            bool yerBulundu = false;

            while (!yerBulundu && deneme < maxDeneme)
            {
                // Polar koordinattan kartezyene geçiş (x = r * cos(a), y = r * sin(a))
                float adayX = merkez.X + (float)(Math.Cos(angle) * radius);
                float adayY = merkez.Y + (float)(Math.Sin(angle) * radius);

                // Bu aday konumda başka bir düğüm var mı?
                if (!CakisiyorMu(adayX, adayY))
                {
                    // Yer boş! Düğümü buraya koy.
                    core.X = adayX;
                    core.Y = adayY;
                    yerBulundu = true;
                }
                else
                {
                    // Yer dolu! Biraz döndür ve biraz uzaklaş (Spiral hareket)
                    angle += angleStep;
                    radius += radiusStep;
                    deneme++;
                }
            }

            gorunurMakaleler.Add(core);
            sonEklenenler.Add(core); 
        }
    }
    this.Invalidate(); // Ekranı yenile
}

    // Verilen X,Y koordinatında (yarıçap payı dahil) başka bir düğüm var mı?
    private bool CakisiyorMu(float x, float y)
    {
        // Düğümlerin yarıçapı DUGUM_R (30). İki düğümün merkezi arasındaki mesafe
        // en az (30 + 30) = 60 olmalı ki değmesinler.
        // Biraz da boşluk (padding) bırakalım: 80 diyelim.
        float guvenliMesafe = (DUGUM_R * 2) + 20; 

        foreach (var other in gorunurMakaleler)
        {
            float dx = x - other.X;
            float dy = y - other.Y;
        
            // Pisagor: a^2 + b^2 = c^2
            float mesafeKaresi = (dx * dx) + (dy * dy);

            // Karekök almak işlemciyi yorar, o yüzden mesafenin karesiyle kıyaslıyoruz
            if (mesafeKaresi < (guvenliMesafe * guvenliMesafe))
            {
                return true; // Çakışma var!
            }
        }
        return false; // Temiz
    }

    // --- KAMERA ---
    private void Form1_MouseWheel(object sender, MouseEventArgs e)
    {
        float oldZoom = zoomFactor;
        if (e.Delta > 0) zoomFactor *= 1.1f; else zoomFactor /= 1.1f;
        zoomFactor = Math.Max(0.1f, Math.Min(zoomFactor, 5.0f));

        float focusX = (e.X < PANEL_W) ? (this.Width + PANEL_W)/2 : e.X;
        float focusY = (e.X < PANEL_W) ? this.Height/2 : e.Y;
        float worldX = (focusX - viewX) / oldZoom;
        float worldY = (focusY - viewY) / oldZoom;
        viewX = focusX - (worldX * zoomFactor);
        viewY = focusY - (worldY * zoomFactor);
        
        // Eğer K-Core modu açıksa ve zoom yaptıkça ekran değiştiyse, hesabı güncelle
        if (kCoreModuAktif && int.TryParse(txtKDegeri.Text, out int k))
        {
            KCoreHesaplaVeGuncelle(k);
        }
        
        this.Invalidate();
    }

    private void Form1_MouseDown(object sender, MouseEventArgs e) { 
        if (e.Button == MouseButtons.Right) { isDragging = true; lastMousePos = e.Location; Cursor = Cursors.SizeAll; } 
    }
    
    private void Form1_MouseUp(object sender, MouseEventArgs e) { 
        isDragging = false; Cursor = Cursors.Default; 
        
        // Sürükleme bittiğinde ekran değiştiği için K-Core'u güncelle
        if (kCoreModuAktif && int.TryParse(txtKDegeri.Text, out int k))
        {
            KCoreHesaplaVeGuncelle(k);
        }
    }
    
    private void Form1_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging) 
        { 
            viewX += e.X - lastMousePos.X; viewY += e.Y - lastMousePos.Y; 
            lastMousePos = e.Location; 
            this.Invalidate(); 
            return; 
        }

        float wx = (e.X - viewX) / zoomFactor;
        float wy = (e.Y - viewY) / zoomFactor;
        
        Makale hit = null;
        foreach (var m in gorunurMakaleler) 
        {
            if (Math.Pow(wx - m.X, 2) + Math.Pow(wy - m.Y, 2) < Math.Pow(DUGUM_R, 2)) 
                hit = m;
        }

        if (hit != mouseUzerindekiMakale)
        {
            mouseUzerindekiMakale = hit;
            Cursor = (hit != null) ? Cursors.Hand : Cursors.Default;
            this.Invalidate();
        }
    }

    // --- ÇİZİM (ONPAINT) REVİZESİ ---
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (anaGraf == null) return;

        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        
        g.TranslateTransform(viewX, viewY);
        g.ScaleTransform(zoomFactor, zoomFactor);

        // 1. KENARLAR (EDGES)
        if (kCoreModuAktif)
        {
            // --- K-CORE MODU ---
            // Sadece hesaplanmış K-Core kenarlarını Mavi ve Yönsüz çiz
            foreach (var edge in kCoreKenarlar)
            {
                g.DrawLine(penKCoreEdge, edge.Item1.X, edge.Item1.Y, edge.Item2.X, edge.Item2.Y);
            }
            // K-Core'a uymayanlar hİÇ ÇİZİLMİYOR.
        }
        else
        {
            // --- NORMAL MOD ---
            // Tüm görünür makalelerin bağlarını Ok şeklinde çiz
            foreach (var m in gorunurMakaleler)
            {
                if (m.ReferencedWorks != null)
                    foreach (string refId in m.ReferencedWorks)
                        if (anaGraf.Makaleler.TryGetValue(refId, out Makale hedef) && gorunurMakaleler.Contains(hedef))
                        {
                            float dx = hedef.X - m.X;
                            float dy = hedef.Y - m.Y;
                            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                            if (dist > 0)
                            {
                                float endX = hedef.X - (dx / dist) * DUGUM_R;
                                float endY = hedef.Y - (dy / dist) * DUGUM_R;
                                g.DrawLine(penNormalEdge, m.X, m.Y, endX, endY);
                            }
                        }
            }
        }

        // 2. DÜĞÜMLER (NODES)
        // Renk değişimi istenmediği için standart renkleri kullanıyoruz.
            // --- ONPAINT İÇİNDEKİ DÜĞÜM ÇİZİM DÖNGÜSÜ ---

foreach (var m in gorunurMakaleler)
{
    // 1. Daireyi Çiz
    Brush firca = Brushes.LightSteelBlue; 
    if (m == seciliMakale) firca = Brushes.Red;
    else if (sonEklenenler.Contains(m)) firca = Brushes.LightGreen;
    if (m == mouseUzerindekiMakale) firca = Brushes.Orange;

    g.FillEllipse(firca, m.X - DUGUM_R, m.Y - DUGUM_R, DUGUM_R * 2, DUGUM_R * 2);
    g.DrawEllipse(Pens.Black, m.X - DUGUM_R, m.Y - DUGUM_R, DUGUM_R * 2, DUGUM_R * 2);

    // --- METİN HAZIRLAMA ---
    string cleanId = m.Id.Replace("https://openalex.org/", "");

    string initials = "";
    if (m.Authors != null && m.Authors.Count > 0)
    {
        string firstAuthor = m.Authors[0];
        string[] parts = firstAuthor.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
            if (part.Length > 0) initials += char.ToUpper(part[0]) + ".";
    }
    if (string.IsNullOrEmpty(initials)) initials = "-";

    // --- DİNAMİK YAZI ÇİZİMİ ---
    
    // Format Ayarları: Tam Ortala
    StringFormat sf = new StringFormat();
    sf.Alignment = StringAlignment.Center;
    sf.LineAlignment = StringAlignment.Center;

    // 1. ADIM: Baş Harfleri Yaz (Üstte, Standart Boyut)
    // Baş harfler kısa olduğu için genelde sığar, fontu sabit tutabiliriz (Arial 8 Bold)
    using (Font fntInitials = new Font("Arial", 8, FontStyle.Bold))
    {
        // Dairenin üst yarısına hizala
        RectangleF rectTop = new RectangleF(m.X - DUGUM_R, m.Y - DUGUM_R + 10, DUGUM_R * 2, DUGUM_R);
        g.DrawString(initials, fntInitials, Brushes.Black, rectTop, sf);
    }

    // 2. ADIM: ID'yi Yaz (Altta, Sığdırana Kadar Küçült)
    // ID için maksimum genişlik: Dairenin çapı eksi 4 piksel kenar payı
    float maxWidth = (DUGUM_R * 2) - 4; 
    float fontSize = 7.5f; // Başlangıç font büyüklüğü

    // Sığana kadar fontu küçültme döngüsü
    Font fntId = null;
    while (true)
    {
        fntId = new Font("Arial Narrow", fontSize, FontStyle.Regular);
        SizeF size = g.MeasureString(cleanId, fntId);

        // Eğer genişlik sığıyorsa VEYA font çok küçüldüyse (okunmaz hale gelmesin diye sınır 5f) döngüyü kır
        if (size.Width < maxWidth || fontSize <= 4.0f)
        {
            break; 
        }
        
        // Sığmadıysa fontu biraz küçült ve tekrar dene
        fntId.Dispose(); // Eski fontu bellekten at
        fontSize -= 0.5f; 
    }

    // Hesaplanan en uygun fontla ID'yi yaz
    // Dairenin alt yarısına hizala
    RectangleF rectBottom = new RectangleF(m.X - DUGUM_R, m.Y - 5, DUGUM_R * 2, DUGUM_R);
    g.DrawString(cleanId, fntId, Brushes.Black, rectBottom, sf);
    
    // Fontu temizle
    fntId.Dispose();
}

        // --- ONPAINT METODUNUN EN ALT KISMI ---

g.ResetTransform(); // Koordinat sistemini ekrana sabitle

if (mouseUzerindekiMakale != null)
{
    // 1. GÖSTERİLECEK METİNLERİ HAZIRLA
    // Başlık
    string title = mouseUzerindekiMakale.Title ?? "Başlık Yok";
    
    // Yazarlar (Liste ise virgülle ayır, string ise direkt al)
    // Not: Makale sınıfında Authors listen varsa string.Join kullan, yoksa direkt string değişkenini yaz.
    string authors = (mouseUzerindekiMakale.Authors != null && mouseUzerindekiMakale.Authors.Count > 0)
                     ? "Yazarlar: " + string.Join(", ", mouseUzerindekiMakale.Authors)
                     : "Yazar bilgisi yok";

    // Yıl
    string year = "Yıl: " + mouseUzerindekiMakale.Year.ToString();

    // 2. BOYUT HESAPLAMA (MEASURING)
    int kartGenisligi = 300; // Kartın maksimum genişliği
    int padding = 10; // Kenar boşluğu

    // Başlığı ölç (300px genişliğe sığacak şekilde yüksekliğini hesaplar)
    SizeF titleSize = g.MeasureString(title, fntInfo, kartGenisligi);
    
    // Diğerlerini ölç
    SizeF authorSize = g.MeasureString(authors, fntInfo, kartGenisligi);
    SizeF yearSize = g.MeasureString(year, fntInfo, kartGenisligi);

    // Toplam kutu yüksekliğini bul
    float totalHeight = padding + titleSize.Height + 5 + authorSize.Height + 5 + yearSize.Height + padding;
    
    // Toplam kutu genişliğini bul (En geniş yazı hangisiyse ona göre veya maks 300)
    float totalWidth = Math.Max(titleSize.Width, Math.Max(authorSize.Width, yearSize.Width)) + (padding * 2);

    // 3. POZİSYON AYARLAMA (EKRANDAN TAŞMAMASI İÇİN)
    Point mousePos = this.PointToClient(Cursor.Position);
    float x = mousePos.X + 20;
    float y = mousePos.Y + 20;

    // Sağa taşıyorsa sola al
    if (x + totalWidth > this.ClientSize.Width) 
        x = mousePos.X - totalWidth - 10;
    
    // Aşağı taşıyorsa yukarı al
    if (y + totalHeight > this.ClientSize.Height) 
        y = mousePos.Y - totalHeight - 10;

    // 4. KUTUYU ÇİZ (Arkaplan ve Çerçeve)
    // Hafif gölgeli şık bir görünüm için istersen önce gri bir kutu, sonra beyaz kutu çizebilirsin ama şimdilik sade tutalım.
    RectangleF rectBackground = new RectangleF(x, y, totalWidth, totalHeight);
    
    g.FillRectangle(Brushes.WhiteSmoke, rectBackground); // Hafif gri/beyaz zemin
    g.DrawRectangle(Pens.Black, x, y, totalWidth, totalHeight); // Siyah çerçeve

    // 5. YAZILARI ÇİZ (Satır satır aşağı inerek)
    float currentY = y + padding;
    float textX = x + padding;

    // Başlığı çiz (Dikdörtgen içine wrap yaparak)
    g.DrawString(title, fntInfo, Brushes.Black, new RectangleF(textX, currentY, kartGenisligi, titleSize.Height));
    currentY += titleSize.Height + 5; // Bir sonraki satır için aşağı kay

    // Yazarları çiz
    g.DrawString(authors, fntInfo, Brushes.DarkSlateGray, new RectangleF(textX, currentY, kartGenisligi, authorSize.Height));
    currentY += authorSize.Height + 5;

    // Yılı çiz
    g.DrawString(year, fntInfo, Brushes.DarkRed, new RectangleF(textX, currentY, kartGenisligi, yearSize.Height));
}
    }
}