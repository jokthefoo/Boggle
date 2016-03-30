using Boggle.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.ServiceModel.Web;
using static System.Net.HttpStatusCode;

namespace Boggle
{
    public class BoggleService : IBoggleService
    {
        /// <summary>
        /// Poor man's database
        /// </summary>
        private readonly static List<Game> games = new List<Game>();
        private readonly static List<User> users = new List<User>();
        private readonly static PendingGame pendingGame = new PendingGame();
        private static readonly object sync = new object();

        /// <summary>
        /// The most recent call to SetStatus determines the response code used when
        /// an http response is sent.
        /// </summary>
        /// <param name="status"></param>
        private static void SetStatus(HttpStatusCode status)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = status;
        }

        /// <summary>
        /// Returns a Stream version of index.html.
        /// </summary>
        /// <returns></returns>
        public Stream API()
        {
            SetStatus(OK);
            WebOperationContext.Current.OutgoingResponse.ContentType = "text/html";
            return File.OpenRead(AppDomain.CurrentDomain.BaseDirectory + "index.html");
        }

        /// <summary>
        /// Cancels pending join request
        /// </summary>
        public void CancelJoinRequest(string UserToken)
        {
            lock (sync)
            {
                User tempUser = LookupUserToken(UserToken);
                if (tempUser == null || UserToken != pendingGame.UserToken)
                {
                    SetStatus(Forbidden);
                }
                else
                {
                    SetStatus(OK);
                    pendingGame.UserToken = "";
                    pendingGame.TimeLimit = 0;
                }
            }
        }

        /// <summary>
        /// Creates a new user
        /// </summary>
        public string CreateUser(string Nickname)
        {
            lock (sync)
            {
                if (Nickname == null || Nickname.Trim(' ') == "")
                {
                    SetStatus(Forbidden);
                    return "";
                }
                else
                {
                    SetStatus(Created);
                    User newUser = new User();
                    newUser.Nickname = Nickname.Trim(' ');
                    newUser.UserToken = Guid.NewGuid().ToString();

                    users.Add(newUser);
                    return newUser.UserToken;
                }
            }
        }

        /// <summary>
        /// Gets a games status
        /// </summary>
        public Game GameStatus(string GameID, string Brief)
        {
            if (pendingGame.GameID.ToString() == GameID)
            {
                Game temp = new Game();
                temp.GameState = "pending";
                return temp;
            }

            Game currentGame = new Game();
            Game tempGame = new Game();
            lock (sync)
            {
                currentGame = LookupGameID(GameID);

                if(currentGame == null)
                {
                    SetStatus(Forbidden);
                    return null;
                }

                // Set the time left
                if (currentGame.StartTime + currentGame.TimeLimit > DateTime.Now.TimeOfDay.TotalSeconds)
                {
                    currentGame.TimeLeft = Convert.ToInt32(currentGame.StartTime + currentGame.TimeLimit - DateTime.Now.TimeOfDay.TotalSeconds);
                }
                else {
                    currentGame.TimeLeft = 0;
                    currentGame.GameState = "completed";
                }
            }


            if (Brief == "yes")
            {
                tempGame.GameState = currentGame.GameState;
                tempGame.TimeLeft = currentGame.TimeLeft;

                if (tempGame.Player1 == null)
                {
                    tempGame.Player1 = new User();
                }
                if (tempGame.Player2 == null)
                {
                    tempGame.Player2 = new User();
                }

                tempGame.Player1.score = currentGame.Player1.score;
                tempGame.Player2.score = currentGame.Player2.score;
            }
            else if (currentGame.GameState == "active")
            {
                tempGame.GameState = currentGame.GameState;
                tempGame.GameBoard = currentGame.GameBoard;
                tempGame.TimeLimit = currentGame.TimeLimit;
                tempGame.TimeLeft = currentGame.TimeLeft;

                if (tempGame.Player1 == null)
                {
                    tempGame.Player1 = new User();
                }
                if (tempGame.Player2 == null)
                {
                    tempGame.Player2 = new User();
                }

                tempGame.Player1.Nickname = currentGame.Player1.Nickname;
                tempGame.Player1.score = currentGame.Player1.score;
                tempGame.Player2.Nickname = currentGame.Player2.Nickname;
                tempGame.Player2.score = currentGame.Player2.score;
            }
            else
            {
                tempGame = currentGame;
            }

            return tempGame;
        }

        /// <summary>
        /// Joins a game
        /// </summary>
        public string JoinGame(string UserToken, int TimeLimit)
        {
            lock (sync)
            {
                if (pendingGame.UserToken == null)
                {
                    pendingGame.UserToken = "";
                    pendingGame.GameID = 0;
                    pendingGame.TimeLimit = 0;
                }

                User tempUser = LookupUserToken(UserToken);
                if (tempUser == null || TimeLimit < 5 || TimeLimit > 120)
                {
                    SetStatus(Forbidden);
                    return "";
                }
                else if (UserToken == pendingGame.UserToken)
                {
                    SetStatus(Conflict);
                    return "";
                }
                else if (pendingGame.UserToken == "")
                {
                    SetStatus(Accepted);
                    pendingGame.UserToken = UserToken;
                    pendingGame.TimeLimit = TimeLimit;
                    return pendingGame.GameID.ToString();
                }
                else
                {

                    SetStatus(Created);
                    //Create the new game
                    Game NewGame = new Game();
                    NewGame.Player1 = new User();
                    NewGame.Player2 = new User();

                    NewGame.GameID = pendingGame.GameID;
                    NewGame.GameState = "active";
                    NewGame.Player1.score = 0;
                    NewGame.Player2.score = 0;
                    NewGame.Player1.UserToken = pendingGame.UserToken;
                    NewGame.Player2.UserToken = UserToken;
                    NewGame.TimeLimit = (pendingGame.TimeLimit + TimeLimit) / 2;
                    NewGame.TimeLeft = NewGame.TimeLimit;
                    NewGame.StartTime = DateTime.Now.TimeOfDay.TotalSeconds;
                    NewGame.GameBoard = new BoggleBoard().ToString();

                    games.Add(NewGame);

                    //Reset the pending game
                    pendingGame.GameID++;
                    pendingGame.UserToken = "";
                    pendingGame.TimeLimit = 0;

                    return NewGame.GameID.ToString();
                }
            }
        }

        /// <summary>
        /// Looks up the given user token
        /// </summary>
        public User LookupUserToken(string UserToken)
        {
            foreach (User u in users)
            {
                if (u.UserToken == UserToken) return u;
            }
            return null;
        }

        /// <summary>
        /// Looks up the given user token
        /// </summary>
        public Game LookupGameID(string GameID)
        {
            foreach (Game g in games)
            {
                if (g.GameID.ToString() == GameID) return g;
            }
            return null;
        }

        /// <summary>
        /// Plays a word in a game
        /// </summary>
        /// <returns></returns>
        public string PlayWord(string GameID, string UserToken, string Word)
        {
            User tempUser = LookupUserToken(UserToken);
            Game tempGame = LookupGameID(GameID);
            Word = Word.Trim(' ');

            if (Word == null || Word == "" || tempUser == null || tempGame == null || (UserToken != tempGame.Player1.UserToken && UserToken != tempGame.Player2.UserToken && UserToken != pendingGame.UserToken))
            {
                if (pendingGame.GameID.ToString() == GameID)
                {
                    SetStatus(Conflict);
                    return "";
                }
                SetStatus(Forbidden);
                return "";
            }
            else if (tempGame.GameState != "active")
            {
                SetStatus(Conflict);
                return "";
            }
            else
            {
                SetStatus(OK);
                BoggleBoard board = new BoggleBoard(tempGame.GameBoard);
                int score = 0;
                if (tempGame.Player1.PlayedWords == null)
                {
                    tempGame.Player1.PlayedWords = new Dictionary<string, int>();
                }
                if (tempGame.Player2.PlayedWords == null)
                {
                    tempGame.Player2.PlayedWords = new Dictionary<string, int>();
                }

                // Check if the word can be formed in the board
                if (board.CanBeFormed(Word))
                {
                    if (Word.Length < 3)
                    {
                        score = 0;
                    }else if (UserToken == tempGame.Player1.UserToken)
                    {
                        foreach (string s in tempGame.Player1.PlayedWords.Keys)
                        {
                            if (Word.ToUpper() == s.ToUpper())
                            {
                                score = 0;
                                tempGame.Player1.PlayedWords.Add(Word, score);
                                return score.ToString();
                            }
                        }
                    }
                    else if (UserToken == tempGame.Player2.UserToken)
                    {
                        foreach (string s in tempGame.Player2.PlayedWords.Keys)
                        {
                            if (Word.ToUpper() == s.ToUpper())
                            {
                                score = 0;
                                tempGame.Player2.PlayedWords.Add(Word, score);
                                return score.ToString();
                            }
                        }
                    }

                    // Check the word in the dictionary
                    if (Resources.dictionary.Contains(Word.ToUpper()))
                    {
                        switch (Word.Length)
                        {
                            case 3:
                                score = 1;
                                break;
                            case 4:
                                score = 1;
                                break;
                            case 5:
                                score = 2;
                                break;
                            case 6:
                                score = 3;
                                break;
                            case 7:
                                score = 5;
                                break;
                            default:
                                score = 11;
                                break;
                        }
                    }
                }
                else
                {
                    score = -1;
                }

                // Add the word and its score to the words played list
                if (UserToken == tempGame.Player1.UserToken)
                {
                    tempGame.Player1.PlayedWords.Add(Word, score);
                    tempGame.Player1.score += score;
                }
                else
                {
                    tempGame.Player2.PlayedWords.Add(Word, score);
                    tempGame.Player2.score += score;
                }

                return score.ToString();
            }
        }
    }
}
