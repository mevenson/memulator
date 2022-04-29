
namespace Memulator
{
    partial class ShowSpecialKeys
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
            this.textBoxSpecialKeys = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBoxSpecialKeys
            // 
            this.textBoxSpecialKeys.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSpecialKeys.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxSpecialKeys.Location = new System.Drawing.Point(32, 26);
            this.textBoxSpecialKeys.Multiline = true;
            this.textBoxSpecialKeys.Name = "textBoxSpecialKeys";
            this.textBoxSpecialKeys.Size = new System.Drawing.Size(405, 214);
            this.textBoxSpecialKeys.TabIndex = 0;
            this.textBoxSpecialKeys.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBoxSpecialKeys_KeyDown);
            // 
            // ShowSpecialKeys
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(474, 270);
            this.Controls.Add(this.textBoxSpecialKeys);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ShowSpecialKeys";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Special Keys";
            this.Load += new System.EventHandler(this.ShowSpecialKeys_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ShowSpecialKeys_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxSpecialKeys;
    }
}