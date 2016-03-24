using System;
using System.Windows.Forms;

namespace BoggleClient
{
    public partial class ServerConnect : Form
    {
        public ServerConnect()
        {
            InitializeComponent();
        }

        public event Action<string,string,int> StartButtonEvent;
        public event Action CancelButtonEvent;

        private void startButton_Click(object sender, EventArgs e)
        {
            if(StartButtonEvent != null)
            {
                StartButtonEvent(serverTextBox.Text,userNameTextBox.Text,int.Parse(lengthTextBox.Text));
            }
        }

        public bool StartButtonStatus
        {
           set { startButton.Enabled = value; }
        }

        public bool CancelButtonStatus
        {
            set { cancelButton.Enabled = value; }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            if(CancelButtonEvent != null)
            {
                CancelButtonEvent();
            }
        }

        public void DoClose()
        {
            Hide();
            serverTextBox.Text = "";
            userNameTextBox.Text = "";
            lengthTextBox.Text = "";
        }
    }
}
