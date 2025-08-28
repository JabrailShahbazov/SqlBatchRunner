namespace WindowsFormsSqlBatchRunner
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtConnection = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtScriptsFolder = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.cmbOrderBy = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.chkUseCreation = new System.Windows.Forms.CheckBox();
            this.chkStopOnError = new System.Windows.Forms.CheckBox();
            this.chkDryRun = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnOpenAppsettings = new System.Windows.Forms.Button();
            this.btnOpenLogs = new System.Windows.Forms.Button();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // txtConnection
            // 
            this.txtConnection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtConnection.Location = new System.Drawing.Point(15, 32);
            this.txtConnection.Multiline = true;
            this.txtConnection.Name = "txtConnection";
            this.txtConnection.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtConnection.Size = new System.Drawing.Size(774, 54);
            this.txtConnection.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(277, 20);
            this.label1.Text = "Connection String";
            // 
            // txtScriptsFolder
            // 
            this.txtScriptsFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtScriptsFolder.Location = new System.Drawing.Point(15, 115);
            this.txtScriptsFolder.Name = "txtScriptsFolder";
            this.txtScriptsFolder.Size = new System.Drawing.Size(692, 23);
            this.txtScriptsFolder.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(12, 92);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(277, 20);
            this.label2.Text = "Scripts Folder";
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(713, 114);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(76, 25);
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // cmbOrderBy
            // 
            this.cmbOrderBy.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbOrderBy.FormattingEnabled = true;
            this.cmbOrderBy.Items.AddRange(new object[] {
            "LastWriteTime",
            "FileNameDate",
            "LastWriteTimeThenFileNameDate",
            "FileNameDateThenLastWriteTime"});
            this.cmbOrderBy.Location = new System.Drawing.Point(15, 164);
            this.cmbOrderBy.Name = "cmbOrderBy";
            this.cmbOrderBy.Size = new System.Drawing.Size(281, 23);
            this.cmbOrderBy.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(12, 141);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(277, 20);
            this.label3.Text = "Order By";
            // 
            // chkUseCreation
            // 
            this.chkUseCreation.AutoSize = true;
            this.chkUseCreation.Location = new System.Drawing.Point(313, 166);
            this.chkUseCreation.Name = "chkUseCreation";
            this.chkUseCreation.Size = new System.Drawing.Size(181, 19);
            this.chkUseCreation.Text = "Use Creation Time (Date created)";
            this.chkUseCreation.UseVisualStyleBackColor = true;
            // 
            // chkStopOnError
            // 
            this.chkStopOnError.AutoSize = true;
            this.chkStopOnError.Location = new System.Drawing.Point(15, 196);
            this.chkStopOnError.Name = "chkStopOnError";
            this.chkStopOnError.Size = new System.Drawing.Size(98, 19);
            this.chkStopOnError.Text = "Stop On Error";
            this.chkStopOnError.UseVisualStyleBackColor = true;
            // 
            // chkDryRun
            // 
            this.chkDryRun.AutoSize = true;
            this.chkDryRun.Location = new System.Drawing.Point(130, 196);
            this.chkDryRun.Name = "chkDryRun";
            this.chkDryRun.Size = new System.Drawing.Size(67, 19);
            this.chkDryRun.Text = "Dry Run";
            this.chkDryRun.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(15, 225);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(104, 28);
            this.btnSave.Text = "Save Settings";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnRun
            // 
            this.btnRun.Location = new System.Drawing.Point(130, 225);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(104, 28);
            this.btnRun.Text = "Run ▶";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // btnOpenAppsettings
            // 
            this.btnOpenAppsettings.Location = new System.Drawing.Point(245, 225);
            this.btnOpenAppsettings.Name = "btnOpenAppsettings";
            this.btnOpenAppsettings.Size = new System.Drawing.Size(129, 28);
            this.btnOpenAppsettings.Text = "Open appsettings.json";
            this.btnOpenAppsettings.UseVisualStyleBackColor = true;
            this.btnOpenAppsettings.Click += new System.EventHandler(this.btnOpenAppsettings_Click);
            // 
            // btnOpenLogs
            // 
            this.btnOpenLogs.Location = new System.Drawing.Point(384, 225);
            this.btnOpenLogs.Name = "btnOpenLogs";
            this.btnOpenLogs.Size = new System.Drawing.Size(104, 28);
            this.btnOpenLogs.Text = "Open Logs";
            this.btnOpenLogs.UseVisualStyleBackColor = true;
            this.btnOpenLogs.Click += new System.EventHandler(this.btnOpenLogs_Click);
            // 
            // rtbLog
            // 
            this.rtbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbLog.Location = new System.Drawing.Point(15, 268);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(774, 260);
            this.rtbLog.TabIndex = 99;
            this.rtbLog.Text = "";

            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(804, 540);
            this.Controls.Add(this.rtbLog);
            this.Controls.Add(this.btnOpenLogs);
            this.Controls.Add(this.btnOpenAppsettings);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.chkDryRun);
            this.Controls.Add(this.chkStopOnError);
            this.Controls.Add(this.chkUseCreation);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmbOrderBy);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtScriptsFolder);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtConnection);
            this.MinimumSize = new System.Drawing.Size(820, 580);
            this.Name = "MainForm";
            this.Text = "SqlBatchRunner (Windows)";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private TextBox txtConnection;
        private Label label1;
        private TextBox txtScriptsFolder;
        private Label label2;
        private Button btnBrowse;
        private ComboBox cmbOrderBy;
        private Label label3;
        private CheckBox chkUseCreation;
        private CheckBox chkStopOnError;
        private CheckBox chkDryRun;
        private Button btnSave;
        private Button btnRun;
        private Button btnOpenAppsettings;
        private Button btnOpenLogs;
        private RichTextBox rtbLog;
    }
}
