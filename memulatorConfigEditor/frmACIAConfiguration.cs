using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ConfigEditor
{
    public partial class FrmAciaConfiguration : Form
    {
        public string BaseAddress = "0000";
        public string NumberOfBytes = "0";

        //public class ACIAConfig
        //{
        //    public int  _baudRate    = 9600;
        //    public int  _parity      = 0;
        //    public int  _stopBits    = 1;
        //    public int  _dataBits    = 8;
        //    public bool _interuptEnabled = false;
        //}

        public List<memulatorConfigEditor.ACIAConfig> aciaConfigs = new List<memulatorConfigEditor.ACIAConfig>();

        public class TabPage
        {
            public System.Windows.Forms.TabPage    _tabPageACIAPort          = new System.Windows.Forms.TabPage();
            public System.Windows.Forms.Label      _labelBaudRate            = new Label();
            public System.Windows.Forms.Label      _labelStopBits            = new Label();
            public System.Windows.Forms.Label      _labelParity              = new Label();
            public System.Windows.Forms.Label      _labelDataBits            = new Label();
            public System.Windows.Forms.ComboBox   _comboBoxParity           = new ComboBox();
            public System.Windows.Forms.TextBox    _textBoxBaudRate          = new TextBox();
            public System.Windows.Forms.ComboBox   _comboBoxDataBits         = new ComboBox();
            public System.Windows.Forms.ComboBox   _comboBoxStopBits         = new ComboBox();
            public System.Windows.Forms.CheckBox   _checkBoxInterruptEnabled = new CheckBox();

            public memulatorConfigEditor.ACIAConfig aciaConfig = new memulatorConfigEditor.ACIAConfig();

            public TabPage(int portNumber)
            {
                _tabPageACIAPort.Location = new System.Drawing.Point(4, 22);
                _tabPageACIAPort.Name = "tabPageACIAPort" + portNumber.ToString();
                _tabPageACIAPort.Padding = new System.Windows.Forms.Padding(3);
                _tabPageACIAPort.Size = new System.Drawing.Size(488, 145);
                _tabPageACIAPort.TabIndex = 0;
                _tabPageACIAPort.Text = "Port " + portNumber.ToString();
                _tabPageACIAPort.UseVisualStyleBackColor = true;

                #region BuildControls
                _labelBaudRate.AutoSize = true;
                _labelBaudRate.Location = new System.Drawing.Point(20, 19);
                _labelBaudRate.Name = "_labelBaudRate";
                _labelBaudRate.Size = new System.Drawing.Size(58, 13);
                _labelBaudRate.TabIndex = 16;
                _labelBaudRate.Text = "Baud Rate";

                // checkBoxInterruptEnabled
                // 
                _checkBoxInterruptEnabled.AutoSize = true;
                _checkBoxInterruptEnabled.Location = new System.Drawing.Point(355, 15);
                _checkBoxInterruptEnabled.Name = "checkBoxInterruptEnabled";
                _checkBoxInterruptEnabled.Size = new System.Drawing.Size(107, 17);
                _checkBoxInterruptEnabled.TabIndex = 15;
                _checkBoxInterruptEnabled.Text = "Interrupt Enabled";
                _checkBoxInterruptEnabled.UseVisualStyleBackColor = true;
                // 
                // labelParity
                // 
                _labelParity.AutoSize = true;
                _labelParity.Location = new System.Drawing.Point(20, 48);
                _labelParity.Name = "labelParity";
                _labelParity.Size = new System.Drawing.Size(33, 13);
                _labelParity.TabIndex = 17;
                _labelParity.Text = "Parity";
                // 
                // labelStopBits
                // 
                _labelStopBits.AutoSize = true;
                _labelStopBits.Location = new System.Drawing.Point(20, 77);
                _labelStopBits.Name = "labelStopBits";
                _labelStopBits.Size = new System.Drawing.Size(49, 13);
                _labelStopBits.TabIndex = 18;
                _labelStopBits.Text = "Stop Bits";
                // 
                // labelDataBits
                // 
                _labelDataBits.AutoSize = true;
                _labelDataBits.Location = new System.Drawing.Point(20, 106);
                _labelDataBits.Name = "labelDataBits";
                _labelDataBits.Size = new System.Drawing.Size(50, 13);
                _labelDataBits.TabIndex = 19;
                _labelDataBits.Text = "Data Bits";
                // 
                // textBoxBaudRate
                // 
                _textBoxBaudRate.Location = new System.Drawing.Point(170, 15);
                _textBoxBaudRate.Name = "textBoxBaudRate";
                _textBoxBaudRate.Size = new System.Drawing.Size(58, 20);
                _textBoxBaudRate.TabIndex = 20;
                // 
                // comboBoxParity
                // 
                _comboBoxParity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
                _comboBoxParity.FormattingEnabled = true;
                _comboBoxParity.Items.AddRange(new object[] {
                "None",
                "Even",
                "Odd"});
                _comboBoxParity.Location = new System.Drawing.Point(170, 44);
                _comboBoxParity.Name = "comboBoxParity";
                _comboBoxParity.Size = new System.Drawing.Size(58, 21);
                _comboBoxParity.TabIndex = 24;
                // 
                // comboBoxStopBits
                // 
                _comboBoxStopBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
                _comboBoxStopBits.FormattingEnabled = true;
                _comboBoxStopBits.Items.AddRange(new object[] {
                "0",
                "1",
                "2"});
                _comboBoxStopBits.Location = new System.Drawing.Point(170, 73);
                _comboBoxStopBits.Name = "comboBoxStopBits";
                _comboBoxStopBits.Size = new System.Drawing.Size(58, 21);
                _comboBoxStopBits.TabIndex = 25;
                // 
                // comboBoxDataBits
                // 
                _comboBoxDataBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
                _comboBoxDataBits.FormattingEnabled = true;
                _comboBoxDataBits.Items.AddRange(new object[] {
                "5",
                "7",
                "8"});
                _comboBoxDataBits.Location = new System.Drawing.Point(170, 102);
                _comboBoxDataBits.Name = "comboBoxDataBits";
                _comboBoxDataBits.Size = new System.Drawing.Size(58, 21);
                _comboBoxDataBits.TabIndex = 26;
                #endregion
                
                _tabPageACIAPort.Controls.Add(_labelBaudRate);
                _tabPageACIAPort.Controls.Add(_labelStopBits);
                _tabPageACIAPort.Controls.Add(_labelParity);
                _tabPageACIAPort.Controls.Add(_labelDataBits);
                _tabPageACIAPort.Controls.Add(_comboBoxParity);
                _tabPageACIAPort.Controls.Add(_textBoxBaudRate);
                _tabPageACIAPort.Controls.Add(_comboBoxDataBits);
                _tabPageACIAPort.Controls.Add(_comboBoxStopBits);
                _tabPageACIAPort.Controls.Add(_checkBoxInterruptEnabled);
            }
        }

        public List<TabPage> _tabPages = new List<TabPage>();
        
        private string currentSelectedNumberOfPorts = "";

        public FrmAciaConfiguration()
        {
            InitializeComponent();
        }
        
        private void frmACIAConfiguration_Load(object sender, EventArgs e)
        {
            textBoxBaseAddress.Text = BaseAddress;
            comboBoxNumberOfPorts.SelectedItem = (Convert.ToInt16(NumberOfBytes) / 2).ToString();
            comboBoxNumberOfPorts_SelectedValueChanged(sender, e);
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            BaseAddress = textBoxBaseAddress.Text;
            NumberOfBytes = (Convert.ToInt16(comboBoxNumberOfPorts.SelectedItem) * 2).ToString();
            for (int i = 0; i < tabControlPorts.TabPages.Count; i++)
            {
                TextBox txtBoxBaudRate = (TextBox)tabControlPorts.TabPages[i].Controls["textBoxBaudRate"];
                ComboBox cbDataBits    = (ComboBox)tabControlPorts.TabPages[i].Controls["comboBoxDataBits"];
                ComboBox cbStopBits    = (ComboBox)tabControlPorts.TabPages[i].Controls["comboBoxStopBits"];
                ComboBox cbParity      = (ComboBox)tabControlPorts.TabPages[i].Controls["comboBoxParity"];
                CheckBox ckInterrupt   = (CheckBox)tabControlPorts.TabPages[i].Controls["checkBoxInterruptEnabled"];

                int parity = 0;
                switch ((string)cbParity.SelectedItem)
                {
                    case "None": parity = 0; break;
                    case "Even": parity = 1; break;
                    case "Odd":  parity = 2; break;
                }

                if (aciaConfigs.Count <= i)
                    aciaConfigs.Add(new memulatorConfigEditor.ACIAConfig());

                if (txtBoxBaudRate.Text.Length > 0) aciaConfigs[i]._baudRate = Convert.ToInt32(txtBoxBaudRate.Text); else aciaConfigs[i]._baudRate = 9600;  // if blank set default
                aciaConfigs[i]._dataBits = Convert.ToInt16(cbDataBits.SelectedItem);
                aciaConfigs[i]._stopBits = Convert.ToInt16(cbStopBits.SelectedItem);
                aciaConfigs[i]._parity = parity;
                aciaConfigs[i]._interuptEnabled = ckInterrupt.Checked;
            }
        }

        private void comboBoxNumberOfPorts_SelectedValueChanged(object sender, EventArgs e)
        {
            if ((string)comboBoxNumberOfPorts.SelectedItem != currentSelectedNumberOfPorts)
            {
                if (tabControlPorts.TabPages.Count > Convert.ToInt16((string)comboBoxNumberOfPorts.SelectedItem))
                {
                    while (tabControlPorts.TabPages.Count > Convert.ToInt16((string)comboBoxNumberOfPorts.SelectedItem))
                    {
                        tabControlPorts.TabPages.RemoveAt(tabControlPorts.TabPages.Count - 1);
                        _tabPages.RemoveAt(tabControlPorts.TabPages.Count - 1);
                    }
                }
                else if (tabControlPorts.TabPages.Count < Convert.ToInt16((string)comboBoxNumberOfPorts.SelectedItem))
                {
                    int portNumber = tabControlPorts.TabPages.Count;
                    while (tabControlPorts.TabPages.Count < Convert.ToInt16((string)comboBoxNumberOfPorts.SelectedItem))
                    {
                        TabPage tp = new TabPage(++portNumber);

                        _tabPages.Add(tp);
                        tabControlPorts.TabPages.Add(tp._tabPageACIAPort);
                    }
                    
                    // now populate the controls

                    for (int i = 0; i < aciaConfigs.Count; i++)
                    {
                        string parityString = "None";
                        switch (aciaConfigs[i]._parity)
                        {
                            case 1: parityString = "Even"; break;
                            case 2: parityString = "Odd";  break;
                        }

                        TextBox txtBoxBaudRate = (TextBox)tabControlPorts.TabPages[i].Controls["textBoxBaudRate"];
                        ComboBox cbDataBits    = (ComboBox)tabControlPorts.TabPages[i].Controls["comboBoxDataBits"];
                        ComboBox cbStopBits    = (ComboBox)tabControlPorts.TabPages[i].Controls["comboBoxStopBits"];
                        ComboBox cbParity      = (ComboBox)tabControlPorts.TabPages[i].Controls["comboBoxParity"];
                        CheckBox ckInterrupt   = (CheckBox)tabControlPorts.TabPages[i].Controls["checkBoxInterruptEnabled"];

                        if (txtBoxBaudRate != null) txtBoxBaudRate.Text = aciaConfigs[i]._baudRate.ToString();

                        if (cbDataBits     != null) cbDataBits     .SelectedItem = aciaConfigs[i]._dataBits.ToString();
                        if (cbStopBits     != null) cbStopBits     .SelectedItem = aciaConfigs[i]._stopBits.ToString();
                        if (cbParity       != null) cbParity       .SelectedItem = parityString;

                        if (ckInterrupt    != null) ckInterrupt    .Checked = aciaConfigs[i]._interuptEnabled;
                    }
                }
            }
            else
            {
            }
            currentSelectedNumberOfPorts = (string)comboBoxNumberOfPorts.SelectedItem;
        }
        private void comboBoxNumberOfPorts_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void comboBoxNumberOfPorts_SelectionChangeCommitted(object sender, EventArgs e)
        {

        }
    }
}
