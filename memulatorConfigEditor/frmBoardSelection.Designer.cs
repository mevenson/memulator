namespace ConfigEditor
{
    partial class frmBoardSelection
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
            this.comboBoxBoard = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxBaseAddress = new System.Windows.Forms.TextBox();
            this.labelNumberOfBytes = new System.Windows.Forms.Label();
            this.textBoxNumberOfBytes = new System.Windows.Forms.TextBox();
            this.checkBoxInterruptEnabled = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // comboBoxBoard
            // 
            this.comboBoxBoard.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxBoard.FormattingEnabled = true;
            this.comboBoxBoard.Location = new System.Drawing.Point(88, 24);
            this.comboBoxBoard.Name = "comboBoxBoard";
            this.comboBoxBoard.Size = new System.Drawing.Size(142, 21);
            this.comboBoxBoard.Sorted = true;
            this.comboBoxBoard.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 27);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Board";
            // 
            // buttonOK
            // 
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.Location = new System.Drawing.Point(292, 27);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = "&OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(292, 57);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 98);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Base Address (Hex)";
            // 
            // textBoxBaseAddress
            // 
            this.textBoxBaseAddress.Location = new System.Drawing.Point(172, 96);
            this.textBoxBaseAddress.Name = "textBoxBaseAddress";
            this.textBoxBaseAddress.Size = new System.Drawing.Size(100, 20);
            this.textBoxBaseAddress.TabIndex = 5;
            // 
            // labelNumberOfBytes
            // 
            this.labelNumberOfBytes.AutoSize = true;
            this.labelNumberOfBytes.Location = new System.Drawing.Point(13, 131);
            this.labelNumberOfBytes.Name = "labelNumberOfBytes";
            this.labelNumberOfBytes.Size = new System.Drawing.Size(113, 13);
            this.labelNumberOfBytes.TabIndex = 6;
            this.labelNumberOfBytes.Text = "Number of Bytes (Hex)";
            // 
            // textBoxNumberOfBytes
            // 
            this.textBoxNumberOfBytes.Location = new System.Drawing.Point(172, 128);
            this.textBoxNumberOfBytes.Name = "textBoxNumberOfBytes";
            this.textBoxNumberOfBytes.Size = new System.Drawing.Size(58, 20);
            this.textBoxNumberOfBytes.TabIndex = 7;
            // 
            // checkBoxInterruptEnabled
            // 
            this.checkBoxInterruptEnabled.AutoSize = true;
            this.checkBoxInterruptEnabled.Location = new System.Drawing.Point(13, 164);
            this.checkBoxInterruptEnabled.Name = "checkBoxInterruptEnabled";
            this.checkBoxInterruptEnabled.Size = new System.Drawing.Size(107, 17);
            this.checkBoxInterruptEnabled.TabIndex = 8;
            this.checkBoxInterruptEnabled.Text = "Interrupt Enabled";
            this.checkBoxInterruptEnabled.UseVisualStyleBackColor = true;
            // 
            // BoardSelection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(405, 240);
            this.Controls.Add(this.checkBoxInterruptEnabled);
            this.Controls.Add(this.textBoxNumberOfBytes);
            this.Controls.Add(this.labelNumberOfBytes);
            this.Controls.Add(this.textBoxBaseAddress);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBoxBoard);
            this.Name = "BoardSelection";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "BoardSelection";
            this.Load += new System.EventHandler(this.BoardSelection_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxBoard;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxBaseAddress;
        private System.Windows.Forms.Label labelNumberOfBytes;
        private System.Windows.Forms.TextBox textBoxNumberOfBytes;
        private System.Windows.Forms.CheckBox checkBoxInterruptEnabled;
    }
}