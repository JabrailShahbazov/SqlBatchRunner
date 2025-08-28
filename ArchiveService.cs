// SqlBatchRunner.Win/ArchiveService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

// RAR/7z/tar üçün
using SharpCompress.Archives;
using SharpCompress.Common;

namespace SqlBatchRunner.Win
{
    /// <summary>
    /// Arxiv emalı və SQL fayllarının toplanması üçün util.
    /// </summary>
    public static class ArchiveService
    {
        private static readonly Regex _rxDate =
            new Regex(@"(?<!\d)(20\d{2})[-_ ]?(0[1-9]|1[0-2])[-_ ]?(0[1-9]|[12]\d|3[01])(?:[ _-]?((?:[01]\d|2[0-3]))[:_-]?([0-5]\d)(?:[:_-]?([0-5]\d))?)?",
                      RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Arxivi {workRoot}\yyyyMMdd_HHmmss_xxxxx altına çıxardır və oradakı .sql fayllarını qaytarır.
        /// </summary>
        public static (string WorkingDir, List<ScriptItem> Items) ExtractSqlToTemp(
            string archivePath,
            string? workingRootOrNull,
            Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
                throw new FileNotFoundException("Archive not found", archivePath);

            var workRoot = workingRootOrNull;
            if (string.IsNullOrWhiteSpace(workRoot))
                workRoot = Paths.WorkRoot;

            Directory.CreateDirectory(workRoot!);

            var stamp = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 5)}";
            var workingDir = Path.Combine(workRoot!, stamp);
            Directory.CreateDirectory(workingDir);

            var ext = Path.GetExtension(archivePath).ToLowerInvariant();

            log($"[INFO] Extracting archive: {archivePath} -> {workingDir}");

            if (ext == ".zip")
            {
                ZipFile.ExtractToDirectory(archivePath, workingDir, overwriteFiles: true);
            }
            else
            {
                // SharpCompress: rar/7z/tar/gz...
                using var archive = ArchiveFactory.Open(archivePath);
                foreach (var entry in archive.Entries.Where(e => !e.IsDirectory))
                {
                    var outPath = Path.Combine(workingDir, entry.Key.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                    entry.WriteToFile(outPath, new ExtractionOptions()
                    {
                        Overwrite = true,
                        ExtractFullPath = true
                    });
                }
            }

            var sqls = Directory
                .EnumerateFiles(workingDir, "*.sql", SearchOption.AllDirectories)
                .Select(p => ToItem(p))
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            log($"[INFO] Extracted {sqls.Count} SQL file(s).");
            return (workingDir, sqls);
        }

        /// <summary>
        /// work kökündə köhnə run qovluqlarını silir — yalnız ən yeni keepCount qalır.
        /// </summary>
        public static void CleanupWorkRoot(string workRoot, int keepCount, Action<string> log)
        {
            try { Directory.CreateDirectory(workRoot); } catch { /* ignore */ }

            var list = Directory.Exists(workRoot)
                ? Directory.GetDirectories(workRoot)
                    .Select(p => new DirectoryInfo(p))
                    .OrderByDescending(d => d.CreationTimeUtc)
                    .ToList()
                : new List<DirectoryInfo>();

            for (int i = keepCount; i < list.Count; i++)
                TryDeleteDir(list[i].FullName, log);
        }

        private static void TryDeleteDir(string path, Action<string> log)
        {
            try
            {
                if (!Directory.Exists(path)) return;

                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { File.SetAttributes(f, FileAttributes.Normal); } catch { /* ignore */ }
                }

                Directory.Delete(path, true);
                log($"[INFO] Old work dir removed: {path}");
            }
            catch (Exception ex)
            {
                log($"[WARN] Couldn't delete '{path}': {ex.Message}");
            }
        }

        private static ScriptItem ToItem(string filePath)
        {
            var fi = new FileInfo(filePath);
            DateTime? fnDate = null;
            if (TryParseDateFromName(fi.Name, out var dt)) fnDate = dt;

            return new ScriptItem
            {
                Path = fi.FullName,
                Name = fi.Name,
                LastWrite = fi.LastWriteTimeUtc,
                Creation = fi.CreationTimeUtc,
                FileNameDate = fnDate
            };
        }

        /// <summary>
        /// Fayl adından tarix (və mümkünsə saat) çıxarır. Məs: 20250820.sql, 2025-08-20 173455_x.sql və s.
        /// </summary>
        public static bool TryParseDateFromName(string fileName, out DateTime value)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            var m = _rxDate.Match(name);
            if (!m.Success)
            {
                value = default;
                return false;
            }

            int y = int.Parse(m.Groups[1].Value);
            int M = int.Parse(m.Groups[2].Value);
            int d = int.Parse(m.Groups[3].Value);
            int hh = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : 0;
            int mm = m.Groups[5].Success ? int.Parse(m.Groups[5].Value) : 0;
            int ss = m.Groups[6].Success ? int.Parse(m.Groups[6].Value) : 0;

            value = new DateTime(y, M, d, hh, mm, ss, DateTimeKind.Utc);
            return true;
        }
    }

    /// <summary>
    /// SQL faylı haqqında meta.
    /// </summary>
    public sealed class ScriptItem
    {
        public string Path { get; set; } = default!;
        public string Name { get; set; } = default!;
        public DateTime LastWrite { get; set; }
        public DateTime Creation { get; set; }
        public DateTime? FileNameDate { get; set; }
    }
}
