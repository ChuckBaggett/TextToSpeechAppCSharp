namespace TextToSpeechApp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnSelectFile;
        private System.Windows.Forms.TextBox txtEditor;
        private System.Windows.Forms.Button btnSelectFolder;
        private System.Windows.Forms.Label lblSelectedFolder;
        private System.Windows.Forms.ComboBox cmbVoiceSelection;
        private System.Windows.Forms.Button btnStartConversion;
        private System.Windows.Forms.Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            btnSelectFile = new Button();
            txtEditor = new TextBox();
            btnSelectFolder = new Button();
            lblSelectedFolder = new Label();
            cmbVoiceSelection = new ComboBox();
            btnStartConversion = new Button();
            lblStatus = new Label();
            lblSpeakerSelection = new Label();
            cmbSpeakerSelection = new ComboBox();
            lblLanguageSelection = new Label();
            cmbLanguageSelection = new ComboBox();
            SuspendLayout();
            // 
            // btnSelectFile
            // 
            this.btnSelectFile.Location = new System.Drawing.Point(12, 12);
            this.btnSelectFile.Name = "btnSelectFile";
            this.btnSelectFile.Size = new System.Drawing.Size(120, 23);
            this.btnSelectFile.TabIndex = 0;
            this.btnSelectFile.Text = "Select Text File";
            this.btnSelectFile.UseVisualStyleBackColor = true;
            this.btnSelectFile.Click += new System.EventHandler(this.btnSelectFile_Click);
            // 
            // txtEditor
            // 
            this.txtEditor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtEditor.Location = new System.Drawing.Point(12, 41);
            this.txtEditor.Multiline = true;
            this.txtEditor.Name = "txtEditor";
            this.txtEditor.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtEditor.Size = new System.Drawing.Size(760, 350);
            this.txtEditor.TabIndex = 1;
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSelectFolder.AutoSize = true;
            btnSelectFolder.Location = new Point(12, 395);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(125, 25);
            btnSelectFolder.TabIndex = 2;
            btnSelectFolder.Text = "Select Output Folder";
            btnSelectFolder.UseVisualStyleBackColor = true;
            btnSelectFolder.Click += btnSelectFolder_Click;
            // 
            // lblSelectedFolder
            // 
            lblSelectedFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblSelectedFolder.AutoEllipsis = true;
            lblSelectedFolder.Location = new Point(143, 397);
            lblSelectedFolder.Name = "lblSelectedFolder";
            lblSelectedFolder.Size = new Size(629, 23);
            lblSelectedFolder.TabIndex = 3;
            lblSelectedFolder.Text = "Output Folder: (None selected)";
            lblSelectedFolder.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbVoiceSelection
            // 
            this.cmbVoiceSelection.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cmbVoiceSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbVoiceSelection.FormattingEnabled = true;
            this.cmbVoiceSelection.Location = new System.Drawing.Point(12, 426);
            this.cmbVoiceSelection.Name = "cmbVoiceSelection";
            this.cmbVoiceSelection.Size = new System.Drawing.Size(250, 23);
            this.cmbVoiceSelection.TabIndex = 4;
            // 
            // btnStartConversion
            // 
            this.btnStartConversion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartConversion.Location = new System.Drawing.Point(652, 426);
            this.btnStartConversion.Name = "btnStartConversion";
            this.btnStartConversion.Size = new System.Drawing.Size(120, 23);
            this.btnStartConversion.TabIndex = 5;
            this.btnStartConversion.Text = "Convert to Speech";
            this.btnStartConversion.UseVisualStyleBackColor = true;
            this.btnStartConversion.Click += new System.EventHandler(this.btnStartConversion_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.Location = new System.Drawing.Point(12, 458);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(760, 23);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.Text = "Status: Ready";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLanguageSelection
            // 
            lblLanguageSelection.AutoSize = true;
            lblLanguageSelection.Location = new Point(357, 439);
            lblLanguageSelection.Name = "lblLanguageSelection";
            lblLanguageSelection.Size = new Size(59, 15);
            lblLanguageSelection.TabIndex = 9;
            lblLanguageSelection.Text = "Language";
            // 
            // cmbLanguageSelection
            // 
            cmbLanguageSelection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLanguageSelection.FormattingEnabled = true;
            cmbLanguageSelection.Location = new Point(422, 436);
            cmbLanguageSelection.Name = "cmbLanguageSelection";
            cmbLanguageSelection.Size = new Size(121, 23);
            cmbLanguageSelection.TabIndex = 10;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 613);
            Controls.Add(cmbLanguageSelection);
            Controls.Add(lblLanguageSelection);
            Controls.Add(cmbSpeakerSelection);
            Controls.Add(lblSpeakerSelection);
            Controls.Add(lblStatus);
            Controls.Add(btnStartConversion);
            Controls.Add(cmbVoiceSelection);
            Controls.Add(lblSelectedFolder);
            Controls.Add(btnSelectFolder);
            Controls.Add(txtEditor);
            Controls.Add(btnSelectFile);
            Name = "Form1";
            Text = "Text to Speech App";
            ResumeLayout(false);
            PerformLayout();
        }
        private Label label1;
        private ComboBox comboBox1;
        private Label lblSpeakerSelection;
        private ComboBox cmbSpeakerSelection;
        private Label lblLanguageSelection;
        private ComboBox cmbLanguageSelection;
    }
}
