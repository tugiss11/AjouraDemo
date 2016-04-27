using Catel.Windows.Controls;

namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    public partial class KarttaView : UserControl
    {
        public KarttaView()
        {
            InitializeComponent();
            CloseViewModelOnUnloaded = false;
        }

        public KarttaViewModel KarttaViewModel
        {
            set { DataContext = value; }
            get { return (KarttaViewModel)ViewModel; }
        }

       
    }
}
