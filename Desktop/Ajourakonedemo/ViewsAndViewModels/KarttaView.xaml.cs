using Catel.Windows.Controls;

namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    public partial class KarttaView : UserControl
    {
        public KarttaView()
        {
            InitializeComponent();
            CloseViewModelOnUnloaded = false;
            Loaded += Window_Loaded;
        }

        public KarttaViewModel KarttaViewModel
        {
            set { DataContext = value; }
            get { return (KarttaViewModel)ViewModel; }
        }

        void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ////Jostain syystä Mapin binding viewmodeliin ei toimi suoraan kuten KarttaViewin puolela joten asetetaan se täällä myös
            ////TODO Selvitä syy
            //if (this.MapView == null)
            //{
            //}
            //else if (MapView.Map == null)
            //{

            //    var vm = this.ViewModel as KarttaViewModel;
            //    if (vm != null)
            //    {
            //        MapView.Map = vm.Map;
            //        vm.MapInitializeAsync();
            //    }
            //}
          
            e.Handled = true;

        }



    }
}
