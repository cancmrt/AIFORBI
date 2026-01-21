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


# 📊 AIFORBI (AI for Business Intelligence)

AIFORBI is an **AI-powered Business Intelligence (BI) reporting and data analysis tool** that connects directly to your **MSSQL database**, automatically scans table relationships, constraints, and table semantics, and is enhanced with a **Vector Database (Qdrant)** and **Ollama Embedding** architecture.

With AIFORBI, you don’t waste time writing SQL queries.
You simply describe **what you need in natural language**, and the system finds, analyzes, and visualizes the data on your behalf.

---

## 🔥 Key Features

* **Intelligent Schema Analysis**

  * Scans MSSQL metadata
  * Automatically learns table relationships (Foreign Keys), data types, and column semantics

* **Vector-Based Schema Memory (RAG)**

  * Database metadata is vectorized using **Ollama (nomic-embed-text)**
  * Stored in **Qdrant**
  * Enables the AI to identify the most relevant tables and relationships within milliseconds

* **Natural Language Querying**

  * Example:

    > *"Show last year’s sales as a region-based pie chart"*

* **Dynamic SQL Generation**

  * Fully compliant with the MSSQL dialect
  * Generates optimized and secure SQL queries

* **Automatic Visualization & Reporting**

  * Automatically selects the most appropriate visualization type based on the dataset
    (Bar, Line, Pie, etc.)

---

## 🛠 Technologies Used

| Component           | Technology                   | Notes                                 |
| ------------------- | ---------------------------- | ------------------------------------- |
| Database            | Microsoft SQL Server (MSSQL) | Currently only MSSQL is supported     |
| Vector Database     | Qdrant                       | Used for RAG and semantic search      |
| Local AI (LLM)      | Ollama (qwen2.5-coder:7b)    | SQL generation and logical reasoning  |
| Embedding Model     | Ollama (nomic-embed-text)    | Used to vectorize schema metadata     |
| Cloud AI (Optional) | Google Gemini API            | Alternative high-performance analysis |

---

## 🏗 How It Works

1. **Scanning**

   * The MSSQL schema is scanned (tables, columns, relationships)

2. **Embedding & Indexing**

   * Metadata is embedded
   * Indexed into the Qdrant vector database

3. **Retrieval**

   * Semantic search is performed based on the user’s question
   * Relevant tables and columns are identified

4. **Generation**

   * Selected schema context is sent to the AI model
   * Accurate and optimized SQL queries are generated

5. **Execution & Visualization**

   * SQL queries are executed on MSSQL
   * Results are transformed into dynamic visualizations

---

## ⚙️ Installation

### 1. Clone the Repository

```bash
git clone https://github.com/cancmrt/AIFORBI.git
cd AIFORBI
```

### 2. Pull Ollama Models

```bash
ollama pull qwen2.5-coder:7b
ollama pull nomic-embed-text
```

### 3. Set Up MSSQL Database and Qdrant

### 4. Configuration (.appsettings.json)

Enter the required configuration values in the `appsettings.json` file.

---

## 💡 Example Queries

* `"List the top 5 most expensive products from the Products table"`
* `"Create a pie chart of total revenue for sales representatives in the West region"`
* `"Show stock changes over the last 6 months as a line chart"`

---

## 🤝 Contributing

1. **Fork** this repository
2. Create a new branch

   ```bash
   git checkout -b feature/new-feature
   ```
3. Commit your changes

   ```bash
   git commit -m "Add new feature"
   ```
4. Push your branch

   ```bash
   git push origin feature/new-feature
   ```
5. Open a **Pull Request**

---

## 👤 Developer

**Antigravity**
**Can Cömert**
GitHub: [https://github.com/cancmrt](https://github.com/cancmrt)

---

## 📄 License

This project is licensed under the **MIT License**.

