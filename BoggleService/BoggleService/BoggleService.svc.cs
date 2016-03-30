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
        // Poor man's database
        /// <summary>
        /// Stores all the games
        /// </summary>
        private readonly static List<Game> games = new List<Game>();
        /// <summary>
        /// Stores all the users
        /// </summary>
        private readonly static List<User> users = new List<User>();
        /// <summary>
        /// Keeps track of the currently pending game
        /// </summary>
        private readonly static PendingGame pendingGame = new PendingGame();
        /// <summary>
        /// Object used for syncing threads
        /// </summary>
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
        /// Cancel a pending request to join a game.
        /// 
        /// If UserToken is invalid or is not a player in the pending game, responds with status 403 (Forbidden).
        /// 
        /// Otherwise, removes UserToken from the pending game and responds with status 200 (OK).
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
        /// Creates a new user. 
        /// 
        /// If Nickname is null, or is empty when trimmed, responds with status 403 (Forbidden).
        /// 
        /// Otherwise, creates a new user with a unique UserToken and the trimmed Nickname. 
        /// The returned UserToken should be used to identify the user in subsequent requests. Responds with status 201 (Created).
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
        /// Get game status information.
        /// 
        /// If GameID is invalid, responds with status 403 (Forbidden).
        /// 
        /// Otherwise, returns information about the game named by GameID as illustrated below. 
        /// Note that the information returned depends on whether "Brief=yes" was included as a 
        /// parameter as well as on the state of the game. Responds with status code 200 (OK). 
        /// Note: The Board and Words are not case sensitive.
        /// </summary>
        public Game GameStatus(string GameID, string Brief)
        {
            lock (sync)
            {
                if (pendingGame.GameID.ToString() == GameID)
                {
                    Game temp = new Game();
                    temp.GameState = "pending";
                    return temp;
                }

                Game currentGame = new Game();
                Game tempGame = new Game();

                currentGame = LookupGameID(GameID);

                if (currentGame == null)
                {
                    SetStatus(Forbidden);
                    return null;
                }

                // Set the time left
                if (currentGame.StartTime + currentGame.TimeLimit > DateTime.Now.TimeOfDay.TotalSeconds)
                {
                    currentGame.TimeLeft = Convert.ToInt32(currentGame.StartTime + currentGame.TimeLimit - DateTime.Now.TimeOfDay.TotalSeconds);
                }
                else
                {
                    currentGame.TimeLeft = 0;
                    currentGame.GameState = "completed";
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
        }

        /// <summary>
        /// Join a game. 
        /// 
        /// If UserToken is invalid, TimeLimit < 5, or TimeLimit > 120, responds with status 403 (Forbidden).
        /// 
        /// Otherwise, if UserToken is already a player in the pending game, responds with status 409 (Conflict).
        /// 
        /// Otherwise, if there is already one player in the pending game, adds UserToken as the second player. 
        /// The pending game becomes active and a new pending game with no players is created. 
        /// The active game's time limit is the integer average of the time limits requested by the two players. 
        /// Returns the new active game's GameID (which should be the same as the old pending game's GameID). Responds with status 201 (Created).
        /// 
        /// Otherwise, adds UserToken as the first player of the pending game, and the TimeLimit as the pending game's requested time limit. 
        /// Returns the pending game's GameID. Responds with status 202 (Accepted).
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
        /// Play a word in a game.
        /// 
        /// If Word is null or empty when trimmed, or if GameID or UserToken is missing or invalid, 
        /// or if UserToken is not a player in the game identified by GameID, responds with response code 403 (Forbidden).
        /// 
        /// Otherwise, if the game state is anything other than "active", responds with response code 409 (Conflict).
        /// 
        /// Otherwise, records the trimmed Word as being played by UserToken in the game identified by GameID. 
        /// Returns the score for Word in the context of the game (e.g. if Word has been played before the score is zero). 
        /// Responds with status 200 (OK). Note: The word is not case sensitive.
        /// </summary>
        public string PlayWord(string GameID, string UserToken, string Word)
        {
            lock (sync)
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
                        tempGame.Player1.PlayedWords = new List<Tuple<string, int>>();
                    }
                    if (tempGame.Player2.PlayedWords == null)
                    {
                        tempGame.Player2.PlayedWords = new List<Tuple<string, int>>();
                    }

                    // Check if the word can be formed in the board
                    if (board.CanBeFormed(Word))
                    {
                        if (Word.Length < 3)
                        {
                            score = 0;
                        }
                        //Check if the word has been played already
                        else if (UserToken == tempGame.Player1.UserToken)
                        {
                            foreach (var s in tempGame.Player1.PlayedWords)
                            {
                                if (Word.ToUpper() == s.Item1.ToUpper())
                                {
                                    score = 0;
                                    var temp = new Tuple<string,int>(Word,score);
                                    tempGame.Player1.PlayedWords.Add(temp);
                                    return score.ToString();
                                }
                            }
                        }
                        else if (UserToken == tempGame.Player2.UserToken)
                        {
                            foreach (var s in tempGame.Player2.PlayedWords)
                            {
                                if (Word.ToUpper() == s.Item1.ToUpper())
                                {
                                    score = 0;
                                    var temp = new Tuple<string, int>(Word, score);
                                    tempGame.Player2.PlayedWords.Add(temp);
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
                        var temp = new Tuple<string, int>(Word, score);
                        tempGame.Player1.PlayedWords.Add(temp);
                        tempGame.Player1.score += score;
                    }
                    else
                    {
                        var temp = new Tuple<string, int>(Word, score);
                        tempGame.Player2.PlayedWords.Add(temp);
                        tempGame.Player2.score += score;
                    }

                    return score.ToString();
                }
            }
        }
    }
}
