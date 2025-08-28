using System.Diagnostics;
using System.Windows.Forms;
using SqlBatchRunner.Win;

namespace WindowsFormsSqlBatchRunner
{
    public partial class MainForm : Form
    {
        // ProgramData-da saxlanılan config
        private readonly string _configPath = Paths.ConfigPath;

        private AppSettings? _cfg;
        private CancellationTokenSource? _cts;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            try
            {
                Paths.EnsureProgramData();

                // İlk dəfə: ProgramData-da appsettings.json yoxdursa, mənbədən kopyala
                if (!File.Exists(_configPath))
                {
                    string? src = null;
                    if (File.Exists(Paths.LegacyConfigPath)) src = Paths.LegacyConfigPath;
                    else if (File.Exists(Paths.DefaultConfigPath)) src = Paths.DefaultConfigPath;

                    if (src != null)
                        File.Copy(src, _configPath, overwrite: false);
                    else
                        File.WriteAllText(_configPath,
@"{
  ""ConnectionString"": """",
  ""ScriptsFolder"": ""./sql"",
  ""FilePattern"": ""*.sql"",
  ""OrderBy"": ""LastWriteTimeThenFileNameDate"",
  ""UseCreationTime"": false,
  ""StopOnError"": false,
  ""JournalFile"": ""executed.json"",
  ""DryRun"": false,
  ""Logging"": { ""Folder"": ""logs"", ""RollingByDate"": true, ""MinimumLevel"": ""Information"" }
}");
                }

                _cfg = AppSettings.Load(_configPath);
                BindToForm(_cfg);

                // Form ikonu: EXE-dən götür
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

                LogInfo($"Config: {_configPath}");

                // Köhnə səhv yerdəki configi .bak et (qarışmasın)
                try
                {
                    if (File.Exists(Paths.LegacyConfigPath))
                        File.Move(Paths.LegacyConfigPath, Paths.LegacyConfigPath + ".bak", true);
                }
                catch { /* ignore */ }
            }
            catch (Exception ex)
            {
                LogError("Config yüklənmə xətası: " + ex.Message);
            }
        }

        private void BindToForm(AppSettings cfg)
        {
            txtConnection.Text = cfg.ConnectionString;
            txtScriptsFolder.Text = ToAbsoluteScriptsFolder(cfg.ScriptsFolder);
            cmbOrderBy.SelectedItem = cfg.OrderBy;
            if (cmbOrderBy.SelectedIndex < 0) cmbOrderBy.SelectedItem = "LastWriteTimeThenFileNameDate";
            chkUseCreation.Checked = cfg.UseCreationTime;
            chkStopOnError.Checked = cfg.StopOnError;
            chkDryRun.Checked = cfg.DryRun;
        }

        private void BindFromForm(AppSettings cfg)
        {
            cfg.ConnectionString = txtConnection.Text.Trim();
            cfg.ScriptsFolder = FromAbsoluteScriptsFolder(txtScriptsFolder.Text.Trim());
            cfg.OrderBy = cmbOrderBy.SelectedItem?.ToString() ?? "LastWriteTimeThenFileNameDate";
            cfg.UseCreationTime = chkUseCreation.Checked;
            cfg.StopOnError = chkStopOnError.Checked;
            cfg.DryRun = chkDryRun.Checked;
        }

        private string ToAbsoluteScriptsFolder(string value)
        {
            if (Path.IsPathRooted(value)) return value;
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, value));
        }
        private string FromAbsoluteScriptsFolder(string absolute)
        {
            var baseDir = AppContext.BaseDirectory.TrimEnd('\\');
            var full = Path.GetFullPath(absolute);
            if (full.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            {
                var rel = "." + full.Substring(baseDir.Length).Replace('/', '\\');
                return rel;
            }
            return absolute;
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            dlg.SelectedPath = Directory.Exists(txtScriptsFolder.Text) ? txtScriptsFolder.Text : AppContext.BaseDirectory;
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtScriptsFolder.Text = dlg.SelectedPath;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (_cfg == null) _cfg = new AppSettings();
                BindFromForm(_cfg);
                _cfg.Save(_configPath);   // hər zaman ProgramData
                LogInfo("Config saxlanıldı.");
            }
            catch (Exception ex)
            {
                LogError("Config yazıla bilmədi: " + ex.Message);
            }
        }

        private async void btnRun_Click(object sender, EventArgs e)
        {
            if (_cts != null) { _cts.Cancel(); return; }

            try
            {
                btnRun.Enabled = false;
                btnSave.Enabled = false;

                if (_cfg == null) _cfg = new AppSettings();
                BindFromForm(_cfg);
                _cfg.Save(_configPath);

                EnsureFolderExists(_cfg.ScriptsFolder);
                EnsureFolderExists(Path.Combine(_cfg.ScriptsFolder, _cfg.Logging?.Folder ?? "logs"));

                _cts = new CancellationTokenSource();

                var svc = new RunnerService(_cfg, LogInfo, LogError);
                LogInfo("===== RUN START =====");
                var code = await svc.RunAsync(_cts.Token);
                LogInfo($"===== RUN END ===== ExitCode={code}");
            }
            catch (Exception ex)
            {
                LogError("Run xətası: " + ex.Message);
            }
            finally
            {
                _cts = null;
                btnRun.Enabled = true;
                btnSave.Enabled = true;
            }
        }

        private void EnsureFolderExists(string path)
        {
            var p = Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
            if (!Directory.Exists(p)) Directory.CreateDirectory(p);
        }

        private void btnOpenAppsettings_Click(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    _cfg ??= new AppSettings();
                    _cfg.Save(_configPath);
                }
                Process.Start(new ProcessStartInfo("notepad.exe", $"\"{_configPath}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogError("appsettings açıla bilmədi: " + ex.Message);
            }
        }

        private void btnOpenLogs_Click(object sender, EventArgs e)
        {
            try
            {
                var logsRoot = _cfg?.Logging?.Folder ?? "logs";
                var logsPath = Path.IsPathRooted(logsRoot) ? logsRoot : Path.Combine(ToAbsoluteScriptsFolder(_cfg?.ScriptsFolder ?? ".\\sql"), logsRoot);
                Directory.CreateDirectory(logsPath);
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{logsPath}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogError("Logs açıla bilmədi: " + ex.Message);
            }
        }

        // --- UI log helpers ---
        private void LogInfo(string msg) => AppendLog(msg, false);
        private void LogError(string msg) => AppendLog(msg, true);

        private void AppendLog(string msg, bool isError)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action<string, bool>(AppendLog), msg, isError);
                return;
            }
            var prefix = isError ? "[ERR ] " : "[INFO] ";
            rtbLog.SelectionColor = isError ? Color.Firebrick : Color.DarkGreen;
            rtbLog.AppendText($"{DateTime.Now:HH:mm:ss} {prefix}{msg}{Environment.NewLine}");
            rtbLog.SelectionColor = Color.Black;
            rtbLog.ScrollToCaret();
        }
    }
}
