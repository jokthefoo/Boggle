using Microsoft.VisualStudio.TestTools.UnitTesting;
using static System.Net.HttpStatusCode;
using System.Diagnostics;
using System.IO;

namespace Boggle
{
    /// <summary>
    /// Provides a way to start and stop the IIS web server from within the test
    /// cases.  If something prevents the test cases from stopping the web server,
    /// subsequent tests may not work properly until the stray process is killed
    /// manually.
    /// </summary>
    public static class IISAgent
    {
        // Reference to the running process
        private static Process process = null;

        /// <summary>
        /// Starts IIS
        /// </summary>
        public static void Start(string arguments)
        {
            if (process == null)
            {
                ProcessStartInfo info = new ProcessStartInfo(Properties.Resources.IIS_EXECUTABLE, arguments);
                info.WindowStyle = ProcessWindowStyle.Minimized;
                info.UseShellExecute = false;
                process = Process.Start(info);
            }
        }

        /// <summary>
        ///  Stops IIS
        /// </summary>
        public static void Stop()
        {
            if (process != null)
            {
                process.Kill();
            }
        }
    }
    [TestClass]
    public class BoggleTests
    {
        /// <summary>
        /// This is automatically run prior to all the tests to start the server
        /// </summary>
        [ClassInitialize()]
        public static void StartIIS(TestContext testContext)
        {
            IISAgent.Start(@"/site:""BoggleService"" /apppool:""Clr4IntegratedAppPool"" /config:""..\..\..\.vs\config\applicationhost.config""");
        }

        /// <summary>
        /// This is automatically run when all tests have completed to stop the server
        /// </summary>
        [ClassCleanup()]
        public static void StopIIS()
        {
            IISAgent.Stop();
        }

        private RestTestClient client = new RestTestClient("http://localhost:60000/");

        [TestMethod]
        public void TestCreateUser1()
        {
            Response r = client.DoPostAsync("/users", null).Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        [TestMethod]
        public void TestCreateUser2()
        {
            Response r = client.DoPostAsync("/users", "test").Result;
            Assert.AreEqual(Created, r.Status);
            Assert.AreEqual(36, r.Data.Length);
        }

        [TestMethod]
        public void TestCreateUser3()
        {
            Response r = client.DoPostAsync("/users", "      ").Result;
            Assert.AreEqual(Forbidden, r.Status);
        }

        [TestMethod]
        public void TestJoinGame1()
        {
            TesterObject test = new TesterObject();
            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 5;
            Response r2 = client.DoPostAsync("/games", test).Result;
            Assert.AreEqual(Accepted, r2.Status);
            Assert.AreEqual("0", r2.Data);
        }

        [TestMethod]
        public void TestJoinGame2()
        {
            TesterObject test = new TesterObject();
            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = "invalid token";
            test.TimeLimit = 5;
            Response r2 = client.DoPostAsync("/games", test).Result;
            Assert.AreEqual(Forbidden, r2.Status);
        }


        [TestMethod]
        public void TestJoinGame3()
        {
            TesterObject test = new TesterObject();
            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 5000;
            Response r2 = client.DoPostAsync("/games", test).Result;
            Assert.AreEqual(Forbidden, r2.Status);
        }

        [TestMethod]
        public void TestJoinGame4()
        {
            TesterObject test = new TesterObject();
            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 5;
            Response r2 = client.DoPostAsync("/games", test).Result;

            Assert.AreEqual("0", r2.Data);

            Response r3 = client.DoPostAsync("/games", test).Result;

            Response r4 = client.DoPostAsync("/games", test).Result;

            Assert.AreEqual(Conflict, r4.Status);
        }

        [TestMethod]
        public void TestJoinGame5()
        {
            TesterObject test = new TesterObject();

            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 50;
            Response r2 = client.DoPostAsync("/games", test).Result;

            Assert.AreEqual(Created, r2.Status);
        }

        [TestMethod]
        public void TestCancelJoin1()
        {
            TesterObject test = new TesterObject();

            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 50;
            Response r2 = client.DoPostAsync("/games", test).Result;

            Response r3 = client.DoPutAsync(r.Data, "/games").Result;

            Assert.AreEqual(OK, r3.Status);
        }

        [TestMethod]
        public void TestCancelJoin2()
        {
            TesterObject test = new TesterObject();

            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 50;
            Response r2 = client.DoPostAsync("/games", test).Result;

            Response r3 = client.DoPutAsync("invalid usertoken", "/games").Result;
            Assert.AreEqual(Forbidden, r3.Status);
        }

        [TestMethod]
        public void TestPlayWord1()
        {
            TesterObject test = new TesterObject();

            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 50;
            Response r2 = client.DoPostAsync("/games", test).Result;

            Assert.AreEqual(Created, r2.Status);

            TesterObject test2 = new TesterObject();

            test2.Word = "";
            test2.UserToken = r.Data;

            string url = "/games/" + r2.Data;
            Response r4 = client.DoPutAsync(test2, url).Result;

            Assert.AreEqual(Forbidden, r4.Status);
        }

        [TestMethod]
        public void TestPlayWord2()
        {
            TesterObject test = new TesterObject();

            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 50;
            Response r2 = client.DoPostAsync("/games", test).Result;

            test.Word = "top";

            Response r4 = client.DoPutAsync(test, "/games/" + r2.Data).Result;

            Assert.AreEqual(Conflict, r4.Status);
        }

        [TestMethod]
        public void TestGameStatus1()
        {
            TesterObject test = new TesterObject();

            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 50;
            Response r2 = client.DoPostAsync("/games", test).Result;

            Response r1 = client.DoGetAsync("/games/" + r2.ToString(), "no").Result;

            Assert.AreEqual(Forbidden, r1.Status);
        }

        [TestMethod]
        public void TestGameStatus2()
        {
            TesterObject test = new TesterObject();

            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 50;
            Response r2 = client.DoPostAsync("/games", test).Result;

            Response r1 = client.DoGetAsync("/games/2", "no").Result;

            Assert.AreEqual(OK, r1.Status);
            Assert.AreEqual("active", r1.Data.GameState.ToString());
        }

        [TestMethod]
        public void TestGameStatus3()
        {
            TesterObject test = new TesterObject();

            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 50;
            Response r2 = client.DoPostAsync("/games", test).Result;

            Response r1 = client.DoGetAsync("/games/2", "yes").Result;

            Assert.AreEqual(OK, r1.Status);
            Assert.AreEqual("active", r1.Data.GameState.ToString());
        }

        [TestMethod]
        public void TestGameStatus4()
        {
            TesterObject test = new TesterObject();

            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 50;
            Response r2 = client.DoPostAsync("/games", test).Result;

            Response r1 = client.DoGetAsync("/games/" + r2.Data + "?Brief=yes").Result;

            Assert.AreEqual(OK, r1.Status);
            Assert.AreEqual("pending", r1.Data.GameState.ToString());

            Response r3 = client.DoGetAsync("/games/4?Brief=yes").Result;

            Assert.AreEqual("active", r3.Data.GameState.ToString());
        }

        [TestMethod]
        public void TestPlayWord3()
        {
            TesterObject test = new TesterObject();

            Response r = client.DoPostAsync("/users", "Testing").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 120;

            Response r2 = client.DoPostAsync("/games", test).Result;

            Response r3 = client.DoGetAsync("/games/" + r2.Data).Result;

            BoggleBoard b = new BoggleBoard(r3.Data.GameBoard.ToString());

            TesterObject test2 = new TesterObject();
            test2.UserToken = r.Data;
            string path =
            Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName).FullName;

            path = path + "\\BoggleService\\dictionary.txt";

            foreach (string s in System.IO.File.ReadLines(path))
            {
                if (b.CanBeFormed(s))
                {
                    test2.Word = s;
                    Response r4 = client.DoPutAsync(test2, "/games/" + r2.Data).Result;
                }
            }

            test2.Word = "test";

            Response r1 = client.DoPutAsync(test2, "/games/" + r2.Data).Result;

            Response r5 = client.DoGetAsync("/games/0").Result;

            Assert.AreEqual(OK, r1.Status);
        }

        [TestMethod]
        public void TestPlayWord4()
        {
            TesterObject test = new TesterObject();

            Response r = client.DoPostAsync("/users", "Tester").Result;
            test.UserToken = r.Data;
            test.TimeLimit = 120;

            Response r1 = client.DoPostAsync("/games", test).Result;
            Assert.AreEqual(Accepted, r1.Status);


            Response r2 = client.DoPostAsync("/users", "Tester").Result;
            test.UserToken = r2.Data;
            test.TimeLimit = 120;

            Response r3 = client.DoPostAsync("/games", test).Result;
            Assert.AreEqual(Created, r3.Status);

            test = new TesterObject();
            test.Word = "word";
            test.UserToken = r.Data;

            Response r4 = client.DoPutAsync(test, "/games/" + r1.Data).Result;
            Response r5 = client.DoPutAsync(test, "/games/" + r1.Data).Result;

            //Assert.AreEqual("0",r5.Data);
            //Assert.AreEqual(Conflict, r4.Status);
        }
    }

    public class TesterObject
    {
        public string UserToken { get; set; }
        public int? TimeLimit { get; set; }
        public string Word { get; set; }
    }
}
