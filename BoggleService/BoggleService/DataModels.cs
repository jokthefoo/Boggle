using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Boggle
{
    [DataContract]
    public class Game
    {
        public int GameID { get; set; }

        [DataMember]
        public string GameState { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string GameBoard { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int TimeLeft { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int TimeLimit { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public User Player1 { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public User Player2 { get; set; }

        public double StartTime { get; set; }
    }

    [DataContract]
    public class User
    {
        [DataMember(EmitDefaultValue = false)]
        public string Nickname { get; set; }

        public string UserToken { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public List<Tuple<string, int>> PlayedWords { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int? score { get; set; }
    }

    public class PendingGame
    {
        public int GameID { get; set; }
        public string UserToken { get; set; }
        public int TimeLimit { get; set; }
    }
}