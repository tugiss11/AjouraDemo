using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Geometry;

namespace ArcGISRuntime.Samples.DesktopViewer.Utils
{
    public class SmoothUtils
    {
        private static readonly SmoothUtils _instance = new SmoothUtils();
        private static double smoothFactor = 0.33;
        private static double smoothFactor2 = 1.00 - smoothFactor;
        private static double SmoothingCount = 3;

        public static SmoothUtils Instance
        {
            get { return _instance; }
        }
        /// <summary>
        /// Chaikin smooth
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Geometry SmoothPolyline(Polyline path)
        {
            var tempResult = GetSmoothenedLines(path);
            var i = 0;
            while (i < 3)
            {
             
               
                var tempGeo = GetSmoothenedLines((Polyline)tempResult);
                
                tempResult = tempGeo;
                i++;
            }
            return tempResult;

        }

        private static Geometry GetSmoothenedLines(Polyline path)
        {
            var tempResult = new List<Geometry>();
            foreach (var part in path.Parts)
            {
                var output = new PolylineBuilder(new SpatialReference(3067));
                output.AddPoint((MapPoint) GeometryEngine.Project(part.StartPoint, new SpatialReference(3067)));
                foreach (var segment in part)
                {
                    var p0 = segment.StartPoint;
                    var p1 = segment.EndPoint;
                    var newStartPoint = new MapPoint(smoothFactor2*p0.X + smoothFactor*p1.X, smoothFactor2*p0.Y + smoothFactor*p1.Y);
                    var newEndPoint = new MapPoint(smoothFactor*p0.X + smoothFactor2*p1.X, smoothFactor*p0.Y + smoothFactor2*p1.Y);
                    output.AddPoint(newStartPoint);
                    output.AddPoint(newEndPoint);
                }
                output.AddPoint((MapPoint) GeometryEngine.Project(part.EndPoint, new SpatialReference(3067)));
                tempResult.Add(output.ToGeometry());
            }
            return GeometryEngine.Union(tempResult);
        }
    }
}
