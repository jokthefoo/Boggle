using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BoggleClient
{
    class BoggleApplicationContext : ApplicationContext
    {
        private static BoggleApplicationContext context;

        private BoggleApplicationContext()
        {
        }

        public static BoggleApplicationContext GetContext()
        {
            if(context == null)
            {
                context = new BoggleApplicationContext();
            }
            return context;
        }

        public void Run()
        {
            BoggleClient mainWindow = new BoggleClient();
            ServerConnect connectWindow = new ServerConnect();
            new Controller(connectWindow,mainWindow);

            mainWindow.FormClosed += (o, e) => { ExitThread(); };

            mainWindow.Show();
            connectWindow.Show();
        }
    }
}
