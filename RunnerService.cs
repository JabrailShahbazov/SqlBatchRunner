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

        public string? LastWorkingDir { get; private set; }

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
                var items = new List<ScriptItem>();
                LastWorkingDir = null;

                if (string.Equals(_cfg.SourceMode, "Archive", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(_cfg.ArchivePath))
                        throw new InvalidOperationException("Archive mode selected but ArchivePath is empty.");

                    Info($"Mode: Archive → {_cfg.ArchivePath}");
                    var (work, extracted) = ArchiveService.ExtractSqlToTemp(_cfg.ArchivePath!, _cfg.WorkingRoot, Info);
                    LastWorkingDir = work;
                    items = extracted;
                }
                else
                {
                    Info($"Mode: Folder → {_cfg.ScriptsFolder}");
                    items = ArchiveService.CollectFromFolder(_cfg.ScriptsFolder, _cfg.FilePattern, Info);
                }

                // Sıfır fayl
                if (items.Count == 0)
                {
                    Info("No .sql files found.");
                    return 0;
                }

                // Sıralama
                items = items
                    .OrderBy(it => ArchiveService.MakeSortKey(it, _cfg.OrderBy, _cfg.UseCreationTime))
                    .ToList();

                // Jurnal
                var journalPath = string.IsNullOrWhiteSpace(_cfg.JournalFile) ? "executed.json"
                                 : (Path.IsPathRooted(_cfg.JournalFile) ? _cfg.JournalFile
                                    : Path.Combine(_cfg.SourceMode.Equals("Archive", StringComparison.OrdinalIgnoreCase) && LastWorkingDir is not null
                                        ? LastWorkingDir : ArchiveService.CollectFromFolder(_cfg.ScriptsFolder, _cfg.FilePattern, _ => { }).Count >= 0 // hack
                                        ? Path.GetFullPath(_cfg.ScriptsFolder) : AppContext.BaseDirectory, _cfg.JournalFile));

                // Əgər Folder modundayıq → jurnalı ScriptsFolder altında saxla.
                // Archive modunda isə jurnalı "EAyni yer"də saxlamaq istəmirsənsə, ProgramData-ya absolute yaz.
                if (_cfg.SourceMode.Equals("Folder", StringComparison.OrdinalIgnoreCase))
                {
                    var root = Path.IsPathRooted(_cfg.ScriptsFolder) ? _cfg.ScriptsFolder : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _cfg.ScriptsFolder));
                    journalPath = Path.IsPathRooted(_cfg.JournalFile) ? _cfg.JournalFile : Path.Combine(root, _cfg.JournalFile);
                }
                else
                {
                    // Archive mode: default — ProgramData\ScriptPilot\journal\executed.json
                    if (!Path.IsPathRooted(_cfg.JournalFile))
                    {
                        var jdir = Path.Combine(Paths.ProgramDataDir, "journal");
                        Directory.CreateDirectory(jdir);
                        journalPath = Path.Combine(jdir, _cfg.JournalFile);
                    }
                }

                var journal = Journal.Load(journalPath);
                Info($"Journal: {journalPath}");

                using var conn = new SqlConnection(_cfg.ConnectionString);
                conn.InfoMessage += (s, e) => { if (!string.IsNullOrWhiteSpace(e.Message)) Info("SQL INFO: " + e.Message.Trim()); };
                await conn.OpenAsync(ct);

                foreach (var it in items)
                {
                    ct.ThrowIfCancellationRequested();

                    var fileKey = it.JournalKey;
                    if (journal.IsExecuted(fileKey, it.Sha256, _cfg.RerunIfChanged))
                    {
                        Info($"[SKIP] {fileKey} already executed (hash match).");
                        continue;
                    }

                    string sql;
                    try
                    {
                        sql = await File.ReadAllTextAsync(it.PhysicalPath, Encoding.UTF8, ct);
                    }
                    catch (Exception exRead)
                    {
                        Err($"[ERR ] FILE_READ_ERROR {fileKey}: {exRead.Message}");
                        if (_cfg.StopOnError) return 1;
                        continue;
                    }

                    var batches = SplitIntoBatches(sql).ToList();
                    Info($"[RUN ] {fileKey} Batches={batches.Count}");

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
                            journal.MarkExecuted(fileKey, it.Sha256);
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

        [GeneratedRegex(@"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
        private static partial Regex GoSplitterRegex();

        private static IEnumerable<string> SplitIntoBatches(string sql) =>
            GoSplitterRegex().Split(sql).Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s));

        private void Info(string m) { _logInfo(m); WriteFileLog("INFO", m); }
        private void Err(string m) { _logError(m); WriteFileLog("ERROR", m); }

        private void WriteFileLog(string level, string message)
        {
            try
            {
                var scripts = _cfg.SourceMode.Equals("Archive", StringComparison.OrdinalIgnoreCase)
                    ? (LastWorkingDir ?? Paths.ProgramDataDir) // arxiv çıxarışı üçün
                    : (Path.IsPathRooted(_cfg.ScriptsFolder) ? _cfg.ScriptsFolder : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _cfg.ScriptsFolder)));

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
