using System.Windows;
using Catel.MVVM;

namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    public class NotificationViewModel : ViewModelBase
    {
        public Command<Window> SeuraavaViivaCommand { get; private set; }
        public Command<Window> SeuraavaSolmuCommand { get; private set; }

        public Command<Window> SuljeCommand { get; private set; }

        public NotifyResult NotifyViewModelResult { get; set; }

        public enum NotifyResult
        {
            NextNode = 1,
            NextLine = 2,
            Close = 3,
            Wait = 4
        }

        public NotificationViewModel()
        {
            SuljeCommand = new Command<Window>(OnSuljeCommand);
            SeuraavaViivaCommand = new Command<Window>(OnSeuraavaViivaCommand);
            SeuraavaSolmuCommand = new Command<Window>(OnSeuraavaSolmuCommand);
            NotifyViewModelResult = NotifyResult.Wait;

        }
        private void OnSeuraavaViivaCommand(Window window)
        {
            HideWindow(window);
            NotifyViewModelResult = NotifyResult.NextLine;
        }

        private void OnSeuraavaSolmuCommand(Window window)
        {
            HideWindow(window);
            NotifyViewModelResult = NotifyResult.NextNode;
        }

        private void OnSuljeCommand(Window window)
        {
            HideWindow(window);
            NotifyViewModelResult = NotifyResult.Close;
        }

        private void HideWindow(Window window)
        {
            if (window != null)
            {
                window.Hide();
            }
        }

        public void Show()
        {
            throw new System.NotImplementedException();
        }
    }
}
