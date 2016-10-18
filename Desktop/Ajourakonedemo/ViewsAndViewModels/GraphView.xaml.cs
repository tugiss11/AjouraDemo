using System.Windows;
using Catel.Windows;

namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    /// <summary>
    /// Interaction logic for GraphView.xaml
    /// </summary>
    public partial class GraphView : DataWindow
    {
        public GraphView()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            ((GraphViewModel)ViewModel).CreateGraphAsync();
        }
    }
}
