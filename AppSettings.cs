using System.Text.Json;

namespace SqlBatchRunner.Win
{
    public sealed class AppSettings
    {
        // MƏNBƏ SEÇİMİ
        // Folder | Archive
        public string SourceMode { get; set; } = "Folder";
        public string? ArchivePath { get; set; } = "";

        // Mövcud sahələr
        public string ConnectionString { get; set; } = "";
        public string ScriptsFolder { get; set; } = ".\\sql";
        public string FilePattern { get; set; } = "*.sql";
        public string OrderBy { get; set; } = "LastWriteTimeThenFileNameDate";
        public bool UseCreationTime { get; set; } = false;
        public bool StopOnError { get; set; } = false;
        public string JournalFile { get; set; } = "executed.json";
        public bool DryRun { get; set; } = false;

        public LoggingSettings? Logging { get; set; } = new();

        // Dəyişmiş faylı yenidən işə sal
        public bool RerunIfChanged { get; set; } = true;

        // Arxiv çıxarışı üçün iş qovluğu kökü (default: C:\ProgramData\ScriptPilot\work)
        public string? WorkingRoot { get; set; } = null;

        public static AppSettings Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException($"Config not found: {Path.GetFullPath(path)}");
            var json = File.ReadAllText(path);
            var cfg = JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return cfg ?? throw new InvalidOperationException("Config deserialize failed.");
        }

        public void Save(string path)
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

    public sealed class LoggingSettings
    {
        public string Folder { get; set; } = "logs";
        public bool RollingByDate { get; set; } = true;
        public string MinimumLevel { get; set; } = "Information";
    }
}
