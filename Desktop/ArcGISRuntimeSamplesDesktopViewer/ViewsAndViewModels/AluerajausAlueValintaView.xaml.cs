using System;
using Catel.Windows;
using Telerik.Windows.Controls;

namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    /// <summary>
    /// Interaction logic for AluerajausAlueValintaView.xaml
    /// </summary>
    public partial class AluerajausAlueValintaView : DataWindow
    {
        private bool _isFirstDataLoad = true;

        public AluerajausAlueValintaView()
            : base(DataWindowMode.Custom)
        {
            AddCustomButton(new DataWindowButton("Valitse", "OkCommand"));
            AddCustomButton(new DataWindowButton("Sulje", "SuljeCommand"));
            InitializeComponent();
        }

        private void FeaturesGridView_OnDataLoaded(object sender, EventArgs e)
        {
            if (_isFirstDataLoad)
            {
                _isFirstDataLoad = false;

                foreach (var column in this.FeaturesGridView.Columns)
                {

                    String columnName = column.UniqueName;
                    if (columnName.Length > 10)
                    {
                        column.Width = 70;
                    }
                    else
                    {
                        column.Width = GridViewLength.SizeToHeader;
                    }

                }
                
            }
        }
    }
}
