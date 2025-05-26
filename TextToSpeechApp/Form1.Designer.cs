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
            SuspendLayout();
            // 
            // btnSelectFile
            // 
            btnSelectFile.Location = new Point(12, 12);
            btnSelectFile.Name = "btnSelectFile";
            btnSelectFile.Size = new Size(120, 23);
            btnSelectFile.TabIndex = 0;
            btnSelectFile.Text = "Select Text File";
            btnSelectFile.UseVisualStyleBackColor = true;
            btnSelectFile.Click += btnSelectFile_Click;
            // 
            // txtEditor
            // 
            txtEditor.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtEditor.Location = new Point(12, 54);
            txtEditor.Multiline = true;
            txtEditor.Name = "txtEditor";
            txtEditor.ScrollBars = ScrollBars.Both;
            txtEditor.Size = new Size(748, 306);
            txtEditor.TabIndex = 1;
            // 
            // btnSelectFolder
            // 
            btnSelectFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSelectFolder.Location = new Point(12, 397);
            btnSelectFolder.Name = "btnSelectFolder";
            btnSelectFolder.Size = new Size(120, 23);
            btnSelectFolder.TabIndex = 2;
            btnSelectFolder.Text = "Select Output Folder";
            btnSelectFolder.UseVisualStyleBackColor = true;
            btnSelectFolder.Click += btnSelectFolder_Click;
            // 
            // lblSelectedFolder
            // 
            lblSelectedFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblSelectedFolder.AutoEllipsis = true;
            lblSelectedFolder.Location = new Point(138, 397);
            lblSelectedFolder.Name = "lblSelectedFolder";
            lblSelectedFolder.Size = new Size(634, 23);
            lblSelectedFolder.TabIndex = 3;
            lblSelectedFolder.Text = "Output Folder: (None selected)";
            lblSelectedFolder.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbVoiceSelection
            // 
            cmbVoiceSelection.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            cmbVoiceSelection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbVoiceSelection.FormattingEnabled = true;
            cmbVoiceSelection.Location = new Point(96, 513);
            cmbVoiceSelection.Name = "cmbVoiceSelection";
            cmbVoiceSelection.Size = new Size(250, 23);
            cmbVoiceSelection.TabIndex = 4;
            // 
            // btnStartConversion
            // 
            btnStartConversion.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnStartConversion.Font = new Font("Segoe UI", 12F);
            btnStartConversion.Location = new Point(606, 513);
            btnStartConversion.Name = "btnStartConversion";
            btnStartConversion.Size = new Size(154, 42);
            btnStartConversion.TabIndex = 5;
            btnStartConversion.Text = "Convert to Speech";
            btnStartConversion.UseVisualStyleBackColor = true;
            btnStartConversion.Click += btnStartConversion_Click;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.Location = new Point(12, 581);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(760, 23);
            lblStatus.TabIndex = 6;
            lblStatus.Text = "Status: Ready";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblSpeakerSelection
            // 
            lblSpeakerSelection.AutoSize = true;
            lblSpeakerSelection.Font = new Font("Segoe UI", 12F);
            lblSpeakerSelection.Location = new Point(12, 515);
            lblSpeakerSelection.Name = "lblSpeakerSelection";
            lblSpeakerSelection.Size = new Size(66, 21);
            lblSpeakerSelection.TabIndex = 7;
            lblSpeakerSelection.Text = "Speaker";
            // 
            // cmbSpeakerSelection
            // 
            cmbSpeakerSelection.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSpeakerSelection.FormattingEnabled = true;
            cmbSpeakerSelection.Location = new Point(377, 513);
            cmbSpeakerSelection.Name = "cmbSpeakerSelection";
            cmbSpeakerSelection.Size = new Size(121, 23);
            cmbSpeakerSelection.TabIndex = 8;
            cmbSpeakerSelection.Visible = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 613);
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
    }
}
