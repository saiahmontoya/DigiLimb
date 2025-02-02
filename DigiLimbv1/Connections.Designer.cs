namespace DigiLimbv1
{
    partial class Connections
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
            lstDevices = new System.Windows.Forms.ListBox();
            txtLogs = new System.Windows.Forms.TextBox();
            btnStartScan = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // lstDevices
            // 
            lstDevices.FormattingEnabled = true;
            lstDevices.HorizontalScrollbar = true;
            lstDevices.Location = new System.Drawing.Point(40, 86);
            lstDevices.Name = "lstDevices";
            lstDevices.Size = new System.Drawing.Size(329, 144);
            lstDevices.TabIndex = 0;
            // 
            // txtLogs
            // 
            txtLogs.Location = new System.Drawing.Point(420, 86);
            txtLogs.Multiline = true;
            txtLogs.Name = "txtLogs";
            txtLogs.Size = new System.Drawing.Size(329, 159);
            txtLogs.TabIndex = 1;
            // 
            // btnStartScan
            // 
            btnStartScan.Location = new System.Drawing.Point(317, 302);
            btnStartScan.Name = "btnStartScan";
            btnStartScan.Size = new System.Drawing.Size(166, 57);
            btnStartScan.TabIndex = 2;
            btnStartScan.Text = "Scan for Devices";
            btnStartScan.UseVisualStyleBackColor = true;
            btnStartScan.Click += btnStartScan_Click_1;
            // 
            // Connections
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.SystemColors.AppWorkspace;
            ClientSize = new System.Drawing.Size(800, 450);
            Controls.Add(btnStartScan);
            Controls.Add(txtLogs);
            Controls.Add(lstDevices);
            Name = "Connections";
            Text = "Form2";
            Load += Connections_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ListBox lstDevices;
        private System.Windows.Forms.TextBox txtLogs;
        private System.Windows.Forms.Button btnStartScan;
    }
}