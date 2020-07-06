namespace ArraySDK_Example
{
    partial class FormCamera
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelCameraDevices = new System.Windows.Forms.Label();
            this.cmbCameraDevices = new System.Windows.Forms.ComboBox();
            this.labelResolutions = new System.Windows.Forms.Label();
            this.cmbCameraResolutions = new System.Windows.Forms.ComboBox();
            this.picCamera = new System.Windows.Forms.PictureBox();
            this.btnSave = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picCamera)).BeginInit();
            this.SuspendLayout();
            // 
            // labelCameraDevices
            // 
            this.labelCameraDevices.AutoSize = true;
            this.labelCameraDevices.Location = new System.Drawing.Point(12, 27);
            this.labelCameraDevices.Name = "labelCameraDevices";
            this.labelCameraDevices.Size = new System.Drawing.Size(91, 13);
            this.labelCameraDevices.TabIndex = 0;
            this.labelCameraDevices.Text = "Camera Devices: ";
            // 
            // cmbCameraDevices
            // 
            this.cmbCameraDevices.FormattingEnabled = true;
            this.cmbCameraDevices.Location = new System.Drawing.Point(124, 24);
            this.cmbCameraDevices.Name = "cmbCameraDevices";
            this.cmbCameraDevices.Size = new System.Drawing.Size(217, 21);
            this.cmbCameraDevices.TabIndex = 1;
            this.cmbCameraDevices.SelectedIndexChanged += new System.EventHandler(this.CmbCameraDevices_SelectedIndexChanged);
            // 
            // labelResolutions
            // 
            this.labelResolutions.AutoSize = true;
            this.labelResolutions.Location = new System.Drawing.Point(12, 64);
            this.labelResolutions.Name = "labelResolutions";
            this.labelResolutions.Size = new System.Drawing.Size(102, 13);
            this.labelResolutions.TabIndex = 2;
            this.labelResolutions.Text = "Camera Resolution: ";
            // 
            // cmbCameraResolutions
            // 
            this.cmbCameraResolutions.FormattingEnabled = true;
            this.cmbCameraResolutions.Location = new System.Drawing.Point(124, 61);
            this.cmbCameraResolutions.Name = "cmbCameraResolutions";
            this.cmbCameraResolutions.Size = new System.Drawing.Size(217, 21);
            this.cmbCameraResolutions.TabIndex = 3;
            this.cmbCameraResolutions.SelectedIndexChanged += new System.EventHandler(this.CmbCameraResolutions_SelectedIndexChanged);
            // 
            // picCamera
            // 
            this.picCamera.Location = new System.Drawing.Point(15, 103);
            this.picCamera.Name = "picCamera";
            this.picCamera.Size = new System.Drawing.Size(326, 294);
            this.picCamera.TabIndex = 4;
            this.picCamera.TabStop = false;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(138, 415);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 5;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);
            // 
            // FormCamera
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(353, 450);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.picCamera);
            this.Controls.Add(this.cmbCameraResolutions);
            this.Controls.Add(this.labelResolutions);
            this.Controls.Add(this.cmbCameraDevices);
            this.Controls.Add(this.labelCameraDevices);
            this.Name = "FormCamera";
            this.Text = "FormCamera";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormCamera_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.picCamera)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelCameraDevices;
        private System.Windows.Forms.ComboBox cmbCameraDevices;
        private System.Windows.Forms.Label labelResolutions;
        private System.Windows.Forms.ComboBox cmbCameraResolutions;
        private System.Windows.Forms.PictureBox picCamera;
        private System.Windows.Forms.Button btnSave;
    }
}