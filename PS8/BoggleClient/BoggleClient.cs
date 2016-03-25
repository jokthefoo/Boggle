using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BoggleClient
{
    /// <summary>
    /// This is the main window for the Boggle client
    /// </summary>
    public partial class BoggleClient : Form
    {
        /// <summary>
        /// This event fires when a word is sent
        /// </summary>
        public event Action<string> inputWordEvent;
        /// <summary>
        /// This event fires when the timer ticks
        /// </summary>
        public event Action timerEvent;
        /// <summary>
        /// This event fires when the new game button is clicked
        /// </summary>
        public event Action newGameEvent;
        /// <summary>
        /// This event is when you hover over the help menu
        /// </summary>
        public event Action helpEvent;
        public BoggleClient()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Allows the controller to display a message (usually used for errors)
        /// </summary>
        /// <param name="message"></param>
        public void Message(string message)
        {
            MessageBox.Show(message);
        }

        /// <summary>
        /// Update the player 1's score
        /// </summary>
        public string UpdateP1ScoreBoard
        {
            set { p1scoreLabel.Text = "Score: " + value.ToString(); }
        }
        /// <summary>
        /// Update the player 2's score
        /// </summary>
        public string UpdateP2ScoreBoard
        {
            set { p2scoreLabel.Text = "Score: " + value.ToString(); }
        }
        /// <summary>
        /// Fills in the words played box for player 1
        /// </summary>
        public string Player1WordsPlayed
        {
            set { player1WordsPlayed.Text = value; }
        }
        /// <summary>
        /// Fills in the words played box for player 2
        /// </summary>
        public string Player2WordsPlayed
        {
            set { player2WordsPlayed.Text = value; }
        }
        /// <summary>
        /// Allows the controller to start and stop a timer
        /// </summary>
        /// <param name="enabled"></param>
        public void TimerStatus(bool enabled)
        {
            if(enabled)
            {
                timer.Start();
            }
            else
            {
                timer.Stop();
            }
        }
        /// <summary>
        /// Controls the countdown timer
        /// </summary>
        public string UpdateTime
        {
            set{ timeLabel.Text = "Time: " + value.ToString(); }
        }
        /// <summary>
        /// Controls the player1's name label
        /// </summary>
        public string Player1Label
        {
            set { player1Label.Text = value.ToString(); }
        }
        /// <summary>
        /// Controls player 2's name label
        /// </summary>
        public string Player2Label
        {
            set { player2Label.Text = value.ToString(); }
        }
        /// <summary>
        /// Controls the input box
        /// </summary>
        public string InputBox
        {
            get { return answersInputBox.Text; }
            set { answersInputBox.Text = value; }
        }
        /// <summary>
        /// Sets the board using a board string
        /// aka "CBEFTGSEGTZXCVET"
        /// </summary>
        /// <param name="input"></param>
        public void cubes(string input)
        {
            List<Label> cubes = new List<Label>();
            cubes.Add(cube1);
            cubes.Add(cube2);
            cubes.Add(cube3);
            cubes.Add(cube4);
            cubes.Add(cube5);
            cubes.Add(cube6);
            cubes.Add(cube7);
            cubes.Add(cube8);
            cubes.Add(cube9);
            cubes.Add(cube10);
            cubes.Add(cube11);
            cubes.Add(cube12);
            cubes.Add(cube13);
            cubes.Add(cube14);
            cubes.Add(cube15);
            cubes.Add(cube16);
            int r = 0;
            foreach(char c in input)
            {
                if (c == 'Q')
                {
                    cubes.ElementAt(r).Text = "QU";
                }
                else
                {
                    cubes.ElementAt(r).Text = c.ToString();
                }
                r++;
            }
        }
        /// <summary>
        /// Fires inputWordEvent when enter is pressed, also stops the annoying windows sound from going off when you press enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void answersInputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Return)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if(inputWordEvent != null)
                {
                    inputWordEvent(answersInputBox.Text);
                    answersInputBox.Text = "";
                }
            }
        }
        /// <summary>
        /// Timer event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e)
        {
            if(timerEvent != null)
            {
                timerEvent();
            }
        }
        /// <summary>
        /// New game event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newGameMenu_Click(object sender, EventArgs e)
        {
            if(newGameEvent !=null)
            {
                newGameEvent();
            }
        }

        /// <summary>
        /// Help event
        /// </summary>
        private void helpMenu_Click(object sender, EventArgs e)
        {
            if(helpEvent != null)
            {
                helpEvent();
            }
        }
    }
}
