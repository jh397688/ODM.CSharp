using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OnvifDeviceManager.Util.UI
{
    /// <summary>
    /// ExceptiomMessageBox.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ExceptionMessageBox : Window
    {
        private bool _detailsVisible = false;

        public ExceptionMessageBox(string customMsg, string className, string methodName, Exception ex)
        {
            InitializeComponent();

            this.Owner = CurrentProcessUtil.GetMainWindow();

            CustomMsgTBK.Text = customMsg ?? "(메시지 없음)";
            ClassNameTBK.Text = className ?? "(알 수 없음)";
            MethodNameTBK.Text = methodName ?? "(알 수 없음)";

            if (ex != null)
            {
                EXMessageTBK.Text = ex.Message;
                EXStackTraceTBK.Text = ex.StackTrace;
            }
            else
            {
                EXMessageTBK.Text = "(예외 객체가 없습니다)";
                EXStackTraceTBK.Text = "";
            }
        }

        private void ViewDetailInfoBTN_Click(object sender, RoutedEventArgs e)
        {
            _detailsVisible = !_detailsVisible;
            if (_detailsVisible)
            {
                DetailContainer.Visibility = Visibility.Visible;
                ViewDetailInfoBTN.Content = "간략히 보기 ▲";
            }
            else
            {
                DetailContainer.Visibility = Visibility.Collapsed;
                ViewDetailInfoBTN.Content = "자세히 보기 ▼";
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
