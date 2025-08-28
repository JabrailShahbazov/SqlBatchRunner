using Microsoft.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace SqlBatchRunner.Win
{
    public sealed partial class RunnerService
    {
        private readonly AppSettings _cfg;
        private readonly Action<string> _logInfo;
        private readonly Action<string> _logError;

        public RunnerService(AppSettings cfg, Action<string> logInfo, Action<string> logError)
        {
            _cfg = cfg;
            _logInfo = logInfo;
            _logError = logError;
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            try
            {
                var scriptsFolder = ResolvePath(_cfg.ScriptsFolder);
                if (!Directory.Exists(scriptsFolder))
                    throw new DirectoryNotFoundException($"Scripts folder not found: {scriptsFolder}");

                var files = Directory.EnumerateFiles(scriptsFolder, _cfg.FilePattern, SearchOption.TopDirectoryOnly)
                                     .OrderBy(f => GetOrderKeyComposite(f, _cfg.OrderBy, _cfg.UseCreationTime))
                                     .ToList();

                if (!files.Any())
                {
                    Info("No .sql files found.");
                    return 0;
                }

                var journalPath = Path.IsPathRooted(_cfg.JournalFile)
                    ? _cfg.JournalFile
                    : Path.Combine(scriptsFolder, _cfg.JournalFile);

                var journal = Journal.Load(journalPath);

                using var conn = new SqlConnection(_cfg.ConnectionString);
                conn.InfoMessage += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Message)) Info("SQL INFO: " + e.Message.Trim()); };
                await conn.OpenAsync(ct);

                foreach (var file in files)
                {
                    ct.ThrowIfCancellationRequested();

                    var fi = new FileInfo(file);
                    var fileKey = fi.Name;

                    if (journal.ExecutedFiles.Contains(fileKey))
                    {
                        Info($"[SKIP] {fileKey} already executed.");
                        continue;
                    }

                    string sql;
                    try
                    {
                        sql = await File.ReadAllTextAsync(file, Encoding.UTF8, ct);
                    }
                    catch (Exception exRead)
                    {
                        Err($"[ERR ] FILE_READ_ERROR {fileKey}: {exRead.Message}");
                        if (_cfg.StopOnError) return 1;
                        continue;
                    }

                    var batches = SplitIntoBatches(sql).ToList();
                    Info($"[RUN ] {fileKey} Batches={batches.Count} Size={fi.Length / 1024}KB");

                    if (_cfg.DryRun)
                    {
                        Info($"[DRY ] {fileKey} — validation only.");
                        continue;
                    }

                    var swFile = Stopwatch.StartNew();
                    using var tx = conn.BeginTransaction();
                    var fileOk = true;

                    try
                    {
                        for (int i = 0; i < batches.Count; i++)
                        {
                            ct.ThrowIfCancellationRequested();

                            var batch = batches[i];
                            var sw = Stopwatch.StartNew();
                            try
                            {
                                using var cmd = new SqlCommand(batch, conn, tx) { CommandTimeout = 0 };
                                var affected = await cmd.ExecuteNonQueryAsync(ct);
                                sw.Stop();
                                Info($"[OK  ] {fileKey} Batch={i + 1}/{batches.Count} Ms={sw.ElapsedMilliseconds} Affected={affected}");
                            }
                            catch (Exception exBatch)
                            {
                                sw.Stop();
                                fileOk = false;
                                Err($"[ERR ] BATCH_ERROR {fileKey} Batch={i + 1}: {exBatch.Message}\nSNIPPET: {GetSnippet(batch)}");
                                try { tx.Rollback(); } catch { /* ignore */ }
                                break;
                            }
                        }

                        if (fileOk)
                        {
                            tx.Commit();
                            journal.ExecutedFiles.Add(fileKey);
                            journal.Save(journalPath);
                            swFile.Stop();
                            Info($"[OK  ] {fileKey} Batches={batches.Count} TotalMs={swFile.ElapsedMilliseconds}");
                        }
                    }
                    catch (Exception exFile)
                    {
                        fileOk = false;
                        Err($"[ERR ] FILE_ERROR {fileKey}: {exFile.Message}");
                        try { tx.Rollback(); } catch { /* ignore */ }
                    }

                    if (!fileOk && _cfg.StopOnError)
                    {
                        Info("StopOnError=true → stopping.");
                        return 1;
                    }
                }

                Info("Done.");
                return 0;
            }
            catch (OperationCanceledException)
            {
                Info("Canceled.");
                return 3;
            }
            catch (Exception ex)
            {
                Err("Fatal: " + ex);
                return 2;
            }
        }

        private static string ResolvePath(string path)
        {
            if (Path.IsPathRooted(path)) return path;
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
        }

        [GeneratedRegex(@"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
        private static partial Regex GoSplitterRegex();

        private static IEnumerable<string> SplitIntoBatches(string sql) =>
            GoSplitterRegex().Split(sql).Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s));

        private static (DateTime k1, DateTime k2, string k3) GetOrderKeyComposite(string path, string orderBy, bool useCreationTime)
        {
            var fi = new FileInfo(path);
            var modified = fi.LastWriteTimeUtc;
            var created = fi.CreationTimeUtc;
            var fileTime = useCreationTime ? created : modified;

            var name = fi.Name;
            var nameDate = TryParseDateFromName(name) ?? DateTime.MinValue;

            return orderBy switch
            {
                "LastWriteTime" => (fileTime, DateTime.MinValue, name),
                "FileNameDate" => (nameDate, DateTime.MinValue, name),
                "LastWriteTimeThenFileNameDate" => (fileTime, nameDate, name),
                "FileNameDateThenLastWriteTime" => (nameDate, fileTime, name),
                _ => (fileTime, DateTime.MinValue, name)
            };
        }

        private static DateTime? TryParseDateFromName(string fileName)
        {
            var n = Path.GetFileNameWithoutExtension(fileName);

            var m1 = Regex.Match(n, @"(?<!\d)(\d{4})[-_](\d{2})[-_](\d{2})(?!\d)");
            if (m1.Success && DateTime.TryParse($"{m1.Groups[1].Value}-{m1.Groups[2].Value}-{m1.Groups[3].Value}", out var d1))
                return d1;

            var m2 = Regex.Match(n, @"(?<!\d)(\d{4})(\d{2})(\d{2})(?!\d)");
            if (m2.Success && DateTime.TryParse($"{m2.Groups[1].Value}-{m2.Groups[2].Value}-{m2.Groups[3].Value}", out var d2))
                return d2;

            return null;
        }

        private void Info(string m) { _logInfo(m); WriteFileLog("INFO", m); }
        private void Err(string m) { _logError(m); WriteFileLog("ERROR", m); }

        private void WriteFileLog(string level, string message)
        {
            try
            {
                var scripts = ResolvePath(_cfg.ScriptsFolder);
                var logsRoot = _cfg.Logging?.Folder ?? "logs";
                var logsPath = Path.IsPathRooted(logsRoot) ? logsRoot : Path.Combine(scripts, logsRoot);
                Directory.CreateDirectory(logsPath);

                var file = (_cfg.Logging?.RollingByDate ?? true)
                    ? Path.Combine(logsPath, $"app-{DateTime.Now:yyyyMMdd}.log")
                    : Path.Combine(logsPath, "app.log");

                File.AppendAllText(file, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}{Environment.NewLine}");
            }
            catch { /* ignore */ }
        }

        private static string GetSnippet(string sql, int maxLen = 300)
        {
            var s = sql.Replace("\r", " ").Replace("\n", " ").Trim();
            return s.Length <= maxLen ? s : s[..maxLen] + "...";
        }
    }
}
