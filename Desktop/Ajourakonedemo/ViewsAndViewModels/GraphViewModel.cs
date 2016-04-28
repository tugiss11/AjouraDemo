using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGISRuntime.Samples.DesktopViewer.Model;
using ArcGISRuntime.Samples.DesktopViewer.Utils;
using Catel.Data;
using Esri.ArcGISRuntime.Geometry;
using GraphSharp.Controls;
using QuickGraph;

namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    public class GraphViewModel : ViewModelBase
    {
        public AdjacencyGraph<MapPoint, TaggedEdge<MapPoint, string>> Graph
        {
            get { return GetValue<AdjacencyGraph<MapPoint, TaggedEdge<MapPoint, string>>>(GraphProperty); }
            set { SetValue(GraphProperty, value); }
        }
        public static readonly PropertyData GraphProperty = RegisterProperty("GraphDebug", typeof(AdjacencyGraph<MapPoint, TaggedEdge<MapPoint, string>>), null);
        public List<string> LayoutAlgorithmTypes 
        {
            get { return GetValue<List<string>>(LayoutAlgorithmTypesProperty); }
            set { SetValue(LayoutAlgorithmTypesProperty, value); }
        }

        public static readonly PropertyData LayoutAlgorithmTypesProperty = RegisterProperty("layoutAlgorithmTypes", typeof(List<string>), null);
        public string LayoutAlgorithmType
        {
            get { return GetValue<string>(LayoutAlgorithmTypeProperty); }
            set { SetValue(LayoutAlgorithmTypeProperty, value); }
        }
        public static readonly PropertyData LayoutAlgorithmTypeProperty = RegisterProperty("LayoutAlgorithmType", typeof(string), null);

        public BidirectionalGraph<int, TaggedEdge<int, string>> GraphDebug
        {
            get { return GetValue<BidirectionalGraph<int, TaggedEdge<int, string>>>(GraphDebugProperty); }
            set { SetValue(GraphDebugProperty, value); }
        }

        public static readonly PropertyData GraphDebugProperty = RegisterProperty("GraphDebug", typeof(BidirectionalGraph<int, TaggedEdge<int, string>>), null);

        public GraphClassBidirectional GraphDebug2
        {
            get { return GetValue<GraphClassBidirectional>(GraphDebug2Property); }
            set { SetValue(GraphDebug2Property, value); }
        }

        public static readonly PropertyData GraphDebug2Property = RegisterProperty("GraphDebug2", typeof(GraphClassBidirectional), null);

        public GraphViewModel(GraphClassBidirectional g)
        {
            GraphDebug2 = g;

            SetLayoutAlgorithm();
        }

        private void SetLayoutAlgorithm()
        {
            LayoutAlgorithmTypes = new List<string>();
            LayoutAlgorithmTypes.Add("BoundedFR");
            LayoutAlgorithmTypes.Add("Circular");
            LayoutAlgorithmTypes.Add("CompoundFDP");
            LayoutAlgorithmTypes.Add("EfficientSugiyama");
            LayoutAlgorithmTypes.Add("FR");
            LayoutAlgorithmTypes.Add("ISOM");
            LayoutAlgorithmTypes.Add("KK");
            LayoutAlgorithmTypes.Add("LinLog");
            LayoutAlgorithmTypes.Add("Tree");

            //Pick a default Layout Algorithm Type
            LayoutAlgorithmType = "LinLog";
        }

        internal void CreateDebugGraph()
        {
            var first = new GraphVertexClass(1);
            var second = new GraphVertexClass(2);
            var third = new GraphVertexClass(3);

            var graph = new GraphClassBidirectional(true);
            graph.AddVerticesAndEdge(new GraphEdgeClass(1, first, second));
            graph.AddVerticesAndEdge(new GraphEdgeClass(2, first, third));
            graph.AddVerticesAndEdge(new GraphEdgeClass(3, second, third));
            GraphDebug2 = graph;
            RaisePropertyChanged("GraphDebug2");
        }

        public async Task CreateGraphAsync()
        {
            var graph = await GraphUtils.Instance.AddFeatureLayersToGraph();
            GraphDebug2 = graph.FirstOrDefault();
        }
    }

    public class ViewModelGraphLayout : GraphLayout<GraphVertexClass, GraphEdgeClass, GraphClassBidirectional> { }
}
