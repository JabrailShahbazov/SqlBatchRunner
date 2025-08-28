using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SqlBatchRunner.Win;
using R = global::SqlBatchRunner.Win.Properties.Resources;

namespace SqlBatchRunner.Win
{
    public partial class MainForm : Form
    {
        // ProgramData-da saxlanılan konfiq yolu
        private readonly string _configPath = Paths.ConfigPath;

        private AppSettings? _cfg;
        private CancellationTokenSource? _cts;
        private string? _lastWorkDir;

        public MainForm()
        {
            InitializeComponent();

            // Connection textbox-u "textarea" kimi
            try
            {
                txtConnection.Multiline = true;
                txtConnection.ScrollBars = ScrollBars.Vertical;
                txtConnection.MinimumSize = new Size(0, 70);
                txtConnection.Height = Math.Max(txtConnection.Height, 70);
            }
            catch { /* ignore */ }

            // Event wiring
            this.Load += MainForm_Load;

            rbSourceFolder.CheckedChanged  += (_, __) => UpdateSourceUi();
            rbSourceArchive.CheckedChanged += (_, __) => UpdateSourceUi();

            btnBrowse.Click          += btnBrowse_Click;
            btnBrowseArchive.Click   += btnBrowseArchive_Click;
            btnOpenWorkDir.Click     += btnOpenWorkDir_Click;
            btnOpenAppsettings.Click += btnOpenAppsettings_Click;
            btnOpenLogs.Click        += btnOpenLogs_Click;
            btnSave.Click            += btnSave_Click;
            btnRun.Click             += btnRun_Click;

            // İlk UI vəziyyəti
            UpdateSourceUi();
        }

        // =================== LOAD ===================
        private void MainForm_Load(object? sender, EventArgs e)
        {
            try
            {
                Paths.EnsureProgramData();

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
  ""Language"": ""az"",
  ""SourceMode"": ""Folder"",
  ""ArchivePath"": """",
  ""ConnectionString"": """",
  ""ScriptsFolder"": ""./sql"",
  ""FilePattern"": ""*.sql"",
  ""OrderBy"": ""LastWriteTimeThenFileNameDate"",
  ""UseCreationTime"": false,
  ""StopOnError"": false,
  ""JournalFile"": ""executed.json"",
  ""DryRun"": false,
  ""Logging"": { ""Folder"": ""logs"", ""RollingByDate"": true, ""MinimumLevel"": ""Information"" },
  ""RerunIfChanged"": true,
  ""WorkingRoot"": """"
}");
                }

                _cfg = AppSettings.Load(_configPath);
                BindToForm(_cfg);

                try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { /* ignore */ }

                LogInfo(string.Format(R.Msg_ConfigPath, _configPath));

                // Köhnə kök qovluqda qalan config varsa .bak et
                try
                {
                    if (File.Exists(Paths.LegacyConfigPath))
                        File.Move(Paths.LegacyConfigPath, Paths.LegacyConfigPath + ".bak", true);
                }
                catch { /* ignore */ }
            }
            catch (Exception ex)
            {
                LogError(R.Err_ConfigLoad + ex.Message);
            }
        }

        // =================== BINDING ===================
        private void BindToForm(AppSettings cfg)
        {
            txtConnection.Text    = cfg.ConnectionString ?? "";
            txtScriptsFolder.Text = ToAbsoluteScriptsFolder(cfg.ScriptsFolder ?? "./sql");

            // --- OrderBy combo (lokallaşdırılmış görünüş + sabit dəyərlər) ---
            {
                cmbOrderBy.DataSource = null;
                cmbOrderBy.Items.Clear();
                cmbOrderBy.DisplayMember = "Key";
                cmbOrderBy.ValueMember   = "Value";

                var items = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, string>>
                {
                    new(R.Sort_LastWriteTime, "LastWriteTime"),
                    new(R.Sort_FileNameDate, "FileNameDate"),
                    new(R.Sort_LastWriteTimeThenFileNameDate, "LastWriteTimeThenFileNameDate"),
                    new(R.Sort_FileNameDateThenLastWriteTime, "FileNameDateThenLastWriteTime"),
                };

                cmbOrderBy.DataSource = items;

                var current = (cfg.OrderBy ?? "LastWriteTimeThenFileNameDate").Trim();
                cmbOrderBy.SelectedValue = current;
                if (cmbOrderBy.SelectedIndex < 0)
                    cmbOrderBy.SelectedValue = "LastWriteTimeThenFileNameDate";
            }

            chkUseCreation.Checked = cfg.UseCreationTime;
            chkStopOnError.Checked = cfg.StopOnError;
            chkDryRun.Checked      = cfg.DryRun;

            // Source mode
            bool isArchive = string.Equals(cfg.SourceMode, "Archive", StringComparison.OrdinalIgnoreCase);
            rbSourceArchive.Checked = isArchive;
            rbSourceFolder.Checked  = !isArchive;
            txtArchivePath.Text     = cfg.ArchivePath ?? "";

            UpdateSourceUi();
            ApplyStrings();
        }

        private void BindFromForm(AppSettings cfg)
        {
            cfg.ConnectionString = (txtConnection.Text ?? "").Trim();
            cfg.ScriptsFolder    = FromAbsoluteScriptsFolder((txtScriptsFolder.Text ?? "").Trim());
            cfg.OrderBy          = (cmbOrderBy.SelectedValue?.ToString() ?? "LastWriteTimeThenFileNameDate").Trim();
            cfg.UseCreationTime  = chkUseCreation.Checked;
            cfg.StopOnError      = chkStopOnError.Checked;
            cfg.DryRun           = chkDryRun.Checked;

            cfg.SourceMode  = rbSourceArchive.Checked ? "Archive" : "Folder";
            cfg.ArchivePath = (txtArchivePath.Text ?? "").Trim();
        }

        private void ApplyStrings()
        {
            // Başlıq
            this.Text = R.AppTitle;

            // Source
            grpSource.Text = R.Source;
            // "Mode:" labelını tapıb tərcümə et (layout-a görə 0,0-da yerləşdirilmiş label)
            foreach (Control c in grpSource.Controls)
            {
                if (c is TableLayoutPanel tlp)
                    if (tlp.GetControlFromPosition(0, 0) is Label lblMode) lblMode.Text = R.Mode;
            }
            rbSourceFolder.Text  = R.Folder;
            rbSourceArchive.Text = R.Archive;
            lblArchive.Text      = R.ArchiveFile;
            lblFolder.Text       = R.ScriptsFolder;

            // Connection
            grpConnection.Text   = R.Connection;

            // Options
            grpOptions.Text      = R.Options;
            lblOrder.Text        = R.OrderBy;
            chkUseCreation.Text  = R.UseCreationTime;
            chkStopOnError.Text  = R.StopOnError;
            chkDryRun.Text       = R.DryRun;

            // Actions
            btnSave.Text            = R.Save;
            btnRun.Text             = R.Run;
            btnOpenAppsettings.Text = R.OpenAppsettings;
            btnOpenLogs.Text        = R.OpenLogs;
            btnOpenWorkDir.Text     = R.OpenWorkDir;

            // Logs
            grpLog.Text = R.Logs;
        }

        // =================== SOURCE UI STATE ===================
        private void UpdateSourceUi()
        {
            bool isArchive = rbSourceArchive.Checked;

            lblArchive.Visible        = txtArchivePath.Visible   = btnBrowseArchive.Visible = isArchive;
            lblFolder.Visible         = txtScriptsFolder.Visible = btnBrowse.Visible        = !isArchive;

            txtArchivePath.ReadOnly   = !isArchive;
            btnBrowseArchive.Enabled  =  isArchive;

            txtScriptsFolder.ReadOnly =  isArchive;
            btnBrowse.Enabled         = !isArchive;
        }

        // =================== BROWSE HANDLERS ===================
        private void btnBrowse_Click(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            dlg.SelectedPath = Directory.Exists(txtScriptsFolder.Text) ? txtScriptsFolder.Text : AppContext.BaseDirectory;
            if (dlg.ShowDialog(this) == DialogResult.OK)
                txtScriptsFolder.Text = dlg.SelectedPath;
        }

        private void btnBrowseArchive_Click(object? sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Archives|*.zip;*.rar;*.7z;*.tar;*.gz|All files|*.*",
                Title  = "Select archive"
            };
            if (ofd.ShowDialog(this) == DialogResult.OK)
                txtArchivePath.Text = ofd.FileName;
        }

        private void btnOpenWorkDir_Click(object? sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_lastWorkDir) || !Directory.Exists(_lastWorkDir))
                {
                    LogError(R.Err_WorkDirNotFound);
                    return;
                }
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_lastWorkDir}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogError(R.Err_WorkDirOpen + ex.Message);
            }
        }

        // =================== APPSETTINGS & LOGS ===================
        private void btnOpenAppsettings_Click(object? sender, EventArgs e)
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

        private void btnOpenLogs_Click(object? sender, EventArgs e)
        {
            try
            {
                var logsRoot = _cfg?.Logging?.Folder ?? "logs";

                // Logların bazası HƏMİŞƏ ScriptsFolder-dır (Archive mode olsa da)
                var baseDir = ToAbsoluteScriptsFolder(_cfg?.ScriptsFolder ?? ".\\sql");

                var logsPath = Path.IsPathRooted(logsRoot) ? logsRoot : Path.Combine(baseDir, logsRoot);
                Directory.CreateDirectory(logsPath);

                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{logsPath}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogError(R.Err_LogsOpen + ex.Message);
            }
        }

        // =================== SAVE & RUN ===================
        private void btnSave_Click(object? sender, EventArgs e)
        {
            try
            {
                _cfg ??= new AppSettings();
                BindFromForm(_cfg);
                _cfg.Save(_configPath);
                LogInfo(R.Msg_ConfigSaved);
            }
            catch (Exception ex)
            {
                LogError(R.Err_ConfigWrite + ex.Message);
            }
        }

        private async void btnRun_Click(object? sender, EventArgs e)
        {
            if (_cts != null) { _cts.Cancel(); return; }

            try
            {
                btnRun.Enabled = false;
                btnSave.Enabled = false;

                _cfg ??= new AppSettings();
                BindFromForm(_cfg);
                _cfg.Save(_configPath);

                if (!rbSourceArchive.Checked) // Folder mode
                {
                    EnsureFolderExists(_cfg.ScriptsFolder);
                    EnsureFolderExists(Path.Combine(_cfg.ScriptsFolder, _cfg.Logging?.Folder ?? "logs"));
                }

                _cts = new CancellationTokenSource();

                var svc = new RunnerService(_cfg, LogInfo, LogError);
                LogInfo(string.Format(R.Msg_RunStart, _cfg.SourceMode));
                var code = await svc.RunAsync(_cts.Token);
                _lastWorkDir = svc.LastWorkingDir;
                LogInfo(string.Format(R.Msg_RunEnd, code));
            }
            catch (Exception ex)
            {
                LogError(R.Err_Run + ex.Message);
            }
            finally
            {
                _cts = null;
                btnRun.Enabled = true;
                btnSave.Enabled = true;
            }
        }

        // =================== HELPERS ===================
        private void EnsureFolderExists(string path)
        {
            var p = Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
            if (!Directory.Exists(p)) Directory.CreateDirectory(p);
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
                return "." + full.Substring(baseDir.Length).Replace('/', '\\');
            return absolute;
        }

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
