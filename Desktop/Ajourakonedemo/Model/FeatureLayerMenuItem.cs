using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Layers;

namespace ArcGISRuntime.Samples.DesktopViewer.Model
{
    public class FeatureLayerMenuItem
    {
        public string Header { get; set; }

        public FeatureLayer Layer { get; set; }
    }
}
