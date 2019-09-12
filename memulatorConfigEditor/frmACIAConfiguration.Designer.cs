namespace ConfigEditor
{
    partial class FrmAciaConfiguration
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
            this.textBoxBaseAddress = new System.Windows.Forms.TextBox();
            this.labelBaseAddress = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.labelNumberOfSerialPorts = new System.Windows.Forms.Label();
            this.tabControlPorts = new System.Windows.Forms.TabControl();
            this.comboBoxNumberOfPorts = new System.Windows.Forms.ComboBox();

            // 
            // tabControlPorts
            // 
            this.tabControlPorts.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControlPorts.Location = new System.Drawing.Point(22, 95);
            this.tabControlPorts.Name = "tabControlPorts";
            this.tabControlPorts.SelectedIndex = 0;
            this.tabControlPorts.Size = new System.Drawing.Size(496, 171);
            this.tabControlPorts.TabIndex = 4;

            this.tabControlPorts.SuspendLayout();

            // 
            // textBoxBaseAddress
            // 
            this.textBoxBaseAddress.Location = new System.Drawing.Point(137, 50);
            this.textBoxBaseAddress.Name = "textBoxBaseAddress";
            this.textBoxBaseAddress.Size = new System.Drawing.Size(100, 20);
            this.textBoxBaseAddress.TabIndex = 3;
            // 
            // labelBaseAddress
            // 
            this.labelBaseAddress.AutoSize = true;
            this.labelBaseAddress.Location = new System.Drawing.Point(23, 54);
            this.labelBaseAddress.Name = "labelBaseAddress";
            this.labelBaseAddress.Size = new System.Drawing.Size(100, 13);
            this.labelBaseAddress.TabIndex = 2;
            this.labelBaseAddress.Text = "Base Address (Hex)";
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(443, 44);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonOK
            // 
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(443, 14);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 5;
            this.buttonOK.Text = "&OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // labelNumberOfSerialPorts
            // 
            this.labelNumberOfSerialPorts.AutoSize = true;
            this.labelNumberOfSerialPorts.Location = new System.Drawing.Point(22, 23);
            this.labelNumberOfSerialPorts.Name = "labelNumberOfSerialPorts";
            this.labelNumberOfSerialPorts.Size = new System.Drawing.Size(109, 13);
            this.labelNumberOfSerialPorts.TabIndex = 0;
            this.labelNumberOfSerialPorts.Text = "Number of serial ports";
            // 
            // frmACIAConfiguration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(537, 278);
            this.Controls.Add(this.comboBoxNumberOfPorts);
            this.Controls.Add(this.tabControlPorts);
            this.Controls.Add(this.labelNumberOfSerialPorts);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.labelBaseAddress);
            this.Controls.Add(this.textBoxBaseAddress);
            this.Name = "frmACIAConfiguration";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "frmACIAConfiguration";
            this.Load += new System.EventHandler(this.frmACIAConfiguration_Load);
            this.tabControlPorts.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

            // 
            // comboBoxNumberOfPorts
            // 
            this.comboBoxNumberOfPorts.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxNumberOfPorts.FormattingEnabled = true;
            this.comboBoxNumberOfPorts.Items.AddRange(new object[] {
            "1",
            "2",
            "4"});
            this.comboBoxNumberOfPorts.Location = new System.Drawing.Point(138, 19);
            this.comboBoxNumberOfPorts.Name = "comboBoxNumberOfPorts";
            this.comboBoxNumberOfPorts.Size = new System.Drawing.Size(42, 21);
            this.comboBoxNumberOfPorts.TabIndex = 1;
            this.comboBoxNumberOfPorts.SelectionChangeCommitted += new System.EventHandler(this.comboBoxNumberOfPorts_SelectionChangeCommitted);
            this.comboBoxNumberOfPorts.SelectedValueChanged += new System.EventHandler(this.comboBoxNumberOfPorts_SelectedValueChanged);
        }

        #endregion

        private System.Windows.Forms.TabControl tabControlPorts;

        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Label labelBaseAddress;
        private System.Windows.Forms.TextBox textBoxBaseAddress;
        private System.Windows.Forms.Label labelNumberOfSerialPorts;
        private System.Windows.Forms.ComboBox comboBoxNumberOfPorts;
    }
}