﻿using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BoggleClient
{
    class Controller
    {
        /// <summary>
        /// This is the connection window, Allows you to chose server and name and time
        /// </summary>
        private ServerConnect connectWindow;
        /// <summary>
        /// This is the main window
        /// </summary>
        private BoggleClient mainWindow;
        /// <summary>
        /// This is the server address
        /// </summary>
        private string server;
        /// <summary>
        /// This is an object that holds information about the current user
        /// </summary>
        private User user;
        /// <summary>
        /// This is an object that holds information about the current game
        /// </summary>
        private Game game;
        /// <summary>
        /// Used to cancel requests
        /// </summary>
        private bool Cancel;
        /// <summary>
        /// Used for setup
        /// </summary>
        private bool setup;

        /// <summary>
        /// This is the controller for the GUI, needs a main window and the server connect window
        /// </summary>
        public Controller(ServerConnect connectWindow, BoggleClient mainWindow)
        {
            this.connectWindow = connectWindow;
            this.mainWindow = mainWindow;

            connectWindow.StartButtonEvent += HandleStartClick;
            connectWindow.CancelButtonEvent += HandleCancelClick;
            mainWindow.inputWordEvent += HandleWordInput;
            mainWindow.timerEvent += HandleTimer;
            mainWindow.newGameEvent += HandleNewGame;
            mainWindow.helpEvent += HandleHelpEvent;
        }

        /// <summary>
        /// Handles when help event is fired
        /// </summary>
        private void HandleHelpEvent()
        {
            mainWindow.Message("Press the new game button to disconnect from this game and have the option to connect to another. \n To send a word type it into the input box and press enter.\n To exit completely use the X in the corner on the main window.");
        }

        /// <summary>
        /// This handles when the new game button is clicked
        /// </summary>
        private void HandleNewGame()
        {
            mainWindow.Player2WordsPlayed = "";
            mainWindow.Player1WordsPlayed = "";
            Cancel = true;
            mainWindow.TimerStatus(false);
            connectWindow.StartButtonStatus = true;
            connectWindow.CancelButtonStatus = false;
            connectWindow.Show();
        }

        /// <summary>
        /// This is the timer that asks the server for info every second while playing or searching for a game
        /// </summary>
        private async void HandleTimer()
        {
            Task<Game> statusTask = new Task<Game>(() => GetStatus("yes"));
            statusTask.Start();
            game = await statusTask;
            if (!Cancel)
            {
                if (game.GameState != "pending")
                {
                    if (setup)
                    {
                        Task<Game> Task = new Task<Game>(() => GetStatus("no"));
                        Task.Start();
                        game = await Task;

                        mainWindow.cubes(game.Board);
                        mainWindow.Player1Label = game.Player1.Nickname;
                        mainWindow.Player2Label = game.Player2.Nickname;
                        mainWindow.UpdateP1ScoreBoard = game.Player1.Score.ToString();
                        mainWindow.UpdateP2ScoreBoard = game.Player2.Score.ToString();
                        mainWindow.UpdateTime = game.TimeLeft.ToString();

                        connectWindow.StartButtonStatus = true;
                        connectWindow.CancelButtonStatus = false;
                        Cancel = false;
                        connectWindow.DoClose();
                        setup = false;
                    }

                    mainWindow.UpdateP1ScoreBoard = game.Player1.Score.ToString();
                    mainWindow.UpdateP2ScoreBoard = game.Player2.Score.ToString();
                    mainWindow.UpdateTime = game.TimeLeft.ToString();

                    if (game.GameState == "completed")
                    {
                        Task<Game> Task = new Task<Game>(() => GetStatus("no"));
                        Task.Start();
                        game = await Task;

                        mainWindow.UpdateP1ScoreBoard = game.Player1.Score.ToString();
                        mainWindow.UpdateP2ScoreBoard = game.Player2.Score.ToString();
                        mainWindow.UpdateTime = game.TimeLeft.ToString();
                        var pat = new string[] { "[", "]", "{", "}", ",", "\""};
                        foreach (var c in pat)
                        {
                            game.Player1.WordsPlayed = game.Player1.WordsPlayed.Replace(c, "");
                            game.Player2.WordsPlayed = game.Player2.WordsPlayed.Replace(c, "");
                        }
                        mainWindow.Player1WordsPlayed = game.Player1.WordsPlayed;
                        mainWindow.Player2WordsPlayed = game.Player2.WordsPlayed;
                        mainWindow.TimerStatus(false);
                    }
                }
            }
        }

        /// <summary>
        /// This handles when a word is sent
        /// </summary>
        private async void HandleWordInput(string word)
        {
            user.Word = word;
            Task<User> wordTask = new Task<User>(() => PutPlayWord());
            wordTask.Start();
            user.WordScore = (await wordTask).WordScore;
        }

        /// <summary>
        /// Handles when you cancel a request
        /// </summary>
        private void HandleCancelClick()
        {
            Cancel = true;
            mainWindow.TimerStatus(false);
            PutCancelJoin();
            connectWindow.StartButtonStatus = true;
            connectWindow.CancelButtonStatus = false;
        }

        /// <summary>
        /// This handles when you start searching for a game
        /// </summary>
        private async void HandleStartClick(string server, string name, int time)
        {
            user = new User();
            user.TimeLimit = time;
            user.Nickname = name;

            this.server = server;

            connectWindow.StartButtonStatus = false;
            connectWindow.CancelButtonStatus = true;
            Cancel = false;
            setup = true;

            Task<User> userTask = new Task<User>(() => PostUser());
            userTask.Start();
            user = await userTask;

            Task<User> joinTask = new Task<User>(() => PostJoin());
            joinTask.Start();
            user = await joinTask;

            Task<Game> statusTask = new Task<Game>(() => GetStatus("no"));
            statusTask.Start();
            game = await statusTask;

            if (game.GameState != "pending")
            {
                mainWindow.cubes(game.Board);
                mainWindow.Player1Label = game.Player1.Nickname;
                mainWindow.Player2Label = game.Player2.Nickname;
                mainWindow.UpdateP1ScoreBoard = game.Player1.Score.ToString();
                mainWindow.UpdateP2ScoreBoard = game.Player2.Score.ToString();
                mainWindow.UpdateTime = game.TimeLeft.ToString();
                
                connectWindow.StartButtonStatus = true;
                connectWindow.CancelButtonStatus = false;
                Cancel = false;
                connectWindow.DoClose();
            }
            mainWindow.TimerStatus(true);
        }

        /// <summary>
        /// This is used to create an HTTP client for requests
        /// </summary>
        /// <returns></returns>
        public HttpClient CreateClient()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(server);
            return client;
        }

        /// <summary>
        /// This handles the POST request to create a user
        /// </summary>
        /// <returns></returns>
        private User PostUser()
        {
            User tempUser = new User();
            using (HttpClient client = CreateClient())
            {
                tempUser.Nickname = user.Nickname;
                tempUser.TimeLimit = user.TimeLimit;

                StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync("/BoggleService.svc/users", content).Result;
                if(Cancel)
                {
                    return null;
                }
                if (response.IsSuccessStatusCode)
                {
                    String result = response.Content.ReadAsStringAsync().Result;
                    dynamic serverObject = JsonConvert.DeserializeObject(result);

                    tempUser.UserToken = serverObject.UserToken.ToString();
                }
                else
                {
                    mainWindow.Message("Error: " + response.StatusCode);
                }
            }
            return tempUser;
        }

        /// <summary>
        /// This handles the POST request to join a game
        /// </summary>
        /// <returns></returns>
        public User PostJoin()
        {
            User tempUser = new User();
            using (HttpClient client = CreateClient())
            {
                tempUser.Nickname = user.Nickname;
                tempUser.TimeLimit = user.TimeLimit;
                tempUser.UserToken = user.UserToken;

                StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PostAsync("/BoggleService.svc/games", content).Result;
                if (Cancel)
                {
                    return null;
                }
                if (response.IsSuccessStatusCode)
                {
                    String result = response.Content.ReadAsStringAsync().Result;
                    dynamic serverObject = JsonConvert.DeserializeObject(result);

                    tempUser.GameID = serverObject.GameID.ToString();
                }
                else
                {
                    mainWindow.Message("Error: " + response.StatusCode);
                }
            }
            return tempUser;
        }

        /// <summary>
        /// This handles cancelling a request
        /// </summary>
        public void PutCancelJoin()
        {
            using (HttpClient client = CreateClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PutAsync("/BoggleService.svc/games", content).Result;

                if (response.IsSuccessStatusCode)
                {
                    mainWindow.Message("Cancelled finding a game.");
                }
                else
                {
                    mainWindow.Message("Error: " + response.StatusCode);
                }
            }
        }

        /// <summary>
        /// This handles the PUT request to play a word
        /// </summary>
        /// <returns></returns>
        public User PutPlayWord()
        {
            User tempUser = new User();
            using (HttpClient client = CreateClient())
            {
                StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                HttpResponseMessage response = client.PutAsync("/BoggleService.svc/games/"+user.GameID, content).Result;

                if (response.IsSuccessStatusCode)
                {
                    String result = response.Content.ReadAsStringAsync().Result;
                    dynamic serverObject = JsonConvert.DeserializeObject(result);

                    tempUser.WordScore = serverObject.Score;
                }
                else
                {
                    mainWindow.Message("Error: " + response.StatusCode);
                }
            }
            return tempUser;
        }

        /// <summary>
        /// This handles getting the status of a game
        /// </summary>
        /// <param name="brief"></param>
        /// <returns></returns>
        public Game GetStatus(string brief)
        {
            Game tempGame = new Game();
            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = client.GetAsync("/BoggleService.svc/games/" + user.GameID + "?Brief="+brief).Result;
                if (Cancel)
                {
                    return null;
                }
                if (response.IsSuccessStatusCode)
                {
                    String result = response.Content.ReadAsStringAsync().Result;
                    dynamic serverObject = JsonConvert.DeserializeObject(result);

                    tempGame.GameState = serverObject.GameState.ToString();

                    if (serverObject.GameState.ToString() != "pending")
                    {
                        if (serverObject.GameState.ToString() == "active" && brief != "yes")
                        {
                            tempGame.Board = serverObject.Board.ToString();
                            tempGame.TimeLeft = serverObject.TimeLeft;
                            tempGame.Player1.Nickname = serverObject.Player1.Nickname.ToString();
                            tempGame.Player2.Nickname = serverObject.Player2.Nickname.ToString();
                            tempGame.Player1.Score = serverObject.Player1.Score;
                            tempGame.Player2.Score = serverObject.Player2.Score;
                        }
                        else if (brief == "yes")
                        {
                            tempGame.TimeLeft = serverObject.TimeLeft;
                            tempGame.Player1.Score = serverObject.Player1.Score;
                            tempGame.Player2.Score = serverObject.Player2.Score;
                        }
                        else if (serverObject.GameState.ToString() == "completed" && brief != "yes")
                        {
                            tempGame.Board = serverObject.Board.ToString();
                            tempGame.Player1.Nickname = serverObject.Player1.Nickname.ToString();
                            tempGame.Player2.Nickname = serverObject.Player2.Nickname.ToString();
                            tempGame.Player1.Score = serverObject.Player1.Score;
                            tempGame.Player2.Score = serverObject.Player2.Score;
                            tempGame.Player1.WordsPlayed = serverObject.Player1.WordsPlayed.ToString();
                            tempGame.Player2.WordsPlayed = serverObject.Player2.WordsPlayed.ToString();
                        }
                    }
                }
            }
            return tempGame;
        }
    }
}
