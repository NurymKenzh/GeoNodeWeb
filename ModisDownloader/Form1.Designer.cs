
namespace ModisDownloader
{
    partial class FormMain
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
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageOptions = new System.Windows.Forms.TabPage();
            this.buttonStop = new System.Windows.Forms.Button();
            this.textBoxBoundingBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonStart = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxArchiveDirectory = new System.Windows.Forms.TextBox();
            this.tabPageLog = new System.Windows.Forms.TabPage();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.backgroundWorkerDownloader = new System.ComponentModel.BackgroundWorker();
            this.tabControl1.SuspendLayout();
            this.tabPageOptions.SuspendLayout();
            this.tabPageLog.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageOptions);
            this.tabControl1.Controls.Add(this.tabPageLog);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1143, 750);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPageOptions
            // 
            this.tabPageOptions.Controls.Add(this.buttonStop);
            this.tabPageOptions.Controls.Add(this.textBoxBoundingBox);
            this.tabPageOptions.Controls.Add(this.label2);
            this.tabPageOptions.Controls.Add(this.buttonStart);
            this.tabPageOptions.Controls.Add(this.label1);
            this.tabPageOptions.Controls.Add(this.textBoxArchiveDirectory);
            this.tabPageOptions.Location = new System.Drawing.Point(4, 34);
            this.tabPageOptions.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabPageOptions.Name = "tabPageOptions";
            this.tabPageOptions.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabPageOptions.Size = new System.Drawing.Size(1135, 712);
            this.tabPageOptions.TabIndex = 0;
            this.tabPageOptions.Text = "Options";
            this.tabPageOptions.UseVisualStyleBackColor = true;
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(149, 5);
            this.buttonStop.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(136, 38);
            this.buttonStop.TabIndex = 6;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // textBoxBoundingBox
            // 
            this.textBoxBoundingBox.Location = new System.Drawing.Point(157, 125);
            this.textBoxBoundingBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textBoxBoundingBox.Name = "textBoxBoundingBox";
            this.textBoxBoundingBox.Size = new System.Drawing.Size(475, 31);
            this.textBoxBoundingBox.TabIndex = 5;
            this.textBoxBoundingBox.Text = "50,40,80,50";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 125);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(128, 25);
            this.label2.TabIndex = 4;
            this.label2.Text = "Bounding box:";
            // 
            // buttonStart
            // 
            this.buttonStart.Location = new System.Drawing.Point(4, 5);
            this.buttonStart.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(136, 38);
            this.buttonStart.TabIndex = 0;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 55);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(149, 25);
            this.label1.TabIndex = 2;
            this.label1.Text = "Archive directory:";
            // 
            // textBoxArchiveDirectory
            // 
            this.textBoxArchiveDirectory.Location = new System.Drawing.Point(157, 60);
            this.textBoxArchiveDirectory.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textBoxArchiveDirectory.Name = "textBoxArchiveDirectory";
            this.textBoxArchiveDirectory.Size = new System.Drawing.Size(475, 31);
            this.textBoxArchiveDirectory.TabIndex = 3;
            this.textBoxArchiveDirectory.Text = "C:\\MODIS\\Archive";
            // 
            // tabPageLog
            // 
            this.tabPageLog.Controls.Add(this.textBoxLog);
            this.tabPageLog.Location = new System.Drawing.Point(4, 34);
            this.tabPageLog.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabPageLog.Name = "tabPageLog";
            this.tabPageLog.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.tabPageLog.Size = new System.Drawing.Size(1135, 712);
            this.tabPageLog.TabIndex = 1;
            this.tabPageLog.Text = "Log";
            this.tabPageLog.UseVisualStyleBackColor = true;
            // 
            // textBoxLog
            // 
            this.textBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxLog.Location = new System.Drawing.Point(4, 5);
            this.textBoxLog.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxLog.Size = new System.Drawing.Size(1127, 702);
            this.textBoxLog.TabIndex = 0;
            this.textBoxLog.WordWrap = false;
            // 
            // backgroundWorkerDownloader
            // 
            this.backgroundWorkerDownloader.WorkerSupportsCancellation = true;
            this.backgroundWorkerDownloader.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerDownloader_DoWork);
            this.backgroundWorkerDownloader.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerDownloader_RunWorkerCompleted);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1143, 750);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "FormMain";
            this.Text = "MODIS Downloader";
            this.tabControl1.ResumeLayout(false);
            this.tabPageOptions.ResumeLayout(false);
            this.tabPageOptions.PerformLayout();
            this.tabPageLog.ResumeLayout(false);
            this.tabPageLog.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageOptions;
        private System.Windows.Forms.TabPage tabPageLog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxArchiveDirectory;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.TextBox textBoxBoundingBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxLog;
        private System.Windows.Forms.Button buttonStop;
        private System.ComponentModel.BackgroundWorker backgroundWorkerDownloader;
    }
}

