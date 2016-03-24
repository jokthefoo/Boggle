using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoggleClient
{
    class Game
    {
        public Game()
        {
            Player1 = new Player();
            Player2 = new Player();
        }
        public string GameState { get; set; }
        public string Board { get; set; }
        public int TimeLimit { get; set; }
        public int TimeLeft { get; set; }
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }

    }
    class Player
    {
        public string Nickname { get; set; }
        public int Score { get; set; }
        public string WordsPlayed { get; set; }
    }
}
