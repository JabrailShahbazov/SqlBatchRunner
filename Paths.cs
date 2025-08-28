// SqlBatchRunner.Win/Paths.cs
using System;
using System.IO;

namespace SqlBatchRunner.Win
{
    public static class Paths
    {
        // C:\ProgramData\ScriptPilot
        public static string ProgramDataRoot =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "ScriptPilot");

        public static string ConfigPath       => Path.Combine(ProgramDataRoot, "appsettings.json");
        public static string LegacyConfigPath => Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        public static string DefaultConfigPath=> Path.Combine(AppContext.BaseDirectory, "appsettings.default.json");

        // NEW: iş qovluqları üçün baza
        public static string WorkRoot         => Path.Combine(ProgramDataRoot, "work");

        public static void EnsureProgramData()
        {
            Directory.CreateDirectory(ProgramDataRoot);
            Directory.CreateDirectory(WorkRoot);
        }
    }
}