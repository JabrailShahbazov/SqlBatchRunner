using System;

namespace SqlBatchRunner.Win
{
    internal static class Paths
    {
        public static string Company => "ScriptPilot";

        // Quraşdırma qovluğu (read-only ola bilər)
        public static string AppDir => AppContext.BaseDirectory;

        // Machine-wide yazıla bilən yer: C:\ProgramData\ScriptPilot
        public static string ProgramDataDir =>
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Company);

        // Config yolları
        public static string ConfigPath => System.IO.Path.Combine(ProgramDataDir, "appsettings.json");
        public static string LegacyConfigPath => System.IO.Path.Combine(AppDir, "appsettings.json");            // köhnə, səhv yer
        public static string DefaultConfigPath => System.IO.Path.Combine(AppDir, "appsettings.default.json");   // şablon

        public static void EnsureProgramData()
        {
            if (!System.IO.Directory.Exists(ProgramDataDir))
                System.IO.Directory.CreateDirectory(ProgramDataDir);
        }
    }
}