using QuickGraph;

namespace ArcGISRuntime.Samples.DesktopViewer.Model
{
    public class GraphClassBidirectional : BidirectionalGraph<GraphVertexClass, GraphEdgeClass>
    {
        public GraphClassBidirectional()
        {
        }

        public GraphClassBidirectional(bool allowParallelEdges)
            : base(allowParallelEdges)
        {
        }

        public GraphClassBidirectional(bool allowParallelEdges, int vertexCapacity)
            : base(allowParallelEdges, vertexCapacity)
        {
        }
    }
}
