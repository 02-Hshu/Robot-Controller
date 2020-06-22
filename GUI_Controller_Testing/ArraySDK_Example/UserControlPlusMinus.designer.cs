namespace SDK_Example
{
    partial class UserControlPlusMinus
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserControlPlusMinus));
            this.labelName = new System.Windows.Forms.Label();
            this.butPlus = new System.Windows.Forms.PictureBox();
            this.butMinus = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.butPlus)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.butMinus)).BeginInit();
            this.SuspendLayout();
            // 
            // labelName
            // 
            this.labelName.AutoEllipsis = true;
            this.labelName.Font = new System.Drawing.Font("Lato", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelName.ForeColor = System.Drawing.Color.White;
            this.labelName.Location = new System.Drawing.Point(7, 52);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(60, 13);
            this.labelName.TabIndex = 2;
            this.labelName.Text = "Nameabcd";
            this.labelName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // butPlus
            // 
            this.butPlus.Image = ((System.Drawing.Image)(resources.GetObject("butPlus.Image")));
            this.butPlus.Location = new System.Drawing.Point(14, 8);
            this.butPlus.Name = "butPlus";
            this.butPlus.Size = new System.Drawing.Size(43, 43);
            this.butPlus.TabIndex = 3;
            this.butPlus.TabStop = false;
            this.butPlus.Click += new System.EventHandler(this.ProtoButPlus_Click);
            this.butPlus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ProtoButPlus_MouseDown);
            this.butPlus.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ProtoButPlus_MouseUp);
            // 
            // butMinus
            // 
            this.butMinus.Image = ((System.Drawing.Image)(resources.GetObject("butMinus.Image")));
            this.butMinus.Location = new System.Drawing.Point(14, 72);
            this.butMinus.Name = "butMinus";
            this.butMinus.Size = new System.Drawing.Size(43, 43);
            this.butMinus.TabIndex = 4;
            this.butMinus.TabStop = false;
            this.butMinus.Click += new System.EventHandler(this.ProtoButMinus_Click);
            this.butMinus.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ProtoButMinus_MouseDown);
            this.butMinus.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ProtoButMinus_MouseUp);
            // 
            // UserControlPlusMinus
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.Transparent;
            this.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("$this.BackgroundImage")));
            this.Controls.Add(this.butMinus);
            this.Controls.Add(this.butPlus);
            this.Controls.Add(this.labelName);
            this.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ForeColor = System.Drawing.Color.Black;
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "UserControlPlusMinus";
            this.Size = new System.Drawing.Size(75, 128);
            ((System.ComponentModel.ISupportInitialize)(this.butPlus)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.butMinus)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label labelName;
        private System.Windows.Forms.PictureBox butPlus;
        private System.Windows.Forms.PictureBox butMinus;
    }
}
