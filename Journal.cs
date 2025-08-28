using System.Text.Json;

namespace SqlBatchRunner.Win
{
    public sealed class JournalEntry
    {
        public string Hash { get; set; } = "";
        public DateTime LastExecutedUtc { get; set; } = DateTime.UtcNow;
    }

    public sealed class Journal
    {
        // key = JournalKey (folder: relative path; archive: <archiveName>::<entryPath>)
        public Dictionary<string, JournalEntry> Executed { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);

        public static Journal Load(string path)
        {
            try
            {
                if (!File.Exists(path)) return new Journal();

                var json = File.ReadAllText(path);

                // Backward compat: köhnə format list<string> ola bilər
                if (json.TrimStart().StartsWith("["))
                {
                    var arr = JsonSerializer.Deserialize<List<string>>(json) ?? new();
                    var j = new Journal();
                    foreach (var k in arr)
                        j.Executed[k] = new JournalEntry { Hash = "", LastExecutedUtc = DateTime.UtcNow };
                    return j;
                }

                var j2 = JsonSerializer.Deserialize<Journal>(json);
                return j2 ?? new Journal();
            }
            catch
            {
                return new Journal();
            }
        }

        public void Save(string path)
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public bool IsExecuted(string key, string hash, bool rerunIfChanged)
        {
            if (!Executed.TryGetValue(key, out var e)) return false;
            if (!rerunIfChanged) return true;           // dəyişsə də skip
            if (string.IsNullOrEmpty(e.Hash)) return false; // köhnə jurnal → yenidən işlət
            return string.Equals(e.Hash, hash, StringComparison.OrdinalIgnoreCase);
        }

        public void MarkExecuted(string key, string hash)
        {
            Executed[key] = new JournalEntry { Hash = hash, LastExecutedUtc = DateTime.UtcNow };
        }
    }
}
