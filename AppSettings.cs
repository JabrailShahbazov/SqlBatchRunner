// SqlBatchRunner.Win/AppSettings.cs
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlBatchRunner.Win
{
    public sealed class AppSettings
    {
        // UI / Locale
        public string? Language { get; set; } = "az";

        // Source
        public string SourceMode { get; set; } = "Folder"; // Folder | Archive
        public string? ArchivePath { get; set; } = "";
        public string ScriptsFolder { get; set; } = "./sql";
        public string FilePattern { get; set; } = "*.sql";

        // Execution
        public string ConnectionString { get; set; } = "";
        public string OrderBy { get; set; } = "LastWriteTimeThenFileNameDate";
        public bool UseCreationTime { get; set; } = false;
        public bool StopOnError { get; set; } = false;
        public bool DryRun { get; set; } = false;

        // Journal & logging
        public string JournalFile { get; set; } = "executed.json";
        public LoggingConfig Logging { get; set; } = new();

        // Extra
        public bool RerunIfChanged { get; set; } = true;
        public string WorkingRoot { get; set; } = ""; // boşdursa default Paths.WorkRoot istifadə olunur

        // NEW: Arxiv iş qovluqlarından neçə dənə saxlanılsın (1 = yalnız sonuncu)
        public int WorkKeepCount { get; set; } = 1;

        // ==== Save/Load ====
        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

        public static AppSettings Load(string path)
        {
            var text = File.ReadAllText(path);
            var cfg = JsonSerializer.Deserialize<AppSettings>(text, _jsonOpts)
                      ?? new AppSettings();
            return cfg;
        }

        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(this, _jsonOpts);
            File.WriteAllText(path, json);
        }
    }

    public sealed class LoggingConfig
    {
        public string Folder { get; set; } = "logs";
        public bool RollingByDate { get; set; } = true;
        public string MinimumLevel { get; set; } = "Information"; // Debug/Information/Warning/Error/Fatal
    }
}
