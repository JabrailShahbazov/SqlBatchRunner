# ScriptPilot — SQL Runner (Windows, .NET 9)

**ScriptPilot** SQL skriptlərinizi təhlükəsiz və nəzarətli şəkildə ardıcıllıqla icra edən **WinForms (.NET 9)** tətbiqidir.
İstifadəçi üçün sadə UI, adminlər üçün çevik konfiq, əməliyyatçılar üçün **anlıq log** və **jurnal** (təkrar işə düşməsin deyə) təmin edir.

---

## ✨ Xüsusiyyətlər
- **Windows tətbiqi (WinForms, .NET 9)** — responsiv UI, yüksək DPI dəstəyi
- **Konfiq**: `C:\ProgramData\ScriptPilot\appsettings.json` (machine‑wide, yazıla bilir)
- **Batch icrası**: SQL faylları `GO` sətrlərinə görə parçalayıb **transaction** daxilində icra edir
- **Sıralama**: `Date modified` və ya **fayl adına yazılmış tarix** kombinə edilərək
- **Jurnal**: `executed.json` → eyni fayl ikinci dəfə işə düşmür (adla tanıyır)
- **Loglar**: hər gün üçün ayrı fayl (`logs\app-YYYYMMDD.log`), həm UI‑da anlıq, həm diskdə
- **Dry‑Run**: skriptləri oxu və validasiya et, amma icra etmə
- **StopOnError**: səhv olanda dərhal dayan və ya davam et (konfiq ilə)
- **Installer**: Inno Setup ilə `Program Files (x86)`-a quraşdırma, Start Menu və Desktop qısayolu

---

## 🧩 Sistem tələbləri
- Windows 10/11 (x64)
- **.NET Runtime tələb olunmur** — *self‑contained* publish ilə gəlir
- SQL Server (on‑prem və ya VM); əlaqə **Microsoft.Data.SqlClient** vasitəsilə

---

## 📦 Quraşdırma (End‑User üçün)
1. **Setup** faylını işə salın (məs.: `ScriptPilot_Setup_1.0.0.exe`).
2. Quraşdırma yeri (default): `C:\Program Files (x86)\ScriptPilot\`
3. İlk açılışda tətbiq konfiqi **`C:\ProgramData\ScriptPilot\appsettings.json`** kimi yaradacaq
   (əvvəldən yoxdursa).
4. UI daxilində:
   - **Connection String** yazın
   - **Scripts Folder** seçin (SQL fayllarınızın olduğu qovluq)
   - **Save** edin və **Run ▶** düyməsinə basın.
5. Loglar altda real vaxt görünür. **Open Logs** ilə log qovluğunu açın.

> **Qeyd:** Program Files yazıla bilmədiyi üçün konfiq **ProgramData**-da saxlanılır.

---

## ⚙️ Konfiqurasiya (appsettings.json)
Fayl: **`C:\ProgramData\ScriptPilot\appsettings.json`**

```jsonc
{
  "ConnectionString": "Server=YOUR-SQL;Database=YourDb;User ID=sa;Password=Your(!)Pass;Encrypt=True;TrustServerCertificate=True;Connection Timeout=60;MultipleActiveResultSets=True;",
  "ScriptsFolder": ".\\sql",
  "FilePattern": "*.sql",
  "OrderBy": "LastWriteTimeThenFileNameDate",   // LastWriteTime | FileNameDate | LastWriteTimeThenFileNameDate | FileNameDateThenLastWriteTime
  "UseCreationTime": false,                     // true → Date created; false → Date modified
  "StopOnError": false,                         // true → 1-ci səhvdə dayan
  "JournalFile": "executed.json",
  "DryRun": false,
  "Logging": {
    "Folder": "logs",                           // nisbi olarsa ScriptsFolder altında yaradılır
    "RollingByDate": true,                      // app-YYYYMMDD.log
    "MinimumLevel": "Information"               // Info/Warning/Error/Fatal (informativ xarakterlidir)
  }
}
```

### Sıralama qaydaları
- **LastWriteTime** — faylın *Date modified* dəyəri
- **FileNameDate** — fayl adında tarix varsa: `YYYY-MM-DD_*` və ya `YYYYMMDD_*`
- **LastWriteTimeThenFileNameDate** — əvvəlcə *modified time*, eyni olanları *ad tarixi* ilə
- **FileNameDateThenLastWriteTime** — əvvəlcə *ad tarixi*, eyni olanları *modified time* ilə

`UseCreationTime=true` edilsə, *Date created* istifadə olunur.

### Jurnal (executed.json)
- Fayl adı səviyyəsində qeyd aparılır. Eyni fayl adı yenidən işə düşməsin deyə **skip** edilir.
- Jurnalın yolu `JournalFile` **absolute** verilsə ora, yoxdursa `ScriptsFolder` altına yazılır.

### Loglar
- Default: `ScriptsFolder\logs\app-YYYYMMDD.log`
- UI rəngi: **yaşıl = INFO**, **qırmızı = ERR**

---

## ▶️ İş prinsipi
1. `ScriptsFolder` altında `FilePattern`-ə uyğun fayllar toplanır.
2. Seçilmiş **OrderBy** və `UseCreationTime`-a görə sıralanır.
3. Hər fayl `GO` separatoruna görə **batch**-lara bölünür (regex: `^\s*GO\s*;?\s*$`, case‑insensitive).
4. Fayl **transaction** daxilində icra olunur:
   - Batch uğursuz olarsa **rollback**, `StopOnError=true` isə proses dayanır.
   - `DryRun=true` → yalnız analiz, icra yoxdur.
5. Uğurlu fayl **jurnal**-a yazılır (yenidən işə düşməsin).

---

## 🖥️ UI bələdçisi
- **Connection String** – SQL Server bağlantınız.
- **Scripts Folder** – SQL fayllarının qovluğu.
- **Order** – icra sırası (yuxarıda izahı var).
- **Use Creation Time** – *Date created* istifadə et.
- **Stop On Error** – 1-ci səhvdə dayandır.
- **Dry Run** – yalnız analiz et, icra etmə.
- **Save** – `appsettings.json`-u **ProgramData**-da saxlayır.
- **Run ▶** – icranı başlayır (UI donmur).
- **Cancel** – icranı dayandır.
- **Open appsettings.json** – Notepad ilə konfiqi açır.
- **Open Logs** – log qovluğunu File Explorer-də açır.
- Status bar: *Running / Finished (ExitCode=…)*

---

## 🔧 Build (developer üçün)
> Layihə kökü: `SqlBatchRunner.Win\`

### 1) Buildup
```bat
dotnet restore
dotnet build
dotnet run
```

### 2) Publish (self‑contained, tək EXE)
```bat
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```
Nəticə: `bin\Release\net9.0-windows\win-x64\publish\SqlBatchRunner.Win.exe`

### 3) EXE ikonu (branding)
- `branding\scriptpilot.ico` faylını layihəyə əlavə edin.
- `SqlBatchRunner.Win.csproj` daxilində:
```xml
<PropertyGroup>
  <ApplicationIcon>branding\scriptpilot.ico</ApplicationIcon>
</PropertyGroup>
<ItemGroup>
  <Content Include="branding\scriptpilot.ico" />
</ItemGroup>
```
- Form ikonu EXE-dən götürülür:
```csharp
this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
```

---

## 📦 Installer (Inno Setup 6) — quraşdırma paketi
**Şərt**: `installer-assets\` qovluğunda aşağıdakılar olsun:
- `scriptpilot.ico` (setup ikonu)
- `wizard_banner.png` (352×120, modern wizard)
- `wizard_logo.png` (55×55)

**`installer.iss` minimal, işlək nümunə:**
```iss
#define MyAppName "ScriptPilot"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "JabrailShahbaozv"
#define MyAppExeName "SqlBatchRunner.Win.exe"
#define MyPublishDir "bin\\Release\\net9.0-windows\\win-x64\\publish"
#define AssetsDir "installer-assets"
#define MyAppId "{{PUT-YOUR-GUID-HERE}}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={pf32}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputBaseFilename={#MyAppName}_Setup_{#MyAppVersion}
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64
WizardStyle=modern
SetupIconFile={#AssetsDir}\scriptpilot.ico
WizardImageFile={#AssetsDir}\wizard_banner.png
WizardSmallImageFile={#AssetsDir}\wizard_logo.png

[Dirs]
Name: "{commonappdata}\ScriptPilot"; Flags: uninsneveruninstall

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion; Excludes: "appsettings.default.json"
Source: "appsettings.default.json"; DestDir: "{commonappdata}\ScriptPilot"; DestName: "appsettings.json"; Flags: onlyifdoesntexist ignoreversion uninsneveruninstall

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Flags: nowait postinstall skipifsilent
```
**Compile:** Inno Setup Compiler → `installer.iss` → **Compile**  
və ya CLI:
```bat
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "installer.iss"
```

> PNG ilə problem olarsa, `WizardStyle=classic` + BMP ölçüləri (164×314 və 55×55) istifadə edin.

---

## 🔐 Təhlükəsizlik qeydləri
- Connection string-də parol saxlanırsa, fayla **ACL** verin (yalnız adminlər/servis hesabları).
- Mümkünsə **Windows Authentication** istifadə edin.
- `Encrypt=True; TrustServerCertificate=True` **yalnız test** üçündür. Prod-da **etibarlı sertifikat/FQDN** ilə `TrustServerCertificate=False` istifadə edin.
- **Least privilege**: SQL loginə yalnız lazım olan səlahiyyətləri verin.

---

## 🧪 Sınaq üçün “mock” cədvəl
`0001_init.sql`
```sql
IF OBJECT_ID('dbo.DemoOrders') IS NULL
BEGIN
    CREATE TABLE dbo.DemoOrders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderNo NVARCHAR(50),
        CreatedAt DATETIME2 DEFAULT SYSUTCDATETIME()
    );
END
GO

INSERT INTO dbo.DemoOrders (OrderNo) VALUES (N'TEST-001');
GO
```
`0002_index.sql`
```sql
CREATE INDEX IX_DemoOrders_OrderNo ON dbo.DemoOrders(OrderNo);
GO
```

---

## 🛠️ Nasazlıqlar və həll yolları (FAQ)
- **“Access to the path ... Program Files ... is denied”**  
  Konfiq **ProgramData**-ya yazılır. `appsettings.json`-u Program Files-a **qoymayın**.
- **“'/' is an invalid start of a property name”**  
  JSON içində `// comment` olmaz. `appsettings.json`-u saf JSON saxlayın.
- **“The certificate chain was issued by an authority that is not trusted”**  
  Test üçün `TrustServerCertificate=True`. Prod-da **etibarlı CA sertifikatı** istifadə edin.
- **Installer “Bitmap image is not valid”**  
  Inno 6 və `WizardStyle=modern` + PNG ölçüləri (352×120 və 55×55). Klassik üçün BMP.
- **SQL faylları tapılmır**  
  `ScriptsFolder` yolunu və `FilePattern`-i yoxlayın; yol boşluqlu isə dırnaq daxilində yazın.
- **Batch STOP olur**  
  `StopOnError=true` isə 1-ci səhvdə dayanır. Davam üçün `false` edin.
- **Eyni fayl yenidən işləmir**  
  Jurnal (executed.json) fayl adları ilə işarələyir. Yenidən işlətmək üçün adı dəyişin və ya jurnalı silin.

---

## 🔄 Yeniləmə və uninstall
- Yeni versiyanı sadəcə **setup** ilə quraşdırın (üzərinə yazır).
- Uninstall: **Settings → Apps** və ya **Start Menu → ScriptPilot → Uninstall**.
- `C:\ProgramData\ScriptPilot\appsettings.json` default olaraq **silinmir** (konfiqinizi qoruyur). Silmək istəyirsinizsə manual silin və ya installer flaglarını dəyişin.

---

## 📁 Layihə quruluşu
```
SqlBatchRunner.Win/
 ├─ branding/                 # EXE ikon
 │   └─ scriptpilot.ico
 ├─ installer-assets/         # installer üçün banner/logo
 │   ├─ scriptpilot.ico
 │   ├─ wizard_banner.png
 │   └─ wizard_logo.png
 ├─ AppSettings.cs
 ├─ Paths.cs
 ├─ RunnerService.cs
 ├─ MainForm.cs (+ Designer.cs)
 ├─ Program.cs
 ├─ appsettings.default.json
 ├─ SqlBatchRunner.Win.csproj
 └─ installer.iss
```

---

## 📜 Lisenziya və müəllif
- Copyright ©
- Daxili istifadə üçün. Açıq mənbə etmək istəyirsinizsə, uyğun lisenziya əlavə edin.

---

## 🤝 Dəstək
- Konfiq/xəta ekranı, log parçaları və `.iss` faylını paylaşsanız, diaqnostika çox sürətlənər.
- UI/feature təkliflərinizi məmnuniyyətlə əlavə edərik: **Dry‑Run**, **multi‑folder**, **subfolder scan**, **cron‑schedule** və s.

Uğurlu deploymentlər! 🚀
