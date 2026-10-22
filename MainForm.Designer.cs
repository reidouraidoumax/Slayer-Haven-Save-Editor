namespace ToukidenSaveEditor
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _backdrop?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        ///  Note: bounds below are the 100% / 1280x900 layout; MainForm.LayoutShell()
        ///  recomputes every region at runtime from the design's responsive rules.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            topBar = new ToukidenSaveEditor.UI.TopBar();
            sidebar = new ToukidenSaveEditor.UI.Sidebar();
            workbench = new ToukidenSaveEditor.UI.Workbench();
            actionBar = new ToukidenSaveEditor.UI.ActionBar();
            btnImport = new ToukidenSaveEditor.UI.InkButton();
            btnExport = new ToukidenSaveEditor.UI.InkButton();
            actionBar.SuspendLayout();
            SuspendLayout();
            //
            // topBar
            //
            topBar.Location = new System.Drawing.Point(0, 0);
            topBar.Name = "topBar";
            topBar.Size = new System.Drawing.Size(1280, 90);
            topBar.TabIndex = 0;
            topBar.TabStop = false;
            //
            // sidebar
            //
            sidebar.Location = new System.Drawing.Point(0, 92);
            sidebar.Name = "sidebar";
            sidebar.Size = new System.Drawing.Size(391, 730);
            sidebar.TabIndex = 1;
            sidebar.TabStop = false;
            //
            // workbench
            //
            workbench.Location = new System.Drawing.Point(442, 128);
            workbench.Name = "workbench";
            workbench.Size = new System.Drawing.Size(787, 658);
            workbench.TabIndex = 2;
            workbench.TabStop = false;
            workbench.ApplyRequested += workbench_ApplyRequested;
            //
            // actionBar
            //
            actionBar.Controls.Add(btnImport);
            actionBar.Controls.Add(btnExport);
            actionBar.Location = new System.Drawing.Point(0, 822);
            actionBar.Name = "actionBar";
            actionBar.Size = new System.Drawing.Size(1280, 78);
            actionBar.TabIndex = 3;
            //
            // btnImport
            //
            btnImport.Icon = ToukidenSaveEditor.UI.InkButtonIcon.Upload;
            btnImport.Location = new System.Drawing.Point(51, 18);
            btnImport.Name = "btnImport";
            btnImport.Size = new System.Drawing.Size(141, 41);
            btnImport.TabIndex = 0;
            btnImport.Text = "Import save";
            btnImport.Variant = ToukidenSaveEditor.UI.InkButtonVariant.Vermilion;
            btnImport.Click += btnImport_Click;
            //
            // btnExport
            //
            btnExport.Enabled = false;
            btnExport.Icon = ToukidenSaveEditor.UI.InkButtonIcon.Download;
            btnExport.Location = new System.Drawing.Point(202, 18);
            btnExport.Name = "btnExport";
            btnExport.Size = new System.Drawing.Size(141, 41);
            btnExport.TabIndex = 1;
            btnExport.Text = "Export  save";
            btnExport.Variant = ToukidenSaveEditor.UI.InkButtonVariant.Teal;
            btnExport.Click += btnExport_Click;
            //
            // MainForm
            //
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            BackColor = System.Drawing.Color.FromArgb(25, 27, 24);
            ClientSize = new System.Drawing.Size(1280, 900);
            Controls.Add(topBar);
            Controls.Add(sidebar);
            Controls.Add(workbench);
            Controls.Add(actionBar);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Toukiden 2 Save Editor";
            actionBar.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private ToukidenSaveEditor.UI.TopBar topBar;
        private ToukidenSaveEditor.UI.Sidebar sidebar;
        private ToukidenSaveEditor.UI.Workbench workbench;
        private ToukidenSaveEditor.UI.ActionBar actionBar;
        private ToukidenSaveEditor.UI.InkButton btnImport;
        private ToukidenSaveEditor.UI.InkButton btnExport;
    }
}
