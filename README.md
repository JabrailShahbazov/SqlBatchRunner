# ScriptPilot — SQL Runner (Windows, .NET 9)

**ScriptPilot** SQL skriptlərinizi təhlükəsiz və nəzarətli şəkildə ardıcıl icra edən **WinForms (.NET 9)** tətbiqidir. 
Sadə **UI**, çevik **konfiq**, anlıq **log** və təkrar icranın qarşısını alan **jurnal** (hash əsasında) ilə gəlir.

- **Platforma:** Windows 10/11 (x64), .NET 9 (self-contained — ayrıca runtime tələb olunmur)  
- **Konfiq:** `C:\ProgramData\ScriptPilot\appsettings.json`  
- **Loglar:** `ScriptsFolder\{Logging.Folder}` (nisbidirsə, *ScriptsFolder*-a nisbətən)

---

## ✨ Xüsusiyyətlər

- **Mənbə:** Qovluq (**Folder**) və ya Arxiv (**.zip/.rar/.7z/.tar/.gz**)
- **Sıralama:** Fayl sisteminin vaxtına və/və ya **fayl adındakı tarixə** görə dəqiq sıralama
- **Batch icrası:** `GO` markerlərinə görə bölüb ardıcıl icra
- **Jurnal (executed.json):** Skript mətni dəyişməyibsə (hash eynidirsə) **SKIP**
- **Dry-Run:** İcrasız “nə ediləcəkdi” planını göstərir
- **Loglama:** Gündəlik fayllar (məs. `app-YYYYMMDD.log`) + UI-da canlı axın
- **Installer:** Inno Setup — Start Menu/desktop qısayolu, `Program Files (x86)`-a quraşdırma

---

## 🧩 Sistem tələbləri

- Windows 10/11 (x64)  
- SQL Server (on-prem/VM) — **Microsoft.Data.SqlClient** ilə qoşulur

---

## 📦 Quraşdırma (End-User)

1. `ScriptPilot_Setup_1.0.0.exe` faylını işə salın.  
2. “Application Language” səhifəsində **Azerbaijani (default)** və ya **English** seçin.  
   > Seçimə uyğun `appsettings.json` ilk dəfə üçün `C:\ProgramData\ScriptPilot\` altına kopyalanır.
3. Quraşdırma bitdikdən sonra tətbiq açılacaq.

> **Qeyd:** Konfiq **Program Files**-da deyil — **ProgramData** yolundadır:  
> `C:\ProgramData\ScriptPilot\appsettings.json`

---

## 🚀 İlk konfiqurasiya (vacib)

**Open appsettings.json** düyməsi ilə faylı açın, ən azı **ConnectionString** dəyərini doldurun.

**Nümunələr**
```txt
Server=./;Database=MyDb;User ID=user;Password=pass;Encrypt=True;TrustServerCertificate=True;
```
```txt
Server=.\SQLEXPRESS;Database=MyDb;Trusted_Connection=True;Encrypt=False;
```

> **TLS/Sertifikat xətası:** test üçün müvəqqəti `Encrypt=True;TrustServerCertificate=True` istifadə edin.  
> Production mühitdə düzgün sertifikat tövsiyə olunur.

---

## 🖥️ UI bələdçisi (düymələr və sahələr)

### Source (Mənbə)
- **Mode**
  - **Folder** — `.sql` faylları qovluqdadır → **Scripts folder** + **Browse…** aktiv
  - **Archive** — `.sql` faylları arxivdədir → **Archive file** + **Browse…** aktiv
- **Scripts folder** — Folder rejimində `.sql` fayllarının olduğu qovluq
- **Archive file** — Archive rejimində `.zip/.rar/.7z/.tar/.gz` faylının yolu
- **Open work…** — yalnız **Archive** rejimində son çıxarılan iş qovluğunu (temp) açır

### Connection
- **Connection string** — geniş textarea; SQL Server bağlantı sətrinizi yazın

### Options (Parametrlər)
- **Order by**
  - `LastWriteTime` — fayl sistemində *son dəyişmə vaxtına* görə (köhnədən→yeni), bərabərlikdə ada görə
  - `FileNameDate` — *fayl adındakı tarixə* görə (köhnədən→yeni), bərabərlikdə ada görə
  - `LastWriteTimeThenFileNameDate` *(default)* — əvvəl FS vaxtı, sonra fayl adı tarixi
  - `FileNameDateThenLastWriteTime` — əvvəl fayl adı tarixi, sonra FS vaxtı
- **Use creation time** — FS vaxtı kimi *CreationTime* istifadə et (işarəli deyilsə *LastWriteTime*)
- **Stop on error** — xətada **dayandır**
- **Dry run** — SQL **icra etmir**, planı göstərir
- **RerunIfChanged** *(config)* — `false` olduqda eyni mətnli skript **yenidən icra edilmir** (hash əsasında)

### Actions (Düymələr)
- **Save** — ekran dəyərlərini `C:\ProgramData\ScriptPilot\appsettings.json`-a yazır
- **Run** — icraya başlayır
- **Open appsettings.json** — konfiqi Notepad ilə açır
- **Open logs** — log qovluğunu Explorer-də açır

### Log paneli
- Anlıq status: `[INFO]`, `[ERR ]`, `[SKIP]`, `[RUN ]` və s.

---

## 🧭 Sıralama və tarix oxuma qaydaları

Fayl adında tarix varsa, sistem onu oxuyub sıralamada istifadə edir. Dəstəklənən format nümunələri  
(tarix **fayl adının istənilən yerində** ola bilər; **boşluq**, **alt xət** `_` və **tire** `-` ayırıcıları qəbul edilir):

- `2025-08-20.sql`  
- `2025_08_20_173455_init.sql`  (HHmmss)  
- `20250820 1734 add_table.sql` (HHmm)  
- `prefix 2025-08-20 suffix.sql`

> Tarix **tapılmazsa**, *FileNameDate* boş qalır — bu halda seçdiyiniz *Order by* qaydasına uyğun FS vaxtı (və ya ad) ilə sıralanır.

---

## 📚 Arxiv rejimi

- Dəstəklənən arxivlər: **.zip / .rar / .7z / .tar / .gz** (rekursiv olaraq iç qovluqlardan `.sql` faylları toplanır)
- Hər icrada arxiv **müvəqqəti iş qovluğuna** çıxarılır:  
  `C:\ProgramData\ScriptPilot\work\yyyyMMdd_HHmmss_xxxxx\`
- **Open work…** düyməsi ilə son iş qovluğunu aça bilərsiniz
- **WorkKeepCount** *(config)* — saxlanılacaq **ən yeni** iş qovluqlarının sayı (default: `1`). Artıq qalanlar avtomatik silinir

---

## 📝 Jurnal (executed.json) və təkrar icra

- `JournalFile` — icra jurnalı; *ScriptsFolder* bazasında saxlanılır (nisbi yazılıbsa)
- Hər skript mətninin **SHA-256** hash-i çıxarılıb jurnala yazılır
- **RerunIfChanged = false** — eyni mətnli skript **SKIP**
- **RerunIfChanged = true** — skript həmişə icra oluna bilər

---

## 📑 Loglama

- Log qovluğu: `ScriptsFolder\{Logging.Folder}` (nisbidirsə – *ScriptsFolder*-a nisbətən)  
  Nümunə: `C:\MyScripts\logs\app-20250828.log`
- **RollingByDate = true** → Serilog avtomatik `app-YYYYMMDD.log` yaradacaq
- **MinimumLevel**: `Debug`, `Information`, `Warning`, `Error`, `Fatal`

---

## ⚙️ Konfiqurasiya nümunəsi və izah

**Fayl:** `C:\ProgramData\ScriptPilot\appsettings.json`

```json
{
  "Language": "az",
  "SourceMode": "Folder",
  "ArchivePath": "",
  "ConnectionString": "",
  "ScriptsFolder": "./sql",
  "FilePattern": "*.sql",
  "OrderBy": "LastWriteTimeThenFileNameDate",
  "UseCreationTime": false,
  "StopOnError": false,
  "JournalFile": "executed.json",
  "DryRun": false,
  "Logging": {
    "Folder": "logs",
    "RollingByDate": true,
    "MinimumLevel": "Information"
  },
  "RerunIfChanged": true,
  "WorkingRoot": "",
  "WorkKeepCount": 1
}
```

| Açar | Tip | Default | İzah |
| --- | --- | --- | --- |
| `Language` | string | `"az"` | İlkin dil (installer bunu nəzərə alıb default config kopyalayır). |
| `SourceMode` | string | `"Folder"` | `"Folder"` və ya `"Archive"`. |
| `ArchivePath` | string | `""` | `Archive` rejimində arxiv faylının tam yolu. |
| `ConnectionString` | string | `""` | SQL Server bağlantısı. |
| `ScriptsFolder` | string | `"./sql"` | `Folder` rejimində SQL faylları qovluğu (nisbi ola bilər). |
| `FilePattern` | string | `"*.sql"` | Fayl maskası. |
| `OrderBy` | string | `"LastWriteTimeThenFileNameDate"` | `LastWriteTime`, `FileNameDate`, `LastWriteTimeThenFileNameDate`, `FileNameDateThenLastWriteTime`. |
| `UseCreationTime` | bool | `false` | FS vaxtı üçün *CreationTime* istifadə et. |
| `StopOnError` | bool | `false` | Xətada dayandır. |
| `JournalFile` | string | `"executed.json"` | İcra jurnalı (nisbiysə, *ScriptsFolder*-a nisbətən). |
| `DryRun` | bool | `false` | Test rejimi; SQL icra edilmir. |
| `Logging.Folder` | string | `"logs"` | Log qovluğu (nisbi → *ScriptsFolder*). |
| `Logging.RollingByDate` | bool | `true` | Günə görə yeni log faylı. |
| `Logging.MinimumLevel` | string | `"Information"` | `Debug/Information/Warning/Error/Fatal`. |
| `RerunIfChanged` | bool | `true` | `false` olduqda eyni mətnli skript yenidən icra edilmir (hash əsasında). |
| `WorkingRoot` | string | `""` | Arxiv çıxarma iş qovluqlarının kökü. Boşdursa `C:\ProgramData\ScriptPilot\work`. |
| `WorkKeepCount` | int | `1` | Saxlanılacaq ən yeni iş qovluğu sayı (arxiv çıxarma). |

---

## 🔄 İcra axını (qısa)

1. Mənbəni oxu (Folder → `ScriptsFolder`; Archive → arxivi `WorkingRoot` altına çıxar)  
2. `.sql` fayllarını tap və seçilmiş **OrderBy** ilə sırala  
3. Serilog-u qur və jurnalı yüklə  
4. SQL Server-ə qoşul (`ConnectionString`)  
5. Skriptləri `GO` bölünmələrinə görə ardıcıl icra et  
6. Uğurla icra olunanları jurnala (hash) yaz  
7. Xətada `StopOnError=true` isə dayandır, yoxsa davam et

---

## 🛠️ Troubleshooting

- **Sertifikat/SSL xətası:** test üçün `Encrypt=True;TrustServerCertificate=True`  
- **“Access denied” appsettings.json:** konfiq **ProgramData** altındadır; **Open appsettings.json** düyməsi ilə açın  
- **Loglar gözlənilən qovluğa düşmür:** `Logging.Folder` nisbidirsə, *ScriptsFolder*-a nisbətdir  
- **Archive rejimində disk dolur:** `WorkKeepCount` dəyərini aşağı saxlayın (məs. 1)

---

## 👩‍💻 Developer (Publish & Installer)

**Publish (self-contained, single-file, trimmed, win-x64):**
```bash
dotnet restore
dotnet publish SqlBatchRunner.Win/SqlBatchRunner.Win.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true
```

**Installer (Inno Setup):**
- Skript: `SqlBatchRunner.Win\installer.iss`
- İkon (opsional): `SqlBatchRunner.Win\installer-assets\branding\scriptpilot.ico`
- İlkin config-lər:
  - `SqlBatchRunner.Win\installer-assets\config\appsettings.default.az.json`
  - `SqlBatchRunner.Win\installer-assets\config\appsettings.default.en.json`

Installer dili ingiliscə qalır; tətbiqin dili installer zamanı seçdiyinizə görə **appsettings.json** olaraq kopyalanır.

---

## © Müəlliflik

© 2025 **Jabrayil Shahbazov**. Bütün hüquqlar qorunur.
