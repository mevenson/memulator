using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;


namespace ConfigEditor
{
    public partial class frmDiskAddEdit : Form
    {
        public string imagePath = "";
        public string imageFormat = "";

        public frmDiskAddEdit()
        {
            InitializeComponent();
            buttonOK.Enabled = false;
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            string path = "";

            if (textBoxImagePath.Text.Length > 0)
                path = Path.GetDirectoryName(textBoxImagePath.Text);

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = memulatorConfigEditor.dataDir + path;
            ofd.FileName = Path.GetFileName(textBoxImagePath.Text);

            DialogResult dr = ofd.ShowDialog();
            if (dr == DialogResult.OK)
            {
                textBoxImagePath.Text = ofd.FileName.Replace(memulatorConfigEditor.dataDir, "");
            }

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonOK.Enabled = true;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            imagePath = textBoxImagePath.Text;
            imageFormat = (string)comboBoxImageFormat.SelectedItem;
        }

        private void frmDiskAddEdit_Load(object sender, EventArgs e)
        {
            if (imageFormat.Length > 0)
                buttonOK.Enabled = true;

            textBoxImagePath.Text = imagePath;
            comboBoxImageFormat.SelectedItem = imageFormat;
        }
    }
}
