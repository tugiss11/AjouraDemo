using System.Windows;
using ArcGISRuntime.Samples.DesktopViewer.Services;
using Catel.Windows;
using Esri.ArcGISRuntime.Controls;

namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    public partial class MainWindow : DataWindow
    {
        public MainWindow() : base(DataWindowMode.Custom)
        {
            InitializeComponent();
            DataContext = ViewModel;

        }

        public MapView MyMapView
        {
            get
            {
                var service = Catel.IoC.ServiceLocator.Default.GetService(typeof (MapViewService)) as MapViewService;
                if (service != null)
                    return service.MapView;

                return null;
            }
        }

        private void MenuItem_OnClick__Restart(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
            System.Windows.Forms.Application.Restart();
        }

        private void MenuItem_OnClick__ShowLayerCount(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(string.Format("Map Layer Count: {0}", MyMapView.Map.Layers.Count));
        }

    
    }
    
}
