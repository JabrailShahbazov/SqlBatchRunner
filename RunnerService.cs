// SqlBatchRunner.Win/RunnerService.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Serilog;

namespace SqlBatchRunner.Win
{
    /// <summary>
    /// SQL skriptlərinin icrası, sıralanması, jurnal, loglama.
    /// </summary>
    public sealed class RunnerService
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
            int errorCount = 0;

            // 1) Mənbədən skriptləri topla
            string baseScriptsDir;
            List<ScriptItem> items;

            if (string.Equals(_cfg.SourceMode, "Archive", StringComparison.OrdinalIgnoreCase))
            {
                // köhnə work qovluqlarını təmizlə
                var keepExisting = Math.Max(0, _cfg.WorkKeepCount - 1);
                ArchiveService.CleanupWorkRoot(Paths.WorkRoot, keepExisting, _logInfo);

                var (workDir, list) = ArchiveService.ExtractSqlToTemp(_cfg.ArchivePath!, _cfg.WorkingRoot, _logInfo);
                LastWorkingDir = workDir;

                baseScriptsDir = workDir;
                items = list;
            }
            else
            {
                baseScriptsDir = ToAbsoluteScriptsFolder(_cfg.ScriptsFolder);
                items = Directory
                    .EnumerateFiles(baseScriptsDir, _cfg.FilePattern ?? "*.sql", SearchOption.AllDirectories)
                    .Select(p => new FileInfo(p))
                    .Select(fi =>
                    {
                        DateTime? fnDate = ArchiveService.TryParseDateFromName(fi.Name, out var dt) ? dt : null;
                        return new ScriptItem
                        {
                            Path = fi.FullName,
                            Name = fi.Name,
                            LastWrite = fi.LastWriteTimeUtc,
                            Creation = fi.CreationTimeUtc,
                            FileNameDate = fnDate
                        };
                    })
                    .ToList();
                LastWorkingDir = null;
            }

            if (items.Count == 0)
            {
                _logInfo("[INFO] No SQL files found.");
                return 0;
            }

            // 2) Sıralama
            items = Order(items, _cfg.OrderBy, _cfg.UseCreationTime);

            // 3) Log qur
            var logsRoot = GetLogsRoot(_cfg);
            Directory.CreateDirectory(logsRoot);
            Log.Logger = BuildLogger(_cfg, logsRoot);

            // 4) Jurnal (executed.json) - ScriptsFolder bazasında saxlanır
            var journalPath = Path.IsPathRooted(_cfg.JournalFile)
                ? _cfg.JournalFile
                : Path.Combine(ToAbsoluteScriptsFolder(_cfg.ScriptsFolder), _cfg.JournalFile);
            var journal = LoadJournal(journalPath);

            // 5) SQL-ə qoşul
            using var conn = new SqlConnection(_cfg.ConnectionString);
            await conn.OpenAsync(ct);

            foreach (var it in items)
            {
                ct.ThrowIfCancellationRequested();

                var sqlText = await File.ReadAllTextAsync(it.Path, ct);
                var hash = ComputeHash(sqlText);

                if (!_cfg.DryRun && !_cfg.RerunIfChanged && journal.TryGetValue(it.Name, out var oldHash) && oldHash == hash)
                {
                    _logInfo($"[SKIP] {it.Name} — already executed (unchanged).");
                    continue;
                }

                _logInfo($"[RUN ] {it.Name}");

                try
                {
                    if (_cfg.DryRun)
                    {
                        _logInfo("       (dry run)");
                    }
                    else
                    {
                        await ExecuteBatchesAsync(conn, sqlText, ct);
                        journal[it.Name] = hash;
                        SaveJournal(journalPath, journal);
                    }

                    Log.Information("Executed {File}", it.Name);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logError($"[FAIL] {it.Name} :: {ex.Message}");
                    Log.Error(ex, "Failed {File}", it.Name);

                    if (_cfg.StopOnError)
                        break;
                }
            }

            Log.CloseAndFlush();
            return errorCount == 0 ? 0 : 1;
        }

        // ============== Helpers ==============

        private static List<ScriptItem> Order(List<ScriptItem> list, string? mode, bool useCreation)
        {
            var m = (mode ?? "LastWriteTimeThenFileNameDate").Trim();

            DateTime GetFsTime(ScriptItem s) => useCreation ? s.Creation : s.LastWrite;
            DateTime FnOrMin(ScriptItem s) => s.FileNameDate ?? DateTime.MinValue;

            return m switch
            {
                "LastWriteTime" => list.OrderBy(GetFsTime).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                "FileNameDate" => list.OrderBy(FnOrMin).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                "FileNameDateThenLastWriteTime" => list
                    .OrderBy(FnOrMin).ThenBy(GetFsTime).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList(),
                _ => list // LastWriteTimeThenFileNameDate (default)
                    .OrderBy(GetFsTime).ThenBy(FnOrMin).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList()
            };
        }

        private static string ToAbsoluteScriptsFolder(string value)
        {
            if (Path.IsPathRooted(value)) return value;
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, value));
        }

        private static string GetLogsRoot(AppSettings cfg)
        {
            var baseDir = ToAbsoluteScriptsFolder(cfg.ScriptsFolder);
            var folder = cfg.Logging?.Folder ?? "logs";
            return Path.IsPathRooted(folder) ? folder : Path.Combine(baseDir, folder);
        }

        // RunnerService.cs içində
        private static ILogger BuildLogger(AppSettings cfg, string logsRoot)
        {
            var level = (cfg.Logging?.MinimumLevel ?? "Information").Trim().ToLowerInvariant();

            var lc = new LoggerConfiguration();
            lc = level switch
                 {
                     "debug"   => lc.MinimumLevel.Debug(),
                     "warning" => lc.MinimumLevel.Warning(),
                     "error"   => lc.MinimumLevel.Error(),
                     "fatal"   => lc.MinimumLevel.Fatal(),
                     _         => lc.MinimumLevel.Information()
                 };

            // Oxunaqlı vaxt şablonu
            const string template = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}";

            if (cfg.Logging?.RollingByDate == true)
            {
                // DİQQƏT: "app-.log" -> Serilog avtomatik "app-YYYYMMDD.log" yaradacaq
                var path = Path.Combine(logsRoot, "app-.log");
                return lc
                       .WriteTo.File(
                                     path,
                                     rollingInterval: RollingInterval.Day,
                                     retainedFileCountLimit: 31,   // istəsən dəyiş
                                     shared: true,
                                     outputTemplate: template)
                       .CreateLogger();
            }
            else
            {
                var path = Path.Combine(logsRoot, "app.log");
                return lc
                       .WriteTo.File(
                                     path,
                                     rollingInterval: RollingInterval.Infinite,
                                     shared: true,
                                     outputTemplate: template)
                       .CreateLogger();
            }
        }

        private static async Task ExecuteBatchesAsync(SqlConnection conn, string fullScript, CancellationToken ct)
        {
            // "GO" ilə parçalama (sətir başlanğıcında, şərhdə deyil)
            var parts = SplitByGo(fullScript);
            foreach (var sql in parts)
            {
                var text = sql.Trim();
                if (text.Length == 0) continue;

                using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = text;
                cmd.CommandTimeout = 0; // limitsiz
                await cmd.ExecuteNonQueryAsync(ct);
            }
        }

        private static List<string> SplitByGo(string script)
        {
            var lines = new List<string>();
            var sb = new StringBuilder();

            var rxGo = new Regex(@"^\s*GO\s*(?:--.*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

            int last = 0;
            foreach (Match m in rxGo.Matches(script))
            {
                sb.Append(script, last, m.Index - last);
                lines.Add(sb.ToString());
                sb.Clear();
                last = m.Index + m.Length;
            }
            sb.Append(script, last, script.Length - last);
            lines.Add(sb.ToString());
            return lines;
        }

        private static string ComputeHash(string text)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(text);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }

        private static Dictionary<string, string> LoadJournal(string path)
        {
            try
            {
                if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var json = File.ReadAllText(path);
                var obj = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return obj ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static void SaveJournal(string path, Dictionary<string, string> data)
        {
            var dir = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
