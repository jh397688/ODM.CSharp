using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OnvifDeviceManager.Util
{
    internal class AutoClosingMessageBox
    {
        System.Threading.Timer _timeoutTimer;
        string _caption;

        AutoClosingMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options, int timeInterval)
        {
            _caption = caption;
            _timeoutTimer = new System.Threading.Timer(OnTimerElapsed, null, timeInterval, System.Threading.Timeout.Infinite);

            using (_timeoutTimer)
            {
                MessageBox.Show(messageBoxText, caption, button, icon, defaultResult, options);

                Environment.Exit(0);
            }
        }

        public static void Show(string messageBoxText, string caption, int timeInterval)
        {
            Show(messageBoxText, caption, MessageBoxButton.OK, timeInterval);
        }

        public static void Show(string messageBoxText, string caption, MessageBoxButton button, int timeInterval)
        {
            Show(messageBoxText, caption, button, MessageBoxImage.None, timeInterval);
        }

        public static void Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, int timeInterval)
        {
            Show(messageBoxText, caption, button, icon, MessageBoxResult.None, timeInterval);
        }

        public static void Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defautResult, int timeInterval)
        {
            Show(messageBoxText, caption, button, icon, defautResult, MessageBoxOptions.None, timeInterval);
        }

        public static void Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options, int timeInterval)
        {
            new AutoClosingMessageBox(messageBoxText, caption, button, icon, defaultResult, options, timeInterval);
        }


        void OnTimerElapsed(object state)
        {
            IntPtr mbWnd = FindWindow("#32770", _caption);

            if (mbWnd != IntPtr.Zero)
            {
                SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }

            _timeoutTimer.Dispose();

            Environment.Exit(0);
        }

        const int WM_CLOSE = 0x0010;
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }
}
