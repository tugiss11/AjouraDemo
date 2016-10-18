﻿using Esri.ArcGISRuntime.Geometry;

namespace ArcGISRuntime.Samples.DesktopViewer.Model
{
    public class GraphVertexClass
    {
        public int ID { get; set; }

        public double? X { get; set; }
        public double? Y { get; set; }

        public GraphVertexClass(int id)
        {
            ID = id;
        }

        public GraphVertexClass(int id, double x, double y)
        {
            ID = id;
            X = x;
            Y = y;
        }

        public override string ToString()
        {
            return string.Format("{2}: X:{1} Y:{0}", ID, X, Y);
        }

        public MapPoint AsMapPoint()
        {
            if (X != null && Y != null)
            {
                return new MapPoint((double)X, (double)Y, new SpatialReference(3067));
            }
            return null;
        }
    }
}