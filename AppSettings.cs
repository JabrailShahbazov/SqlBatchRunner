using System.Text.Json;

namespace SqlBatchRunner.Win
{
    public sealed class AppSettings
    {
        public string ConnectionString { get; set; } = "";
        public string ScriptsFolder { get; set; } = "";
        public string FilePattern { get; set; } = "*.sql";
        // LastWriteTime | FileNameDate | LastWriteTimeThenFileNameDate | FileNameDateThenLastWriteTime
        public string OrderBy { get; set; } = "LastWriteTimeThenFileNameDate";
        public bool UseCreationTime { get; set; } = false;
        public bool StopOnError { get; set; } = false;
        public string JournalFile { get; set; } = "executed.json";
        public bool DryRun { get; set; } = false;

        public LoggingSettings? Logging { get; set; } = new();

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

    public sealed class Journal
    {
        public HashSet<string> ExecutedFiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public static Journal Load(string path)
        {
            if (!File.Exists(path)) return new Journal();
            var json = File.ReadAllText(path);
            return System.Text.Json.JsonSerializer.Deserialize<Journal>(json) ?? new Journal();
        }

        public void Save(string path)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
