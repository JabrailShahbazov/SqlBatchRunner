using System;
using System.IO;
using System.Windows.Forms;
using SqlBatchRunner.Win;

namespace WindowsFormsSqlBatchRunner
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            try { Paths.EnsureProgramData(); } catch { /* ignore */ }

            // Default AZ
            var language = "az";
            var cfgPath = Paths.ConfigPath;

            try
            {
                if (File.Exists(cfgPath))
                {
                    var cfg = AppSettings.Load(cfgPath);
                    if (!string.IsNullOrWhiteSpace(cfg.Language))
                        language = cfg.Language!;
                }
            }
            catch { /* ignore */ }

            I18n.ApplyCulture(language);

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}