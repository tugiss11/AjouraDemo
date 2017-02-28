using Esri.ArcGISRuntime.Geometry;

namespace ArcGISRuntime.Samples.DesktopViewer.Extensions
{
    public static class Extensions
    {
        public static Geometry ToSr3067(this Geometry geo)
        {
            return GeometryEngine.Project(geo, new SpatialReference(3067));
        }

    }
}
