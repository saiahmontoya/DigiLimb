namespace DigiLimbv1
{
    partial class UserSettings
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
            btnSave = new System.Windows.Forms.Button();
            txtDPI = new System.Windows.Forms.TextBox();
            txtName = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // btnSave
            // 
            btnSave.Location = new System.Drawing.Point(338, 270);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(125, 63);
            btnSave.TabIndex = 0;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // txtDPI
            // 
            txtDPI.Location = new System.Drawing.Point(469, 179);
            txtDPI.Name = "txtDPI";
            txtDPI.Size = new System.Drawing.Size(137, 27);
            txtDPI.TabIndex = 1;
            // 
            // txtName
            // 
            txtName.Location = new System.Drawing.Point(469, 74);
            txtName.Name = "txtName";
            txtName.Size = new System.Drawing.Size(137, 27);
            txtName.TabIndex = 2;
            // 
            // label1
            // 
            label1.Location = new System.Drawing.Point(186, 74);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(167, 27);
            label1.TabIndex = 3;
            label1.Text = "DigiLimb PC Name";
            label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.Location = new System.Drawing.Point(186, 179);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(167, 27);
            label2.TabIndex = 4;
            label2.Text = "Mouse Emulator DPI";
            label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // UserSettings
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 450);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtName);
            Controls.Add(txtDPI);
            Controls.Add(btnSave);
            Name = "UserSettings";
            Text = "UserSettings";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.TextBox txtDPI;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}