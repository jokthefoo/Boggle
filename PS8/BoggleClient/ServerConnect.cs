using System;
using System.Windows.Forms;

namespace BoggleClient
{
    /// <summary>
    /// This is the GUI window for connecting to a server and inputting a name
    /// </summary>
    public partial class ServerConnect : Form
    {
        public ServerConnect()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Search for game event
        /// </summary>
        public event Action<string,string,int> StartButtonEvent;
        /// <summary>
        /// Cancel event
        /// </summary>
        public event Action CancelButtonEvent;

        /// <summary>
        /// Start button event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startButton_Click(object sender, EventArgs e)
        {
            if(StartButtonEvent != null)
            {
                StartButtonEvent(serverTextBox.Text,userNameTextBox.Text,int.Parse(lengthTextBox.Text));
            }
        }
        /// <summary>
        /// Set start button enabled
        /// </summary>
        public bool StartButtonStatus
        {
           set { startButton.Enabled = value; }
        }
        /// <summary>
        /// Set cancel button enabled
        /// </summary>
        public bool CancelButtonStatus
        {
            set { cancelButton.Enabled = value; }
        }
        /// <summary>
        /// Cancel button event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelButton_Click(object sender, EventArgs e)
        {
            if(CancelButtonEvent != null)
            {
                CancelButtonEvent();
            }
        }
        /// <summary>
        /// Handles closing of window, instead just hides it incase the person wants to connect to a new game
        /// </summary>
        public void DoClose()
        {
            Hide();
            serverTextBox.Text = "";
            userNameTextBox.Text = "";
            lengthTextBox.Text = "";
        }

        private void ServerConnect_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            DoClose();
        }
    }
}
