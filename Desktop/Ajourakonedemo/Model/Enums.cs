using System.ComponentModel;

namespace ArcGISRuntime.Samples.DesktopViewer.Model
{


    public enum KookoajauraTyyppi
    {
        [Description("Using visited edges")]
        UsingVisitedEdges = 1,
        [Description("Using shortest paths")]
        UsingShortestPaths = 2,
        [Description("Using buffered zones")]
        UsingBuffers = 3
    }

}
