# AIFORBI - AI-Powered Database Intelligence

AIFORBI, veritabanlarınızı doğal dil ile sorgulayabileceğiniz yapay zeka destekli bir platformdur. SQL Server veritabanlarınızı analiz eder, AI kullanarak sorularınızı yanıtlar ve görselleştirmeler oluşturur.

## 🚀 Özellikler

- **🤖 AI-Powered Chat**: Gemini veya Ollama kullanarak doğal dil sorguları
- **📊 Otomatik Görselleştirme**: Verilerinizi grafikler ve tablolarla sunar
- **🔍 Akıllı Veritabanı Indexing**: Vector search ile hızlı veri erişimi
- **👥 Rol Tabanlı Erişim**: Admin ve User rolleri ile güvenli yönetim
- **⚙️ Ayarlar Yönetimi**: Web arayüzünden tüm ayarları değiştirebilme
- **💬 Oturum Yönetimi**: Geçmiş sohbetlerinizi kaydetme ve geri yükleme

## 📋 Gereksinimler

### Yazılım Gereksinimleri
- **.NET 8.0 SDK** veya üzeri
- **Node.js 18+** ve **npm**
- **SQL Server** (LocalDB, Express veya Full Edition)
- **Qdrant Vector Database** (Docker ile çalıştırılabilir)
- **Ollama** (opsiyonel, local AI için) VEYA **Gemini API Key**

### Qdrant Kurulumu (Docker)
```bash
docker pull qdrant/qdrant
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

### Ollama Kurulumu (Opsiyonel)
```bash
# Windows için: https://ollama.ai/download/windows
# Yüklendikten sonra:
ollama pull qwen2.5-coder:7b
ollama pull nomic-embed-text
```

## 🛠️ Kurulum

### 1. Repository'yi İndirin
```bash
cd c:\Users\admin\Documents\Projects\.NET\AIFORBI
```

### 2. Veritabanı Kurulumu

#### SQL Server Bağlantısını Yapılandırın
`appsettings.json` dosyasında veritabanı bağlantı bilgilerinizi güncelleyin:

```json
{
  "ConnStrs": {
    "DbConnector": {
      "Type": "Mssql",
      "Mssql": {
        "ConnStr": "Server=localhost,1433;Database=YourDatabase;User Id=sa;Password=YourPassword;TrustServerCertificate=True;",
        "DatabaseName": "YourDatabase",
        "Schema": "dbo"
      }
    }
  }
}
```

#### Uygulama Tablolarını Oluşturun
İlk çalıştırmada `AIFORBI_USERS` ve `AIFORBI_CHATS` tabloları otomatik oluşturulur.

**Manuel olarak oluşturmak isterseniz:**
```sql
-- Kullanıcı tablosu
CREATE TABLE AIFORBI_USERS (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    EMAIL NVARCHAR(255) NOT NULL UNIQUE,
    PASSWORD_HASH NVARCHAR(255) NOT NULL,
    DISPLAY_NAME NVARCHAR(255),
    USER_ROLE NVARCHAR(20) NOT NULL DEFAULT 'user',
    CREATED_AT DATETIME DEFAULT GETDATE()
);

-- Varsayılan admin kullanıcısı (şifre: admin123)
INSERT INTO AIFORBI_USERS (EMAIL, PASSWORD_HASH, DISPLAY_NAME, USER_ROLE)
VALUES ('admin@admin.com', 'admin123', 'Admin User', 'admin');

-- Chat geçmişi tablosu
CREATE TABLE AIFORBI_CHATS (
    ID INT IDENTITY(1,1) PRIMARY KEY,
    SESSION_ID NVARCHAR(50) NOT NULL,
    USER_ID INT NOT NULL,
    ROLE NVARCHAR(20) NOT NULL,
    CONTENT NVARCHAR(MAX) NOT NULL,
    IS_HTML BIT DEFAULT 0,
    CREATED_AT DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (USER_ID) REFERENCES AIFORBI_USERS(ID)
);
```

### 3. AI Konfigürasyonu

#### Gemini API (Önerilen)
1. [Google AI Studio](https://makersuite.google.com/app/apikey) adresinden API key alın
2. `appsettings.json` dosyasını güncelleyin:

```json
{
  "ConnStrs": {
    "AI": {
      "ChatProvider": "Gemini",
      "EmbedProvider": "Ollama"
    },
    "Gemini": {
      "ApiKey": "YOUR_GEMINI_API_KEY",
      "Model": "gemini-2.0-flash-exp",
      "FallbackModels": [
        "gemini-2.5-flash",
        "gemini-2.0-flash"
      ]
    }
  }
}
```

#### Ollama (Local AI)
```json
{
  "ConnStrs": {
    "AI": {
      "ChatProvider": "Ollama",
      "EmbedProvider": "Ollama"
    },
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "ChatModel": "qwen2.5-coder:7b",
      "EmbedModel": "nomic-embed-text"
    }
  }
}
```

### 4. Bağımlılıkları Yükleyin

#### Backend
```bash
cd AIFORBI
dotnet restore
```

#### Frontend
```bash
cd Client
npm install
```

### 5. Uygulamayı Çalıştırın

**.NET Backend + Vite Frontend (Tek Komut):**
```bash
cd AIFORBI
dotnet run
```

Tarayıcınız otomatik açılmalı ve `https://localhost:5173` adresinde uygulama başlamalı.

**Alternatif: Ayrı Ayrı Çalıştırma**
```bash
# Terminal 1 - Backend
cd AIFORBI
dotnet run

# Terminal 2 - Frontend
cd Client
npm run dev
```

## 📖 Kullanım

### İlk Giriş

1. Tarayıcıda `https://localhost:5173` adresine gidin
2. Varsayılan admin hesabı ile giriş yapın:
   - **Email:** `admin@admin.com`
   - **Şifre:** `admin123`

### Veritabanı Indexing

İlk kullanımdan önce veritabanınızı AI için hazırlamanız gerekir:

1. Sol altta **Settings** butonuna tıklayın (sadece admin görür)
2. Veritabanı ve AI ayarlarını kontrol edin
3. **"Run DB Indexing"** butonuna tıklayın
4. İşlem tamamlanana kadar bekleyin (birkaç dakika sürebilir)

### Soru Sorma

Chat arayüzünde doğal dil ile sorular sorabilirsiniz:

**Örnek Sorular:**
- "Geçen ay kaç adet satış yapıldı?"
- "En çok satılan 10 ürünü listele"
- "Aylık gelir trendini göster"
- "Hangi müşteriler en fazla alışveriş yaptı?"

AI sorunuzu anlayacak, SQL sorgusu oluşturacak, çalıştıracak ve sonuçları grafiklerle sunacak.

### Ayarlar Yönetimi (Admin)

Settings sayfasında şunları yapabilirsiniz:
- ✏️ Veritabanı bağlantısını değiştirme
- 🤖 AI provider seçimi (Gemini/Ollama)
- 🔑 API anahtarlarını güncelleme
- 🎯 Model ayarlarını değiştirme
- 🔄 Veritabanını yeniden indexleme

## 🔐 Kullanıcı Rolleri

### Admin
- Tüm ayarlara erişim
- Veritabanı indexing
- Chat kullanımı
- Kullanıcı yönetimi (Settings'ten)

### User
- Sadece chat kullanımı
- Geçmiş sohbetleri görüntüleme
- Ayarlara erişim YOK

**Yeni admin kullanıcı oluşturma:**
```sql
INSERT INTO AIFORBI_USERS (EMAIL, PASSWORD_HASH, DISPLAY_NAME, USER_ROLE)
VALUES ('newadmin@company.com', 'password123', 'New Admin', 'admin');
```

## 🗂️ Proje Yapısı

```
AIFORBI/
├── AIFORBI/                    # Backend (.NET 8)
│   ├── Controllers/            # API Controllers
│   ├── Services/               # Business Logic
│   ├── Models/                 # DTOs ve Models
│   └── appsettings.json        # Konfigürasyon
├── DBCONNECTOR/                # Veritabanı katmanı
│   ├── Connectors/             # DB Connectors (MSSQL)
│   ├── Repositories/           # Data Access
│   └── Dto/                    # Database DTOs
├── Client/                     # Frontend (React + Vite)
│   ├── src/
│   │   ├── components/         # React Components
│   │   ├── types.ts            # TypeScript Types
│   │   └── App.tsx             # Ana Uygulama
│   └── package.json
└── README.md
```

## 🐛 Sorun Giderme

### Backend Hatası: "Cannot connect to database"
- SQL Server'ın çalıştığından emin olun
- `appsettings.json`'daki bağlantı bilgilerini kontrol edin
- Firewall ayarlarını kontrol edin

### Frontend Hatası: "API connection failed"
- Backend'in çalıştığından emin olun (`dotnet run`)
- CORS ayarlarını kontrol edin
- Tarayıcı console'unda hata mesajlarını inceleyin

### Qdrant Bağlantı Hatası
```bash
# Qdrant'ın çalıştığını kontrol edin
docker ps | grep qdrant

# Çalışmıyorsa başlatın
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

### Ollama Modeli Bulunamadı
```bash
# Modellerin yüklü olduğunu kontrol edin
ollama list

# Eksikse yükleyin
ollama pull qwen2.5-coder:7b
ollama pull nomic-embed-text
```

### Indexing Çok Uzun Sürüyor
- Büyük veritabanları için normal (ilk indexing)
- Backend console'da ilerlemeyi takip edebilirsiniz
- Gerekirse `ForceToAISummary=false` kullanın (AI özeti atlar)

## 🔧 Gelişmiş Konfigürasyon

### Qdrant Ayarları
```json
{
  "ConnStrs": {
    "Qdrant": {
      "Host": "localhost",
      "Grpc": "6334"
    }
  }
}
```

### AI Model Seçenekleri

**Gemini Modeller:**
- `gemini-2.0-flash-exp` (Hızlı, önerilen)
- `gemini-2.5-flash` (Yeni, daha güçlü)
- `gemini-pro` (Daha detaylı)

**Ollama Modeller:**
- `qwen2.5-coder:7b` (Kod ve SQL için optimize)
- `llama3.1:8b` (Genel amaçlı)
- `mistral:7b` (Hızlı ve verimli)

## 📝 Notlar

- İlk indexing işlemi sırasında backend'de yoğun CPU kullanımı normal
- Gemini API key'i için Google Cloud hesabı gerekebilir
- Ollama local çalıştığı için internet gerektirmez
- Chat geçmişi veritabanında saklanır, silinmez

## 🤝 Destek

Sorun yaşarsanız:
1. Backend console'da hata mesajlarını kontrol edin
2. Browser console'u açın (F12) ve network tabını inceleyin
3. `appsettings.json` dosyasının doğru formatta olduğundan emin olun

## 📄 Lisans

Bu proje özel kullanım içindir.

---

**Geliştirici Notu:** Bu uygulama .NET 8, React 18, ve modern AI teknolojileri ile geliştirilmiştir. Sorunlarınız için lütfen dokümantasyonu kontrol edin veya admin ile iletişime geçin.
