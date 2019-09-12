using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ConfigEditor
{
    public partial class frmBoardSelection : Form
    {
        public string BoardName = "";
        public string GUID = "";
        public string BaseAddress = "0000";
        public string NumberOfBytes = "0";
        public bool InterruptEnabled = false;

        public List<string> BoardsInstalled = new List<string>();

        public bool EditMode = false;

        public frmBoardSelection()
        {
            InitializeComponent();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            BoardName = (string)comboBoxBoard.SelectedItem;
            BaseAddress = textBoxBaseAddress.Text;
            NumberOfBytes = textBoxNumberOfBytes.Text;
            InterruptEnabled = checkBoxInterruptEnabled.Checked;
        }

        private void BoardSelection_Load(object sender, EventArgs e)
        {
            if (EditMode)
            {
                comboBoxBoard.Items.Add(BoardName);
                comboBoxBoard.Enabled = false;
                comboBoxBoard.SelectedItem = BoardName;
                textBoxBaseAddress.Text = BaseAddress;
                textBoxNumberOfBytes.Text = NumberOfBytes;
                checkBoxInterruptEnabled.Checked = InterruptEnabled;
            }
            else
            {
                foreach (string boardName in memulatorConfigEditor._deviceNames)
                {
                    if (boardName != "ROM" && boardName != "RAM" && boardName != "8274" && boardName != "DAT")
                    {
                        int MPScount = 0;
                        foreach (string board in BoardsInstalled)
                        {
                            if (board == "MPS")
                                MPScount++;
                        }

                        if (!BoardsInstalled.Contains(boardName) || (boardName == "MPS" && MPScount < 4))
                            comboBoxBoard.Items.Add(boardName);
                    }
                }
            }
        }
    }
}
