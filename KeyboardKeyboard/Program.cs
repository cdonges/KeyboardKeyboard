using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeyboardKeyboard
{
    class Program
    {
        private static void Main(string[] args)
        {
            LogKeys.Do(null);
            Application.Run(new ApplicationContext());
        }


        private static void Exit(Action quit)
        {
            Application.Exit();
        }
    }
}
