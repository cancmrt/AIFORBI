# 📊 AIFORBI (AI for Business Intelligence)

AIFORBI, **MSSQL veritabanınıza doğrudan bağlanarak** tablo ilişkilerini, kısıtlamaları ve tablo işlevlerini otomatik olarak tarayan;
**Vektör Veritabanı (Qdrant)** ve **Ollama Embedding** mimarisiyle güçlendirilmiş, yapay zekâ destekli bir **İş Zekâsı (BI) raporlama ve veri analiz aracıdır**.

AIFORBI ile SQL sorguları yazmakla vakit kaybetmezsiniz.
Yalnızca neye ihtiyacınız olduğunu **doğal dilde söylersiniz**, sistem sizin yerinize veriyi bulur, analiz eder ve görselleştirir.

---

## 🔥 Temel Özellikler

* **Akıllı Şema Analizi**

  * MSSQL metadata bilgilerini tarar
  * Tablo ilişkilerini (Foreign Key), veri tiplerini ve kolon işlevlerini otomatik öğrenir

* **Vektör Tabanlı Şema Belleği (RAG)**

  * Veritabanı metadata bilgileri **Ollama (nomic-embed-text)** ile vektörleştirilir
  * **Qdrant** üzerinde saklanır
  * AI, soruya en uygun tablo ve ilişkileri milisaniyeler içinde tespit eder

* **Doğal Dil ile Sorgulama**

  * Örn:

    > *"Geçen yılın satışlarını bölge bazlı pasta grafiği olarak göster"*

* **Dinamik SQL Üretimi**

  * MSSQL diyalektine tam uyumlu
  * Optimize edilmiş ve güvenli sorgular üretir

* **Otomatik Grafik ve Raporlama**

  * Veri setine göre en uygun görselleştirme türünü otomatik seçer
    (Bar, Line, Pie vb.)

---

## 🛠 Kullanılan Teknolojiler

| Bileşen              | Teknoloji                    | Notlar                                 |
| -------------------- | ---------------------------- | -------------------------------------- |
| Veritabanı           | Microsoft SQL Server (MSSQL) | Şu an yalnızca MSSQL desteklenmektedir |
| Vektör Veritabanı    | Qdrant                       | RAG ve anlamsal arama için             |
| Lokal AI (LLM)       | Ollama (qwen2.5-coder:7b)    | SQL üretimi ve mantıksal analiz        |
| Embedding Modeli     | Ollama (nomic-embed-text)    | Şema bilgisini vektörleştirmek için    |
| Bulut AI (Opsiyonel) | Google Gemini API            | Alternatif yüksek performanslı analiz  |

---

## 🏗 Çalışma Mantığı

1. **Scanning**

   * MSSQL şeması taranır (tablolar, kolonlar, ilişkiler)

2. **Embedding & Indexing**

   * Metadata bilgileri embed edilir
   * Qdrant vektör veritabanına indekslenir

3. **Retrieval**

   * Kullanıcı sorusu için anlamsal arama yapılır
   * İlgili tablo ve kolonlar belirlenir

4. **Generation**

   * Seçilen şema bilgileri AI modeline gönderilir
   * Doğru ve optimize SQL üretilir

5. **Execution & Visualization**

   * SQL sorgusu MSSQL üzerinde çalıştırılır
   * Sonuçlar dinamik grafiklere dönüştürülür

---

## ⚙️ Kurulum

### 1. Depoyu Klonlayın

```bash
git clone https://github.com/cancmrt/AIFORBI.git
cd AIFORBI
```

### 2. Ollama Modellerini Çekiniz

```bash
ollama pull qwen2.5-coder:7b
ollama pull nomic-embed-text
```

### 3. Mssql veritabanınızı ve Qdrantı kurun

### 4. Yapılandırma (.appsettings.json)

Appsettings dosyasına gerekli birgileri girin

---

## 💡 Örnek Kullanımlar

* `"Ürünler tablosundaki en yüksek fiyatlı 5 ürünü listele"`
* `"Batı bölgesindeki satış temsilcilerinin toplam cirosunu pasta grafiği yap"`
* `"Son 6 aydaki stok değişimlerini çizgi grafik olarak göster"`

---

## 🤝 Katkıda Bulunma

1. Bu depoyu **fork** edin
2. Yeni bir dal oluşturun

   ```bash
   git checkout -b feature/yeniozellik
   ```
3. Değişikliklerinizi commit edin

   ```bash
   git commit -m "Yeni özellik eklendi"
   ```
4. Dalınızı push edin

   ```bash
   git push origin feature/yeniozellik
   ```
5. **Pull Request** açın

---

## 👤 Geliştirici

**Antigravity**
**Can Cömert**
GitHub: [https://github.com/cancmrt](https://github.com/cancmrt)

---

## 📄 Lisans

Bu proje **MIT Lisansı** ile lisanslanmıştır.
