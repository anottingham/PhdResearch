using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NetMQ;

namespace ZmqInterface
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (NetMQContext context = NetMQContext.Create())
            {
                using (NetMQSocket socket = context.CreateRequestSocket())
                {
                    Application.Run(new ConnectionForm(context, socket));
                }
            }
        }
    }
}
