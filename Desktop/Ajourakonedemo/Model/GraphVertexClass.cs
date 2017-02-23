using System;
using Esri.ArcGISRuntime.Geometry;

namespace ArcGISRuntime.Samples.DesktopViewer.Model
{
    public class GraphVertexClass
    {
        public int ID { get; set; }

        public double? X { get; set; }
        public double? Y { get; set; }


        /// <summary>
        /// Puumäärä dm3
        /// </summary>
        public int Puumaara { get; set; }

        public GraphVertexClass(int id)
        {
            ID = id;
        }

        public GraphVertexClass(int id, double x, double y)
        {
            ID = id;
            X = x;
            Y = y;
            Puumaara = 1;
        }

        public override string ToString()
        {
            return string.Format("{0}: X:{1} Y:{2}", ID, X, Y);
        }

        public MapPoint AsMapPoint()
        {
            if (X != null && Y != null)
            {
                return new MapPoint((double)X, (double)Y, new SpatialReference(3067));
            }
            return null;
        }

        public bool EqualsMapPointCoordinates(MapPoint mapPoint)
        {
            return Convert.ToInt32(X) == Convert.ToInt32(mapPoint.X) && Convert.ToInt32(Y) == Convert.ToInt32(mapPoint.Y);
        }

        public long[] Distances;
        public int[] Neighbours;
    }
}
