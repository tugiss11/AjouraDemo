using System.Windows;
using Catel.MVVM;

namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    public class NotificationViewModel : ViewModelBase
    {
        public Command<Window> AvaaViestitCommand { get; private set; }

 

        public NotificationViewModel()
        {
            AvaaViestitCommand = new Command<Window>(OnAvaaViestitCommand);
        
        }
        private void OnAvaaViestitCommand(Window window)
        {
          HideWindow(window);
            MessageBox.Show("Hello!");
        }

        private void HideWindow(Window window)
        {
            if (window != null)
            {
                window.Hide();
            }
        }
    }
}
