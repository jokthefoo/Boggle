using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoggleClient
{
    /// <summary>
    /// Object to hold info about the user
    /// </summary>
    class User
    {
        public string Nickname { get; set; }
        public string UserToken { get; set; }
        public string GameID { get; set; }
        public int TimeLimit { get; set; }
        public string Word { get; set; }
        public int WordScore { get; set; }
    }
}
