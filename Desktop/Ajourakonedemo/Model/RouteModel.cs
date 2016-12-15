using System.Collections.Generic;
using ArcGISRuntime.Samples.DesktopViewer.SQLite;
using Catel.Data;
using Esri.ArcGISRuntime.Geometry;

namespace ArcGISRuntime.Samples.DesktopViewer.Model
{
    public class RouteModel : ModelBase
    {

        public Geometry Geometry { get; set; }
        
        public double Pituus { get; set; }

        public int Puumaaraa { get; set; }
      
        public int Id { get; set; }
      

        public override string ToString()
        {
            return string.Format("ID:{0}, {1}m, {2} dm3", Id, Pituus, Puumaaraa);
        }

    }
}
