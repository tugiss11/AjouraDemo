using System;
using System.Windows.Forms;
using Catel.Windows;
using Window = System.Windows.Window;

namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    /// <summary>
    /// Interaction logic for NotificationWindow.xaml
    /// </summary>
    public partial class NotificationView : DataWindow
    {
        public NotificationView() : base(DataWindowMode.Custom)
        {
            InitializeComponent();
            this.Closed += this.NotificationWindowClosed;
        }

        public new void Show()
        {
            this.Topmost = true;
            base.Show();

            this.Owner = System.Windows.Application.Current.MainWindow;
            this.Closed += this.NotificationWindowClosed;
            var workingArea = Screen.PrimaryScreen.WorkingArea;

            this.Left = (workingArea.Right/2) - (this.ActualWidth/2);
            double top = (workingArea.Bottom/2) - (this.ActualHeight / 2);

            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                string windowName = window.GetType().Name;

                if (windowName.Equals("NotificationWindow") && window != this)
                {
                    window.Topmost = true;
                    top = (workingArea.Top / 2) + (this.ActualHeight / 2);
                }
            }

            this.Top = top;
        }
        private void ImageMouseUp(object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void DoubleAnimationCompleted(object sender, EventArgs e)
        {
           
        }

        private void NotificationWindowClosed(object sender, EventArgs e)
        {
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                string windowName = window.GetType().Name;

                if (windowName.Equals("NotificationWindow") && window != this)
                {
                    // Adjust any windows that were above this one to drop down
                    if (window.Top < this.Top)
                    {
                        window.Top = window.Top + this.ActualHeight;
                    }
                }
            }
        }
    }
}
