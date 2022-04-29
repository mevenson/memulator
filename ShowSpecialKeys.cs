﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Memulator
{
    public partial class ShowSpecialKeys : Form
    {
        public ShowSpecialKeys()
        {
            InitializeComponent();
        }

        private void ShowSpecialKeys_Load(object sender, EventArgs e)
        {
            textBoxSpecialKeys.ReadOnly = true;
            textBoxSpecialKeys.BorderStyle = BorderStyle.None;
            textBoxSpecialKeys.Text =
@"     F1     Show Configuration Editor
CTRL-F1     Kill emulation
CTRL-F2     Reset emulation
     F2     Show Special Keys Menu
CTRL-F3     Start save output to Console Dump File
CTRL-F4     Stop  save output to Console Dump File
CTRL-F5     Flush save output to Console Dump File
CTRL-F6     Reload drive maps from configuration file (Windows)
     F6     Reload drive maps from configuration file (Linux)
CTRL-F7     Start Trace
     F8     Stuff OS9 format date time string into keyboard buffer
CTRL-F9     Toggle DMAF-3 access logging
     F11    Stuff FLEX date time format into keyboard buffer
CTRL-F11    Stuff UniFLEX date time format into keyboard buffer
     F12    Toggle Debug output
";
            textBoxSpecialKeys.Select(0, 0);
        }

        // this is for the form key down
        private void ShowSpecialKeys_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        // this is for the text box key down
        private void textBoxSpecialKeys_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
    }
}
