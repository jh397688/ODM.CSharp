using OnvifDeviceManager.Util;
using OnvifDeviceManager.Util.UI;
using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace OnvifDeviceManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public App()
        {
            #region SetGlobalException
            this.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            #endregion
        }

        #region SetGlobalExceptionMethod
        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            new ExceptionMessageBox("UI 쓰레드에서 알 수 없는 예외 발생", "App", "Dispatcher_UnhandledException", e.Exception).ShowDialog();

            CurrentProcessUtil.DoCurrentProcessKill();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            new ExceptionMessageBox("비-UI 쓰레드에서 알 수 없는 예외 발생", "App", "CurrentDomain_UnhandledException", (Exception)e.ExceptionObject).ShowDialog();

            CurrentProcessUtil.DoCurrentProcessKill();
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            new ExceptionMessageBox("Task 에서 알 수 없는 예외 발생", "App", "TaskScheduler_UnobservedTaskException", e.Exception).ShowDialog();

            CurrentProcessUtil.DoCurrentProcessKill();
        }
        #endregion

        protected override void ConfigureViewModelLocator()
        {
            base.ConfigureViewModelLocator();

            ViewModelLocationProvider.SetDefaultViewTypeToViewModelTypeResolver((viewType) =>
            {
                string viewDir = viewType.FullName.Replace(".View.", ".View.");

                int offset = 0;

                if (viewDir.EndsWith("Window") || viewDir.EndsWith("Dialog"))
                    offset = 6;
                else if (viewDir.EndsWith("View"))
                    offset = 4;
                else if (viewDir.EndsWith("UC"))
                    offset = 2;

                string viewName = viewDir.Substring(0, viewDir.Length - offset);
                var viewAssemblyName = viewType.GetTypeInfo().Assembly.FullName;

                return Type.GetType($"{viewName}VM, {viewAssemblyName}");
            });
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            ManagerRegisterTypes(containerRegistry);
            NavigationRegisterTypes(containerRegistry);
        }

        private void ManagerRegisterTypes(IContainerRegistry containerRegistry)
        {
            
        }

        private void NavigationRegisterTypes(IContainerRegistry containerRegistry)
        {
            
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Process proc = Process.GetCurrentProcess();
            int count = Process.GetProcesses().Where(p =>
                             p.ProcessName == proc.ProcessName).Count();
            if (count > 1)
            {
                AutoClosingMessageBox.Show(
                    "프로그램이 이미 실행중 입니다.\n10초 뒤 자동 종료됩니다.", 
                    "실행 에러", 
                    MessageBoxButton.OK, MessageBoxImage.Error, 1000 * 10);
                
                CurrentProcessUtil.DoCurrentProcessKill();
            }

            try
            {
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                new ExceptionMessageBox("실행중 알 수 없는 예외 발생", "App", "OnStartup", ex).Show();

                CurrentProcessUtil.DoCurrentProcessKill();
            }
        }
    }
}
