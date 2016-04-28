using QuickGraph;

namespace ArcGISRuntime.Samples.DesktopViewer.Model
{
    public class GraphClass : UndirectedGraph<GraphVertexClass, GraphEdgeClass>
    {
        public GraphClass()
        {
        }

        public GraphClass(bool allowParallelEdges)
            : base(allowParallelEdges)
        {
        }

        public GraphClass(bool allowParallelEdges, EdgeEqualityComparer<GraphVertexClass, GraphEdgeClass> vertexCapacity)
            : base(allowParallelEdges, vertexCapacity)
        {
        }
    }
}
