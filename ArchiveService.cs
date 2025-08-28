using SharpCompress.Archives;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SqlBatchRunner.Win
{
    public sealed record ScriptItem(
        string PhysicalPath,
        string JournalKey,
        string DisplayName,
        DateTime LastWriteTimeUtc,
        DateTime CreationTimeUtc,
        string Sha256
    );

    public static class ArchiveService
    {
        /// <summary>
        /// Arxivi (zip/rar/7z/tar/gz və s.) müvəqqəti iş qovluğuna çıxarır, yalnız .sql faylları qaytarır.
        /// </summary>
        public static (string WorkingDir, List<ScriptItem> Items) ExtractSqlToTemp(
            string archivePath,
            string? workingRootOrNull,
            Action<string> log)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException("Archive not found", archivePath);

            var workRoot = string.IsNullOrWhiteSpace(workingRootOrNull)
                ? Path.Combine(Paths.ProgramDataDir, "work")
                : workingRootOrNull!;

            Directory.CreateDirectory(workRoot);

            var runId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N")[..6];
            var workingDir = Path.Combine(workRoot, runId);
            Directory.CreateDirectory(workingDir);

            var items = new List<ScriptItem>();
            var archiveName = Path.GetFileName(archivePath);

            using var arc = SharpCompress.Archives.ArchiveFactory.Open(archivePath);
            foreach (var entry in arc.Entries.Where(e => !e.IsDirectory))
            {
                if (!entry.Key.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)) continue;

                var safeRel = Sanitize(entry.Key);
                var destPath = Path.Combine(workingDir, safeRel);
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                log($"Extract: {entry.Key} -> {MakeRel(workingDir, destPath)}");

                using (var s = entry.OpenEntryStream())
                using (var fs = File.Create(destPath))
                {
                    s.CopyTo(fs);
                    fs.Flush();
                }

                // Arxiv metadata-sından gələn vaxtı təhlükəsiz şəkildə UTC-yə çevir
                var lm = entry.LastModifiedTime ?? DateTime.UtcNow;
                DateTime lmUtc =
                    lm.Kind == DateTimeKind.Utc ? lm :
                    lm.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(lm, DateTimeKind.Utc) :
                    lm.ToUniversalTime();

                try { File.SetLastWriteTimeUtc(destPath, lmUtc); } catch { /* ignore */ }
                try { File.SetCreationTimeUtc(destPath, lmUtc); } catch { /* ignore */ }

                var hash = ComputeSHA256(destPath);
                var display = Path.GetFileName(destPath);
                var journalKey = $"{archiveName}::{safeRel.Replace('\\', '/')}";

                items.Add(new ScriptItem(
                    destPath,
                    journalKey,
                    display,
                    File.GetLastWriteTimeUtc(destPath),
                    File.GetCreationTimeUtc(destPath),
                    hash
                ));
            }

            return (workingDir, items);
        }

        /// <summary>
        /// Qovluqdan rekursiv .sql faylları toplayır.
        /// </summary>
        public static List<ScriptItem> CollectFromFolder(string folder, string filePattern, Action<string> log)
        {
            var root = Resolve(folder);
            if (!Directory.Exists(root))
                throw new DirectoryNotFoundException($"Scripts folder not found: {root}");

            var files = Directory.EnumerateFiles(root, filePattern, SearchOption.AllDirectories).ToList();
            var list = new List<ScriptItem>();
            foreach (var f in files)
            {
                var rel = MakeRel(root, f).Replace('\\', '/');
                var key = rel;
                var fi = new FileInfo(f);
                var hash = ComputeSHA256(f);

                list.Add(new ScriptItem(
                    f, key, fi.Name, fi.LastWriteTimeUtc, fi.CreationTimeUtc, hash));
            }
            return list;
        }

        /// <summary>
        /// Sıralama üçün açar: fayl vaxtı + ad daxilində tarix (əgər varsa).
        /// </summary>
        public static (DateTime k1, DateTime k2, string k3) MakeSortKey(ScriptItem it, string orderBy, bool useCreationTime)
        {
            var fileTime = useCreationTime ? it.CreationTimeUtc : it.LastWriteTimeUtc;
            var nameDate = TryParseDateFromName(it.DisplayName) ?? DateTime.MinValue;

            return orderBy switch
            {
                "LastWriteTime" => (fileTime, DateTime.MinValue, it.DisplayName),
                "FileNameDate" => (nameDate, DateTime.MinValue, it.DisplayName),
                "LastWriteTimeThenFileNameDate" => (fileTime, nameDate, it.DisplayName),
                "FileNameDateThenLastWriteTime" => (nameDate, fileTime, it.DisplayName),
                _ => (fileTime, DateTime.MinValue, it.DisplayName)
            };
        }

        /// <summary>
        /// Fayl adından tarixi çıxarır (ilk uyğunluq qaytarılır).
        /// Dəstəklənən formatlar (separator: -, _, ., *boşluq*):
        ///   YYYY-MM-DD | YYYY_MM_DD | YYYY.MM.DD | YYYY MM DD | YYYYMMDD
        ///   DD-MM-YYYY | DD_MM_YYYY | DD.MM.YYYY | DD MM YYYY | DDMMYYYY
        /// </summary>
        public static DateTime? TryParseDateFromName(string fileName)
        {
            var n = Path.GetFileNameWithoutExtension(fileName);

            // Separator: '-', '_', '.', WHITESPACE (bir və ya daha çox)
            const string sep = @"[-_.\s]+";

            // 1) ISO tərzi (YYYY sep MM sep DD) – ambiq deyil, prioritetlə yoxlayırıq
            var mIso = Regex.Match(n, $@"(?<!\d)(\d{{4}}){sep}(\d{{2}}){sep}(\d{{2}})(?!\d)",
                                   RegexOptions.CultureInvariant);
            if (mIso.Success &&
                TryMakeDate(int.Parse(mIso.Groups[1].Value), int.Parse(mIso.Groups[2].Value), int.Parse(mIso.Groups[3].Value), out var dtIso))
                return dtIso;

            // 2) Bitişik (YYYYMMDD)
            var mIsoTight = Regex.Match(n, @"(?<!\d)(\d{4})(\d{2})(\d{2})(?!\d)",
                                        RegexOptions.CultureInvariant);
            if (mIsoTight.Success &&
                TryMakeDate(int.Parse(mIsoTight.Groups[1].Value), int.Parse(mIsoTight.Groups[2].Value), int.Parse(mIsoTight.Groups[3].Value), out var dtIsoT))
                return dtIsoT;

            // 3) Lokal tərz (DD sep MM sep YYYY) – ambiq ola bilər, ISO-dan sonra
            var mDmY = Regex.Match(n, $@"(?<!\d)(\d{{2}}){sep}(\d{{2}}){sep}(\d{{4}})(?!\d)",
                                   RegexOptions.CultureInvariant);
            if (mDmY.Success &&
                TryMakeDate(int.Parse(mDmY.Groups[3].Value), int.Parse(mDmY.Groups[2].Value), int.Parse(mDmY.Groups[1].Value), out var dtDmY))
                return dtDmY;

            // 4) Bitişik (DDMMYYYY)
            var mDmYTight = Regex.Match(n, @"(?<!\d)(\d{2})(\d{2})(\d{4})(?!\d)",
                                        RegexOptions.CultureInvariant);
            if (mDmYTight.Success &&
                TryMakeDate(int.Parse(mDmYTight.Groups[3].Value), int.Parse(mDmYTight.Groups[2].Value), int.Parse(mDmYTight.Groups[1].Value), out var dtDmYT))
                return dtDmYT;

            return null;
        }

        /// <summary>Y/M/D intervallarını yoxlayıb etibarlı tarix yaradır.</summary>
        private static bool TryMakeDate(int y, int m, int d, out DateTime dt)
        {
            dt = default;
            if (y is < 1900 or > 9999) return false;
            if (m is < 1 or > 12) return false;
            var maxD = DateTime.DaysInMonth(y, m);
            if (d < 1 || d > maxD) return false;

            dt = new DateTime(y, m, d);
            return true;
        }

        private static string Resolve(string p) =>
            Path.IsPathRooted(p) ? p : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, p));

        private static string Sanitize(string key)
        {
            var rel = key.Replace('/', '\\').TrimStart('\\');
            if (rel.Contains("..")) rel = rel.Replace("..", "__"); // path traversal qarşısı
            return rel;
        }

        public static string ComputeSHA256(string filePath)
        {
            using var sha = SHA256.Create();
            using var fs = File.OpenRead(filePath);
            var hash = sha.ComputeHash(fs);
            return Convert.ToHexString(hash);
        }

        private static string MakeRel(string root, string path)
        {
            var r = Path.GetFullPath(root).TrimEnd('\\') + "\\";
            var p = Path.GetFullPath(path);
            if (p.StartsWith(r, StringComparison.OrdinalIgnoreCase))
                return p.Substring(r.Length);
            return path;
        }
    }
}
