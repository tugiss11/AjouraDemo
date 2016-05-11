using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels;
using Catel.IO;
using Catel.IoC;
using Catel.Logging;
using Catel.MVVM;
using Path = Catel.IO.Path;

namespace ArcGISRuntime.Samples.DesktopViewer
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#if DEBUG
            LogManager.AddDebugListener();
#endif
            const string folderpath = "C:\\temp\\";
            const string logFileName = "TestApp.log";
            LogManager.AddListener(new FileLogListener(Path.Combine(folderpath, logFileName), 2048)
            {
                IsDebugEnabled = true
            });


            var viewModelLocator = ServiceLocator.Default.ResolveType<IViewModelLocator>();

            viewModelLocator.Register(typeof(KarttaView), typeof(KarttaViewModel));
            viewModelLocator.Register(typeof(GraphView), typeof(GraphViewModel));
            viewModelLocator.Register(typeof(AluerajausAlueValintaView), typeof(AluerajausAlueValintaViewModel));
            viewModelLocator.Register(typeof(MainWindow), typeof(MainWindowViewModel));
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Debug.WriteLine("An unhandled exception occured!");
            Debug.WriteLine(string.Format("Error : {0}", exception));
            MessageBox.Show(string.Format(exception.Message), "An exception occured");
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
            Debug.WriteLine("An unhandled exception occured!");
            Debug.WriteLine(string.Format("Error : {0}", e.Exception));
            MessageBox.Show(string.Format(e.Exception.Message), "An exception occured");
            e.Handled = true;
		}
	}
}
