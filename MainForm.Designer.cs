namespace SqlBatchRunner.Win
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Root
        private TableLayoutPanel tlpRoot;

        // Source
        private GroupBox grpSource;
        private TableLayoutPanel tlpSource;
        private FlowLayoutPanel flpSourceMode;
        private RadioButton rbSourceFolder;
        private RadioButton rbSourceArchive;
        private Label lblArchive;
        private TextBox txtArchivePath;
        private Button btnBrowseArchive;
        private Label lblFolder;
        private TextBox txtScriptsFolder;
        private Button btnBrowse;

        // Connection
        private GroupBox grpConnection;
        private TextBox txtConnection;

        // Options
        private GroupBox grpOptions;
        private TableLayoutPanel tlpOptions;
        private Label lblOrder;
        private ComboBox cmbOrderBy;
        private CheckBox chkUseCreation;
        private CheckBox chkStopOnError;
        private CheckBox chkDryRun;

        // Actions
        private FlowLayoutPanel flpActions;
        private Button btnSave;
        private Button btnRun;
        private Button btnOpenAppsettings;
        private Button btnOpenLogs;
        private Button btnOpenWorkDir;

        // Logs
        private GroupBox grpLog;
        private RichTextBox rtbLog;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ==== FORM ====
            this.Text = "ScriptPilot — SQL Runner";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 700);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.BackColor = Color.FromArgb(248, 249, 251);

            // ==== ROOT ====
            tlpRoot = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(16)
            };
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Source
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Connection
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Options
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // Actions
            tlpRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Log

            // ==== SOURCE ====
            grpSource = new GroupBox
            {
                Text = "Source",
                Dock = DockStyle.Top,
                Padding = new Padding(12),
                Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point)
            };

            tlpSource = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3
            };
            tlpSource.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tlpSource.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tlpSource.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tlpSource.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // mode
            tlpSource.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // archive
            tlpSource.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // folder

            // Mode row
            flpSourceMode = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            rbSourceFolder  = new RadioButton { Text = "Folder",  AutoSize = true, Checked = true, Margin = new Padding(0, 2, 16, 0) };
            rbSourceArchive = new RadioButton { Text = "Archive", AutoSize = true, Margin = new Padding(0, 2, 0, 0) };
            flpSourceMode.Controls.Add(rbSourceFolder);
            flpSourceMode.Controls.Add(rbSourceArchive);

            var lblSource = new Label { Text = "Mode:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 8, 6) };
            tlpSource.Controls.Add(lblSource, 0, 0);
            tlpSource.Controls.Add(flpSourceMode, 1, 0);
            tlpSource.SetColumnSpan(flpSourceMode, 2);

            // Archive row (həmişə əlavə edirik; görünməni kod idarə edəcək)
            lblArchive       = new Label  { Text = "Archive file:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 8, 6) };
            txtArchivePath   = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 0, 8, 6) };
            btnBrowseArchive = new Button { Text = "Browse…", Anchor = AnchorStyles.Right, Width = 100, Margin = new Padding(0, 0, 0, 6) };
            tlpSource.Controls.Add(lblArchive,       0, 1);
            tlpSource.Controls.Add(txtArchivePath,   1, 1);
            tlpSource.Controls.Add(btnBrowseArchive, 2, 1);

            // Folder row (həmişə əlavə edirik; görünməni kod idarə edəcək)
            lblFolder       = new Label  { Text = "Scripts folder:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 8, 0) };
            txtScriptsFolder= new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 0, 8, 0) };
            btnBrowse       = new Button { Text = "Browse…", Anchor = AnchorStyles.Right, Width = 100, Margin = new Padding(0) };
            tlpSource.Controls.Add(lblFolder,        0, 2);
            tlpSource.Controls.Add(txtScriptsFolder, 1, 2);
            tlpSource.Controls.Add(btnBrowse,        2, 2);

            grpSource.Controls.Add(tlpSource);

            // ==== CONNECTION ====
            grpConnection = new GroupBox
                            {
                                Text = "Connection",
                                Dock = DockStyle.Top,
                                Padding = new Padding(12),
                                Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point)
                            };
            txtConnection = new TextBox
                            {
                                Dock = DockStyle.Fill,
                                Margin = new Padding(2),
                                BorderStyle = BorderStyle.FixedSingle,
                                Multiline = true,                 // textarea
                                ScrollBars = ScrollBars.Vertical, // vertikal scroll
                                WordWrap = true,
                                MinimumSize = new Size(0, 70)     // hündürlüyü böyüdür
                            };
            grpConnection.Controls.Add(txtConnection);


            // ==== OPTIONS ====
            grpOptions = new GroupBox
            {
                Text = "Options",
                Dock = DockStyle.Top,
                Padding = new Padding(12),
                Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point)
            };
            tlpOptions = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1
            };
            tlpOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            tlpOptions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tlpOptions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpOptions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpOptions.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            lblOrder = new Label { Text = "Order by:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 8, 0) };
            cmbOrderBy = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(0, 0, 12, 0)
            };
            chkUseCreation = new CheckBox { Text = "Use Creation Time", AutoSize = true, Anchor = AnchorStyles.Left };
            chkStopOnError = new CheckBox { Text = "Stop On Error", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(16, 0, 0, 0) };
            chkDryRun      = new CheckBox { Text = "Dry Run", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(16, 0, 0, 0) };

            tlpOptions.Controls.Add(lblOrder, 0, 0);
            tlpOptions.Controls.Add(cmbOrderBy, 1, 0);
            tlpOptions.Controls.Add(chkUseCreation, 2, 0);
            tlpOptions.Controls.Add(chkStopOnError, 3, 0);
            tlpOptions.Controls.Add(chkDryRun, 4, 0);

            grpOptions.Controls.Add(tlpOptions);

            // ==== ACTIONS ====
            flpActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 8, 0, 0)
            };
            btnSave            = new Button { Text = "Save", Width = 110, Height = 34, Margin = new Padding(0, 0, 8, 0) };
            btnRun             = new Button { Text = "Run ▶", Width = 130, Height = 34, Margin = new Padding(0, 0, 12, 0) };
            btnOpenAppsettings = new Button { Text = "Open appsettings.json", AutoSize = true, Height = 34, Margin = new Padding(0, 0, 8, 0) };
            btnOpenLogs        = new Button { Text = "Open Logs", AutoSize = true, Height = 34, Margin = new Padding(0, 0, 8, 0) };
            btnOpenWorkDir     = new Button { Text = "Open Work Dir", AutoSize = true, Height = 34, Margin = new Padding(0) };
            flpActions.Controls.AddRange(new Control[] { btnSave, btnRun, btnOpenAppsettings, btnOpenLogs, btnOpenWorkDir });
            this.AcceptButton = btnRun;

            // ==== LOG ====
            grpLog = new GroupBox
            {
                Text = "Logs",
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point)
            };
            rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point)
            };
            grpLog.Controls.Add(rtbLog);

            // ==== ADD ====
            tlpRoot.Controls.Add(grpSource,    0, 0);
            tlpRoot.Controls.Add(grpConnection,0, 1);
            tlpRoot.Controls.Add(grpOptions,   0, 2);
            tlpRoot.Controls.Add(flpActions,   0, 3);
            tlpRoot.Controls.Add(grpLog,       0, 4);
            this.Controls.Add(tlpRoot);

            // Order combobox default (ilk açılışda boş qalmasın deyə)
            if (cmbOrderBy.Items.Count == 0)
            {
                cmbOrderBy.Items.AddRange(new object[]
                {
                    "LastWriteTime",
                    "FileNameDate",
                    "LastWriteTimeThenFileNameDate",
                    "FileNameDateThenLastWriteTime"
                });
                cmbOrderBy.SelectedItem = "LastWriteTimeThenFileNameDate";
            }
        }
    }
}
