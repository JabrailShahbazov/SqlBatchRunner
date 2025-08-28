using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Serilog;

namespace SqlBatchRunner.Win
{
    public class RunnerService
    {
        private readonly AppSettings _cfg;
        private readonly Action<string> _info;
        private readonly Action<string> _err;

        private ILogger? _fileLogger;
        public string? LastWorkingDir { get; private set; }

        public RunnerService(AppSettings cfg, Action<string> info, Action<string> err)
        {
            _cfg = cfg;
            _info = msg => { info(msg); _fileLogger?.Information(msg); };
            _err  = msg => { err(msg);  _fileLogger?.Error(msg);       };
        }

        // ==== PUBLIC ====
        public async Task<int> RunAsync(CancellationToken ct)
        {
            try
            {
                InitFileLogger();

                // 1) Skriptləri topla
                List<ScriptItem> items;
                if (string.Equals(_cfg.SourceMode, "Archive", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(_cfg.ArchivePath))
                        throw new InvalidOperationException("Archive mode seçilib, amma ArchivePath boşdur.");

                    var (workDir, list) = ArchiveService.ExtractSqlToTemp(_cfg.ArchivePath!, _cfg.WorkingRoot ?? "", _info);
                    LastWorkingDir = workDir; // Open Work Dir üçün
                    items = list;
                }
                else
                {
                    items = ArchiveService.CollectFromFolder(_cfg.ScriptsFolder ?? ".\\sql", _cfg.FilePattern ?? "*.sql", _info);
                }

                if (items.Count == 0)
                {
                    _info("Heç bir .sql tapılmadı.");
                    return 0;
                }

                // 2) Sort
                var useCreation = _cfg.UseCreationTime;
                items = items
                    .OrderBy(it =>
                    {
                        var k = ArchiveService.MakeSortKey(it, _cfg.OrderBy ?? "LastWriteTimeThenFileNameDate", useCreation);
                        return (k.k1, k.k2, k.k3);
                    })
                    .ToList();

                // 3) Jurnal yeri (HƏMİŞƏ ScriptsFolder altında!)
                var scriptsRoot = ToAbsoluteScriptsFolder(_cfg.ScriptsFolder ?? ".\\sql");
                Directory.CreateDirectory(scriptsRoot);
                var journalPath = Path.IsPathRooted(_cfg.JournalFile ?? "")
                    ? _cfg.JournalFile!
                    : Path.Combine(scriptsRoot, _cfg.JournalFile ?? "executed.json");

                var journal = LoadJournal(journalPath);

                // 4) SQL bağlantısını hazırla
                using var conn = new SqlConnection(_cfg.ConnectionString);
                await conn.OpenAsync(ct);

                // 5) İcra et
                int ok = 0, skipped = 0, failed = 0;
                foreach (var it in items)
                {
                    ct.ThrowIfCancellationRequested();

                    var already = journal.TryGetValue(it.JournalKey, out var j)
                                  ? j
                                  : null;

                    // rerun qaydası
                    var shallRun = already is null || (_cfg.RerunIfChanged && !string.Equals(already.Sha256, it.Sha256, StringComparison.OrdinalIgnoreCase));

                    if (!shallRun)
                    {
                        skipped++;
                        _info($"SKIP: {it.DisplayName} (jurnal: {already!.ExecutedAtUtc:yyyy-MM-dd HH:mm:ss} UTC)");
                        continue;
                    }

                    try
                    {
                        var sql = await File.ReadAllTextAsync(it.PhysicalPath, ct);
                        var batches = SplitSqlBatches(sql);

                        _info($"RUN : {it.DisplayName}  (batches={batches.Count})");

                        if (!_cfg.DryRun)
                        {
                            foreach (var b in batches)
                            {
                                using var cmd = new SqlCommand(b, conn) { CommandType = CommandType.Text, CommandTimeout = 0 };
                                await cmd.ExecuteNonQueryAsync(ct);
                            }
                        }

                        // jurnal
                        journal[it.JournalKey] = new JournalEntry
                        {
                            Key = it.JournalKey,
                            DisplayName = it.DisplayName,
                            Sha256 = it.Sha256,
                            ExecutedAtUtc = DateTime.UtcNow
                        };
                        SaveJournal(journalPath, journal);

                        ok++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        _err($"FAIL: {it.DisplayName} -> {ex.Message}");
                        if (_cfg.StopOnError) break;
                    }
                }

                _info($"Netice: OK={ok}, Skipped={skipped}, Failed={failed}");
                return failed == 0 ? 0 : 1;
            }
            catch (OperationCanceledException)
            {
                _err("Ləğv edildi.");
                return 2;
            }
            catch (Exception ex)
            {
                _err("Fatal: " + ex.Message);
                return 1;
            }
            finally
            {
                ( _fileLogger as IDisposable )?.Dispose();
            }
        }

        // ==== LOGGER ====
        private void InitFileLogger()
        {
            // Logların bazası HƏMİŞƏ ScriptsFolder + Logging.Folder
            var scriptsRoot = ToAbsoluteScriptsFolder(_cfg.ScriptsFolder ?? ".\\sql");
            var logsRoot = _cfg.Logging?.Folder ?? "logs";
            var logsDir = Path.IsPathRooted(logsRoot) ? logsRoot : Path.Combine(scriptsRoot, logsRoot);

            Directory.CreateDirectory(logsDir);

            var fileName = (_cfg.Logging?.RollingByDate ?? true)
                ? $"app-{DateTime.Now:yyyyMMdd}.log"
                : "app.log";

            var path = Path.Combine(logsDir, fileName);

            var level = (_cfg.Logging?.MinimumLevel ?? "Information").Trim().ToLowerInvariant();
            var le = level switch
            {
                "debug" => Serilog.Events.LogEventLevel.Debug,
                "warning" => Serilog.Events.LogEventLevel.Warning,
                "error" => Serilog.Events.LogEventLevel.Error,
                "fatal" => Serilog.Events.LogEventLevel.Fatal,
                _ => Serilog.Events.LogEventLevel.Information
            };

            _fileLogger = new LoggerConfiguration()
                .MinimumLevel.Is(le)
                .WriteTo.File(path, rollingInterval: RollingInterval.Infinite, shared: true)
                .CreateLogger();

            _info($"Log file: {path}");
        }

        // ==== HELPERS ====
        private static List<string> SplitSqlBatches(string raw)
        {
            // GO ayırıcısı (sətir başlanğıcı/sonu)
            var re = new Regex(@"^\s*GO\s*;$|^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var parts = re.Split(raw).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            return parts.Count == 0 ? new List<string> { raw } : parts;
        }

        private static string ToAbsoluteScriptsFolder(string value)
        {
            if (Path.IsPathRooted(value)) return value;
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, value));
        }

        // ==== JOURNAL ====
        private static Dictionary<string, JournalEntry> LoadJournal(string path)
        {
            try
            {
                if (!File.Exists(path)) return new();
                var json = File.ReadAllText(path, Encoding.UTF8);
                var map = JsonSerializer.Deserialize<Dictionary<string, JournalEntry>>(json) ?? new();
                return map;
            }
            catch { return new(); }
        }

        private static void SaveJournal(string path, Dictionary<string, JournalEntry> data)
        {
            var dir = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private sealed class JournalEntry
        {
            public string Key { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Sha256 { get; set; } = "";
            public DateTime ExecutedAtUtc { get; set; }
        }
    }
}
