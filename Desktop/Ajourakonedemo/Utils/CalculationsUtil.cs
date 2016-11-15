using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGISRuntime.Samples.DesktopViewer.Model;
using ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;

namespace ArcGISRuntime.Samples.DesktopViewer.Utils
{
    class CalculationsUtil
    {
        private static readonly CalculationsUtil _instance = new CalculationsUtil();
        public static CalculationsUtil Instance
        {
            get { return _instance; }
        }

        public double CalculateTotalLength(GraphicCollection resultList)
        {
            var graphicsList = new List<Graphic>(resultList);
            var geometries = graphicsList.Select(o => o.Geometry);
            var unionGeometry = GeometryEngine.Union(geometries);
            return Math.Abs(GeometryEngine.Length(unionGeometry));
        }

        public double CalculateKokoajaUraTotalLength(List<List<GraphEdgeClass>> kokoajauraList)
        {
            var lenghtSum = 0.0;
            foreach (var kokoajaura in kokoajauraList)
            {
                var kokoajauraLineGeometry = GeometryEngine.Union(MapUtils.Instance.GetGeometriesWithIdsFromGraphicLayer(kokoajaura.Select(o => o.Id))) as Polyline;
                lenghtSum = lenghtSum + Math.Abs(GeometryEngine.Length(kokoajauraLineGeometry));
            }
            return lenghtSum;
        }
    }
}
