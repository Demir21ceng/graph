using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;


namespace prolab3
{
    public partial class Form1 : Form
    {
        // --- UI Kontrolleri ---
        TextBox txtKadi, txtSifre, txtKDegeri, txtAramaId;
        Button btnGiris, btnReset, btnBetweenness, btnKCore, btnAra;
        Label lblDurum, lblSeciliBilgi, lblDetayliIstatistik;

        // --- Veri ---
        MakaleGrafi anaGraf;
        HashSet<Makale> gorunurMakaleler = new HashSet<Makale>();
        HashSet<Makale> sonEklenenler = new HashSet<Makale>();

        // K-Core Verileri
        HashSet<Makale> kCoreDugumler = new HashSet<Makale>();

        Makale seciliMakale = null;
        Makale mouseUzerindekiMakale = null;

        // --- Modlar ve Hafıza ---
        bool kCoreModuAktif = false;

        // Zoom/Pan Hafızası
        private List<Makale> hafizadakiMakaleler = new List<Makale>();
        private Dictionary<string, PointF> hafizadakiKonumlar = new Dictionary<string, PointF>();
        private float hafizaZoom = 1.0f;
        private float hafizaViewX = 0;
        private float hafizaViewY = 0;
        private bool tumunuGosterModu = false;

        // --- Kamera (Zoom/Pan) ---
        private float zoomFactor = 1.0f;
        private float viewX = 0;
        private float viewY = 0;
        private Point lastMousePos;
        private bool isDragging = false;

        // --- Görsel Ayarlar ---
        private const int PANEL_W = 260;
        private const int DUGUM_R = 35;
        private Font fntNode = new Font("Arial", 7, FontStyle.Bold);
        private Font fntInfo = new Font("Calibri", 10);
        private Font fntTitle = new Font("Calibri", 11, FontStyle.Bold);

        // --- KALEM TANIMLARI (GÜNCELLENDİ) ---

        // 1. Normal Mod (Oklar Var)
        private Pen penNormalArrow = new Pen(Color.Gray, 2) { CustomEndCap = new AdjustableArrowCap(5, 5) };

        // 2. K-Core Modu (Oklar Yok, Sadece Çizgi)
        private Pen penNormalLine = new Pen(Color.Gray, 2); // Oksuz Gri
        private Pen penKCoreLine = new Pen(Color.Blue, 4); // Oksuz Mavi (Kalın)

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

            Panel pnl = new Panel()
            {
                Dock = DockStyle.Left, Width = 280, BackColor = Color.WhiteSmoke, BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(pnl);

            int y = 20;
            int margin = 10;

            // Giriş
            pnl.Controls.Add(new Label()
            {
                Text = "Okul No / Kullanıcı Adı:", Location = new Point(margin, y), AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold)
            });
            txtKadi = new TextBox() { Location = new Point(margin, y + 25), Width = 240, Font = new Font("Arial", 11) };
            pnl.Controls.Add(txtKadi);

            y += 60;
            pnl.Controls.Add(new Label()
            {
                Text = "Şifre:", Location = new Point(margin, y), AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold)
            });
            txtSifre = new TextBox()
                { Location = new Point(margin, y + 25), Width = 240, PasswordChar = '*', Font = new Font("Arial", 11) };
            pnl.Controls.Add(txtSifre);

            y += 60;
            btnGiris = new Button()
            {
                Text = "Giriş Yap ve Verileri İndir", Location = new Point(margin, y), Size = new Size(240, 40),
                BackColor = Color.SteelBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 10, FontStyle.Bold)
            };
            btnGiris.Click += BtnGiris_Click;
            pnl.Controls.Add(btnGiris);

            y += 50;
            btnReset = new Button()
            {
                Text = "🎥 Kamerayı Sıfırla", Location = new Point(margin, y), Size = new Size(240, 30),
                BackColor = Color.LightGray
            };
            btnReset.Click += (s, e) =>
            {
                viewX = 0;
                viewY = 0;
                zoomFactor = 1.0f;
                kCoreModuAktif = false;
                this.Invalidate();
            };
            pnl.Controls.Add(btnReset);

            y += 40;
            Button btnTumunuGoster = new Button()
            {
                Text = "🔍 Tümünü Ekrana Sığdır", Location = new Point(margin, y), Size = new Size(240, 35),
                BackColor = Color.LightSeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            btnTumunuGoster.Click += BtnTumunuGoster_Click;
            pnl.Controls.Add(btnTumunuGoster);

            y += 45;
            lblDurum = new Label()
            {
                Text = "Durum: Giriş bekleniyor...",
                Location = new Point(margin, y),
                AutoSize = true,
                ForeColor = Color.Green,
                Font = new Font("Arial", 9, FontStyle.Bold),
                MaximumSize = new Size(240, 0)
            };
            pnl.Controls.Add(lblDurum);

            y += 30;
            pnl.Controls.Add(new Label()
            {
                Text = "Makale ID Ara:", Location = new Point(margin, y), AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.DarkBlue
            });
            txtAramaId = new TextBox()
                { Location = new Point(margin, y + 25), Width = 180, Font = new Font("Arial", 10) };
            pnl.Controls.Add(txtAramaId);

            btnAra = new Button()
            {
                Text = "🔍", Location = new Point(margin + 185, y + 23), Size = new Size(55, 26),
                BackColor = Color.CornflowerBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat
            };
            btnAra.Click += BtnAra_Click;
            pnl.Controls.Add(btnAra);

            y += 55;
            pnl.Controls.Add(new Label()
            {
                Text = "--- ANALİZ İŞLEMLERİ ---", Location = new Point(margin, y), AutoSize = true,
                Font = new Font("Arial", 10, FontStyle.Bold), ForeColor = Color.DarkRed
            });

            y += 30;
            btnBetweenness = new Button()
            {
                Text = "2. Betweenness Hesapla", Location = new Point(margin, y), Size = new Size(240, 35),
                BackColor = Color.Bisque
            };
            btnBetweenness.Click += BtnBetweenness_Click;
            pnl.Controls.Add(btnBetweenness);

            y += 45;
            pnl.Controls.Add(new Label() { Text = "K Değeri:", Location = new Point(margin, y + 5), AutoSize = true });
            txtKDegeri = new TextBox() { Text = "2", Location = new Point(margin + 70, y), Width = 50 };
            pnl.Controls.Add(txtKDegeri);

            btnKCore = new Button()
            {
                Text = "3. K-Core (Highlight)", Location = new Point(margin + 130, y - 2), Size = new Size(110, 30),
                BackColor = Color.Thistle
            };
            btnKCore.Click += BtnKCore_Click;
            pnl.Controls.Add(btnKCore);

            y += 40;
            CheckBox chkYesil = new CheckBox()
            {
                Text = "ID Bağlarını Göster (Yeşil)", Location = new Point(margin, y), AutoSize = true, Checked = false
            };
            chkYesil.CheckedChanged += (s, e) => { this.Invalidate(); };
            pnl.Controls.Add(chkYesil);

            y += 30;
            pnl.Controls.Add(new Label()
            {
                Text = "📊 GRAF İSTATİSTİKLERİ", Location = new Point(margin, y), AutoSize = true,
                Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = Color.DarkRed
            });
            lblDetayliIstatistik = new Label()
            {
                Text = "...", Location = new Point(margin, y + 25), AutoSize = true, Font = new Font("Calibri", 9),
                MaximumSize = new Size(260, 0)
            };
            pnl.Controls.Add(lblDetayliIstatistik);

            y += 200;
            GroupBox grpBilgi = new GroupBox()
            {
                Text = "Seçili Makale Detayı", Location = new Point(margin, y), Size = new Size(250, 300),
                Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = Color.DarkSlateGray,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };
            pnl.Controls.Add(grpBilgi);

            lblSeciliBilgi = new Label()
            {
                Text = "Henüz bir seçim yapılmadı.", Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9, FontStyle.Regular), ForeColor = Color.Black, Padding = new Padding(5)
            };
            grpBilgi.Controls.Add(lblSeciliBilgi);
        }

        private async void BtnGiris_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtKadi.Text) || string.IsNullOrEmpty(txtSifre.Text))
            {
                MessageBox.Show("Giriş bilgilerini yazın.");
                return;
            }

            btnGiris.Enabled = false;
            lblDurum.Text = "İndiriliyor...";

            VeriIndirici indirici = new VeriIndirici();
            string url = "https://edestek2.kocaeli.edu.tr/pluginfile.php/111813/mod_resource/content/1/data.json";
            bool sonuc = await indirici.GirisYapVeIndir(txtKadi.Text, txtSifre.Text, url, "okul_verisi.json");

            if (sonuc)
            {
                await indirici.CikisYap();
                VerileriYukle();
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
                    seciliMakale = null;
                    viewX = 0;
                    viewY = 0;
                    zoomFactor = 1.0f;

                    lblDurum.Text = "Veri Yüklendi. ID arayarak başlayın.";
                    lblDurum.ForeColor = Color.Green;
                    this.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Hata: " + ex.Message);
                }
            }
        }

        private void BtnAra_Click(object sender, EventArgs e)
        {
            if (anaGraf == null || anaGraf.MakaleListesi.Count == 0)
            {
                MessageBox.Show("Önce veri indirin.");
                return;
            }

            string aranan = txtAramaId.Text.Trim();
            if (string.IsNullOrEmpty(aranan)) return;

            Makale bulunan = anaGraf.MakaleListesi.FirstOrDefault(m => m.Id.EndsWith(aranan) || m.Id == aranan);
            if (bulunan != null)
            {
                gorunurMakaleler.Clear();
                sonEklenenler.Clear();
                kCoreModuAktif = false;

                bulunan.X = this.ClientSize.Width / 2.0f;
                bulunan.Y = this.ClientSize.Height / 2.0f;
                gorunurMakaleler.Add(bulunan);
                seciliMakale = bulunan;

                PanelGuncelle(bulunan);
                IstatistikleriGuncelle();

                viewX = 0;
                viewY = 0;
                zoomFactor = 1.0f;
                lblDurum.Text = $"Bulundu: {aranan}";
                this.Invalidate();
            }
            else MessageBox.Show("Bulunamadı.");
        }

        private async void BtnBetweenness_Click(object sender, EventArgs e)
        {
            if (anaGraf.MakaleListesi.Count == 0) return;
            lblDurum.Text = "Analiz yapılıyor...";
            btnBetweenness.Enabled = false;
            await Task.Run(() => anaGraf.CalculateBetweenness());
            lblDurum.Text = "Betweenness Hazır.";
            btnBetweenness.Enabled = true;
            if (seciliMakale != null) PanelGuncelle(seciliMakale);
        }

        private void BtnKCore_Click(object sender, EventArgs e)
        {
            if (anaGraf == null || gorunurMakaleler.Count == 0)
            {
                MessageBox.Show("Ekranda veri yok.");
                return;
            }

            if (int.TryParse(txtKDegeri.Text, out int k))
            {
                kCoreModuAktif = true;
                KCoreHesaplaVeGuncelle(k);
            }
        }

        private void KCoreHesaplaVeGuncelle(int kValue)
        {
            kCoreDugumler.Clear();

            var active = new HashSet<string>(gorunurMakaleler.Select(m => m.Id));

            // Yönsüz K-Core Hesabı için görünür kenarları topla
            var visibleEdges = new List<Tuple<string, string>>();
            foreach (var m in gorunurMakaleler)
            {
                if (m.ReferencedWorks != null)
                    foreach (var r in m.ReferencedWorks)
                        if (active.Contains(r))
                            visibleEdges.Add(new Tuple<string, string>(m.Id, r));
            }

            // Dereceleri hesapla (Giren+Çıkan değil, toplam bağlantı gibi davranılır genelde yönsüzde)
            // Ancak bu algoritma için basit degree hesabı:
            var degrees = new Dictionary<string, int>();
            foreach (var m in gorunurMakaleler) degrees[m.Id] = 0;
            foreach (var ed in visibleEdges)
            {
                degrees[ed.Item1]++;
                degrees[ed.Item2]++;
            }

            bool changed = true;
            while (changed)
            {
                changed = false;
                var toRem = active.Where(id => degrees[id] < kValue).ToList();
                if (toRem.Count > 0)
                {
                    changed = true;
                    foreach (var rem in toRem)
                    {
                        active.Remove(rem);
                        var conn = visibleEdges.Where(ed => ed.Item1 == rem || ed.Item2 == rem);
                        foreach (var ed in conn)
                        {
                            string n = (ed.Item1 == rem) ? ed.Item2 : ed.Item1;
                            if (active.Contains(n)) degrees[n]--;
                        }
                    }
                }
            }

            foreach (var id in active) kCoreDugumler.Add(gorunurMakaleler.First(x => x.Id == id));

            lblDurum.Text = $"K-Core: {gorunurMakaleler.Count} düğümden {kCoreDugumler.Count} tanesi Core.";
            this.Invalidate();
        }

        private void PanelGuncelle(Makale m)
        {
            double bet = anaGraf.BetweennessScores.ContainsKey(m.Id) ? anaGraf.BetweennessScores[m.Id] : 0;
            int hMedian = HesaplaHMedian(m);

            lblSeciliBilgi.Text = $"ID: {m.Id.Replace("https://openalex.org/W", "")}\n" +
                                  $"Başlık: {m.Title}\n" +
                                  $"Yıl: {m.Year}\n" +
                                  $"Atıf (In-Degree): {m.CitationCount}\n" +
                                  $"H-Index: {m.HIndex}\n" +
                                  $"H-Median: {hMedian}\n" +
                                  $"Betweenness: {bet:F2}";
        }

        private int HesaplaHMedian(Makale m)
        {
            if (m.HCore == null || m.HCore.Count == 0) return 0;
            List<int> atifs = m.HCore.Select(x => x.CitationCount).OrderBy(x => x).ToList();
            return atifs[atifs.Count / 2];
        }

        private void IstatistikleriGuncelle()
        {
            if (gorunurMakaleler.Count == 0)
            {
                lblDetayliIstatistik.Text = "Veri yok.";
                return;
            }

            int totalNodes = gorunurMakaleler.Count;
            int totalEdges = 0;
            int maxIn = -1, maxOut = -1;
            string maxInId = "-", maxOutId = "-";

            foreach (var m in gorunurMakaleler)
            {
                int outDeg = m.ReferencedWorks?.Count ?? 0;
                if (outDeg > maxOut)
                {
                    maxOut = outDeg;
                    maxOutId = m.Id;
                }

                int inDeg = m.CitationCount;
                if (inDeg > maxIn)
                {
                    maxIn = inDeg;
                    maxInId = m.Id;
                }

                if (m.ReferencedWorks != null)
                    foreach (var r in m.ReferencedWorks)
                        if (gorunurMakaleler.Any(x => x.Id == r))
                            totalEdges++;
            }

            lblDetayliIstatistik.Text = $"• Makale: {totalNodes}\n• İlişki: {totalEdges}\n" +
                                        $"★ En Çok Atıf:\nID:{maxInId.Replace("https://openalex.org/", "")}\nSayısı:{maxIn}\n" +
                                        $"★ En Çok Ref:\nID:{maxOutId.Replace("https://openalex.org/", "")}\nSayısı:{maxOut}";
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

            float angle = 0, radius = 120, angleStep = 0.6f, radiusStep = 5.0f;

            foreach (var core in merkez.HCore)
            {
                if (!gorunurMakaleler.Contains(core))
                {
                    int maxTry = 2000;
                    bool found = false;
                    while (!found && maxTry-- > 0)
                    {
                        float ax = merkez.X + (float)(Math.Cos(angle) * radius);
                        float ay = merkez.Y + (float)(Math.Sin(angle) * radius);
                        if (!CakisiyorMu(ax, ay))
                        {
                            core.X = ax;
                            core.Y = ay;
                            found = true;
                        }
                        else
                        {
                            angle += angleStep;
                            radius += radiusStep;
                        }
                    }

                    gorunurMakaleler.Add(core);
                    sonEklenenler.Add(core);
                }
            }

            IstatistikleriGuncelle();
            this.Invalidate();
        }

        private bool CakisiyorMu(float x, float y)
        {
            float safeDist = (DUGUM_R * 2) + 20;
            foreach (var other in gorunurMakaleler)
            {
                float dx = x - other.X;
                float dy = y - other.Y;
                if ((dx * dx) + (dy * dy) < safeDist * safeDist) return true;
            }

            return false;
        }

        private void BtnTumunuGoster_Click(object sender, EventArgs e)
        {
            if (anaGraf == null || anaGraf.MakaleListesi.Count == 0)
            {
                MessageBox.Show("Henüz veri indirilmedi.");
                return;
            }

            if (!tumunuGosterModu)
            {
                hafizadakiMakaleler = gorunurMakaleler.ToList();
                hafizadakiKonumlar.Clear();

                foreach (var m in gorunurMakaleler)
                {
                    hafizadakiKonumlar[m.Id] = new PointF(m.X, m.Y);
                }

                hafizaZoom = zoomFactor;
                hafizaViewX = viewX;
                hafizaViewY = viewY;

                gorunurMakaleler.Clear();
                foreach (var m in anaGraf.MakaleListesi)
                {
                    gorunurMakaleler.Add(m);
                }

                int toplamSayi = gorunurMakaleler.Count;
                int sutunSayisi = (int)Math.Ceiling(Math.Sqrt(toplamSayi));
                int bosluk = 120;

                int i = 0;
                foreach (var m in gorunurMakaleler)
                {
                    int satir = i / sutunSayisi;
                    int sutun = i % sutunSayisi;

                    m.X = sutun * bosluk;
                    m.Y = satir * bosluk;
                    i++;
                }

                float gridGenislik = sutunSayisi * bosluk;
                float gridYukseklik = (toplamSayi / sutunSayisi) * bosluk;

                float ekranW = this.ClientSize.Width - 280;
                float ekranH = this.ClientSize.Height;

                float oranX = ekranW / (gridGenislik + 200);
                float oranY = ekranH / (gridYukseklik + 200);

                zoomFactor = Math.Min(oranX, oranY);
                if (zoomFactor > 1.0f) zoomFactor = 1.0f;
                if (zoomFactor < 0.02f) zoomFactor = 0.02f;

                float gridOrtaX = gridGenislik / 2;
                float gridOrtaY = gridYukseklik / 2;
                float ekranOrtaX = 280 + (ekranW / 2);
                float ekranOrtaY = ekranH / 2;

                viewX = ekranOrtaX - (gridOrtaX * zoomFactor);
                viewY = ekranOrtaY - (gridOrtaY * zoomFactor);

                ((Button)sender).Text = "↩️ Önceki Duruma Dön";
                ((Button)sender).BackColor = Color.Orange;
                lblDurum.Text = $"Tüm veriler ({toplamSayi} adet) gösteriliyor.";

                tumunuGosterModu = true;
            }
            else
            {
                gorunurMakaleler.Clear();
                foreach (var m in hafizadakiMakaleler)
                {
                    gorunurMakaleler.Add(m);
                    if (hafizadakiKonumlar.TryGetValue(m.Id, out PointF eskiKonum))
                    {
                        m.X = eskiKonum.X;
                        m.Y = eskiKonum.Y;
                    }
                }

                zoomFactor = hafizaZoom;
                viewX = hafizaViewX;
                viewY = hafizaViewY;

                ((Button)sender).Text = "🔍 Tümünü Ekrana Sığdır";
                ((Button)sender).BackColor = Color.LightSeaGreen;
                lblDurum.Text = "Önceki görünüme dönüldü.";

                tumunuGosterModu = false;
            }

            this.Invalidate();
        }

        // --- ÇİZİM FONKSİYONU GÜNCELLENDİ ---
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (anaGraf == null) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.TranslateTransform(viewX, viewY);
            g.ScaleTransform(zoomFactor, zoomFactor);

            // 1. ADIM: ÇİZGİLER (Edges)
            foreach (var m in gorunurMakaleler)
            {
                if (m.ReferencedWorks != null)
                {
                    foreach (string refId in m.ReferencedWorks)
                    {
                        if (anaGraf.Makaleler.TryGetValue(refId, out Makale hedef) && gorunurMakaleler.Contains(hedef))
                        {
                            float dx = hedef.X - m.X, dy = hedef.Y - m.Y;
                            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                            if (dist > 0)
                            {
                                float ex = hedef.X - (dx / dist) * DUGUM_R;
                                float ey = hedef.Y - (dy / dist) * DUGUM_R;

                                // --- ÇİZGİ TİPİ SEÇİMİ ---
                                if (kCoreModuAktif)
                                {
                                    // K-CORE MODUNDA OK YOK, SADECE ÇİZGİ VAR.
                                    // Eğer iki uç da core ise -> Mavi Kalın Çizgi
                                    // Değilse -> Gri İnce Çizgi
                                    bool isCoreEdge = kCoreDugumler.Contains(m) && kCoreDugumler.Contains(hedef);
                                    g.DrawLine(isCoreEdge ? penKCoreLine : penNormalLine, m.X, m.Y, ex, ey);
                                }
                                else
                                {
                                    // NORMAL MODDA OK VAR (YÖNLÜ)
                                    g.DrawLine(penNormalArrow, m.X, m.Y, ex, ey);
                                }
                            }
                        }
                    }
                }
            }

            // 2. ADIM: DÜĞÜMLER (Nodes)
            foreach (var m in gorunurMakaleler)
            {
                Brush firca = (m == seciliMakale)
                    ? Brushes.Red
                    : (sonEklenenler.Contains(m) ? Brushes.LightGreen : Brushes.LightSteelBlue);
                if (m == mouseUzerindekiMakale) firca = Brushes.Orange;

                g.FillEllipse(firca, m.X - DUGUM_R, m.Y - DUGUM_R, DUGUM_R * 2, DUGUM_R * 2);
                g.DrawEllipse(Pens.Black, m.X - DUGUM_R, m.Y - DUGUM_R, DUGUM_R * 2, DUGUM_R * 2);

                string cleanId = m.Id.Replace("https://openalex.org/", "");
                string initials = (m.Authors?.Count > 0)
                    ? string.Join("", m.Authors[0].Split(' ').Select(p => p.Length > 0 ? p[0] + "." : ""))
                    : "-";

                StringFormat sf = new StringFormat
                    { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                using (Font fInit = new Font("Arial", 8, FontStyle.Bold))
                    g.DrawString(initials, fInit, Brushes.Black,
                        new RectangleF(m.X - DUGUM_R, m.Y - DUGUM_R + 5, DUGUM_R * 2, DUGUM_R), sf);

                float fSize = 7.5f;
                Font fId;
                while (true)
                {
                    fId = new Font("Arial Narrow", fSize);
                    if (g.MeasureString(cleanId, fId).Width < (DUGUM_R * 2) - 4 || fSize <= 4f) break;
                    fId.Dispose();
                    fSize -= 0.5f;
                }

                g.DrawString(cleanId, fId, Brushes.Black, new RectangleF(m.X - DUGUM_R, m.Y, DUGUM_R * 2, DUGUM_R), sf);
                fId.Dispose();
            }

            // TOOLTIP
            g.ResetTransform();
            if (mouseUzerindekiMakale != null)
            {
                string t = mouseUzerindekiMakale.Title ?? "Başlık Yok";
                string a = (mouseUzerindekiMakale.Authors != null)
                    ? string.Join(", ", mouseUzerindekiMakale.Authors)
                    : "-";

                int calculatedCitation = 0;
                foreach (var other in anaGraf.MakaleListesi)
                    if (other.ReferencedWorks != null && other.ReferencedWorks.Contains(mouseUzerindekiMakale.Id))
                        calculatedCitation++;

                string yl = $"Yıl: {mouseUzerindekiMakale.Year} | Atıf: {calculatedCitation}";

                int w = 300, pad = 10;
                SizeF ts = g.MeasureString(t, fntTitle, w);
                SizeF @as = g.MeasureString(a, fntInfo, w);
                SizeF ys = g.MeasureString(yl, fntInfo, w);
                float th = ts.Height + @as.Height + ys.Height + (pad * 3);

                Point mp = this.PointToClient(Cursor.Position);
                float bx = mp.X + 20, by = mp.Y + 20;
                if (bx + w > this.Width) bx = mp.X - w - 10;
                if (by + th > this.Height) by = mp.Y - th - 10;

                g.FillRectangle(Brushes.WhiteSmoke, bx, by, w, th);
                g.DrawRectangle(Pens.Black, bx, by, w, th);

                float cy = by + pad;
                g.DrawString(t, fntTitle, Brushes.Black, new RectangleF(bx + pad, cy, w - pad, ts.Height));
                cy += ts.Height;
                g.DrawString(a, fntInfo, Brushes.DarkSlateGray, new RectangleF(bx + pad, cy, w - pad, @as.Height));
                cy += @as.Height;
                g.DrawString(yl, fntInfo, Brushes.DarkRed, new RectangleF(bx + pad, cy, w - pad, ys.Height));
            }
        }

        private void Form1_MouseWheel(object sender, MouseEventArgs e)
        {
            float old = zoomFactor;
            zoomFactor *= (e.Delta > 0) ? 1.1f : 1 / 1.1f;
            zoomFactor = Math.Max(0.02f, Math.Min(zoomFactor, 5f));

            float mx = (e.X < PANEL_W) ? (Width + PANEL_W) / 2 : e.X;
            float my = (e.X < PANEL_W) ? Height / 2 : e.Y;

            float wx = (mx - viewX) / old, wy = (my - viewY) / old;
            viewX = mx - (wx * zoomFactor);
            viewY = my - (wy * zoomFactor);
            this.Invalidate();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                isDragging = true;
                lastMousePos = e.Location;
                Cursor = Cursors.SizeAll;
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            Cursor = Cursors.Default;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                viewX += e.X - lastMousePos.X;
                viewY += e.Y - lastMousePos.Y;
                lastMousePos = e.Location;
                this.Invalidate();
                return;
            }

            float wx = (e.X - viewX) / zoomFactor, wy = (e.Y - viewY) / zoomFactor;
            Makale hit = null;
            foreach (var m in gorunurMakaleler)
                if (Math.Pow(wx - m.X, 2) + Math.Pow(wy - m.Y, 2) < Math.Pow(DUGUM_R, 2))
                    hit = m;
            if (hit != mouseUzerindekiMakale)
            {
                mouseUzerindekiMakale = hit;
                Cursor = (hit != null) ? Cursors.Hand : Cursors.Default;
                this.Invalidate();
            }
        }
    }
}