using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace BoggleClient
{
    public partial class BoggleClient : Form
    {
        public event Action<string> inputWordEvent;
        public event Action timerEvent;
        public event Action newGameEvent;
        public BoggleClient()
        {
            InitializeComponent();
        }

        public void Message(string message)
        {
            MessageBox.Show(message);
        }

        public string UpdateP1ScoreBoard
        {
            set { p1scoreLabel.Text = "Score: " + value.ToString(); }
        }
        public string UpdateP2ScoreBoard
        {
            set { p2scoreLabel.Text = "Score: " + value.ToString(); }
        }
        public string Player1WordsPlayed
        {
            set { player1WordsPlayed.Text = value; }
        }

        public string Player2WordsPlayed
        {
            set { player2WordsPlayed.Text = value; }
        }
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

        public string UpdateTime
        {
            set{ timeLabel.Text = "Time: " + value.ToString(); }
        }

        public string Player1Label
        {
            set { player1Label.Text = value.ToString(); }
        }
        public string Player2Label
        {
            set { player2Label.Text = value.ToString(); }
        }
        public string InputBox
        {
            get { return answersInputBox.Text; }
            set { answersInputBox.Text = value; }
        }

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

        private void timer_Tick(object sender, EventArgs e)
        {
            if(timerEvent != null)
            {
                timerEvent();
            }
        }

        private void newGameMenu_Click(object sender, EventArgs e)
        {
            if(newGameEvent !=null)
            {
                newGameEvent();
            }
        }
    }
}
