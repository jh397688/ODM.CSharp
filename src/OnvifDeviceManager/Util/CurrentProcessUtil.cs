using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OnvifDeviceManager.Util
{
    internal class CurrentProcessUtil
    {
        public static Process GetCurrentProcess()
        {
            return Process.GetCurrentProcess();
        }

        public static void DoCurrentProcessKill()
        {
            GetCurrentProcess().Kill();
        }

        public static Window GetMainWindow()
        {
            return Application.Current.MainWindow;
        }
    }
}
