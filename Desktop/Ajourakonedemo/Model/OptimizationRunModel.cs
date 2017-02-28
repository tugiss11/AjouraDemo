using System.Collections.Generic;
using ArcGISRuntime.Samples.DesktopViewer.SQLite;
using Catel.Data;

namespace ArcGISRuntime.Samples.DesktopViewer.Model
{
    public class OptimizationRunModel : ModelBase
    {

        public double UraTotalLength
        {
            get { return GetValue<double>(UraTotalLengthProperty); }
            set { SetValue(UraTotalLengthProperty, value); }
        }
        public static readonly PropertyData UraTotalLengthProperty = RegisterProperty("UraTotalLength", typeof(double));

        public double KokoajauraTotalLength
        {
            get { return GetValue<double>(KokoajauraTotalLengthProperty); }
            set { SetValue(KokoajauraTotalLengthProperty, value); }
        }
        public static readonly PropertyData KokoajauraTotalLengthProperty = RegisterProperty("KokoajauraTotalLength", typeof(double));


        public int Capacity { get; set; }



        public int SlopeMultiplier
        {
            get { return GetValue<int>(SlopeMultiplierProperty); }
            set { SetValue(SlopeMultiplierProperty, value); }
        }

        public static readonly PropertyData SlopeMultiplierProperty = RegisterProperty("Capacity", typeof(int));


        public int WetnessMultiplier
        {
            get { return GetValue<int>(WetnessMultiplierProperty); }
            set { SetValue(WetnessMultiplierProperty, value); }
        }

        public static readonly PropertyData WetnessMultiplierProperty = RegisterProperty("WetnessMultiplier", typeof(int));


        public bool UseLocalDistances
        {
            get { return GetValue<bool>(UseLocalDistancesProperty); }
            set { SetValue(UseLocalDistancesProperty, value); }
        }

        public static readonly PropertyData UseLocalDistancesProperty = RegisterProperty("UseLocalDistances", typeof(bool));

        public bool UseVisitedEdges
        {
            get { return GetValue<bool>(UseVisitedEdgesProperty); }
            set { SetValue(UseVisitedEdgesProperty, value); }
        }

        public static readonly PropertyData UseVisitedEdgesProperty = RegisterProperty("UseVisitedEdges", typeof(bool));


        [Ignore]
        public List<List<long>> OrderLists
        {
            get { return GetValue<List<List<long>>>(OrderListsProperty); }
            set { SetValue(OrderListsProperty, value); }
        }

        public static readonly PropertyData OrderListsProperty = RegisterProperty("OrderLists", typeof(List<List<long>>));

        [Ignore]
        public List<GraphVertexClass> Vertices
        {
            get { return GetValue<List<GraphVertexClass>>(VerticesProperty); }
            set { SetValue(VerticesProperty, value); }
        }



        public static readonly PropertyData VerticesProperty = RegisterProperty("Vertices", typeof(List<GraphVertexClass>));

        public int StartVertice
        {
            get { return GetValue<int>(StartVerticeProperty); }
            set { SetValue(StartVerticeProperty, value); }
        }

        public static readonly PropertyData StartVerticeProperty = RegisterProperty("StartVertice", typeof(int));

        [Ignore]
        public GraphVertexClass Root
        {
            get { return GetValue<GraphVertexClass>(RootProperty); }
            set { SetValue(RootProperty, value); }
        }

        public static readonly PropertyData RootProperty = RegisterProperty("Root", typeof(GraphVertexClass));

        public bool UseShortestPaths
        {
            get { return GetValue<bool>(UseShortestPathsProperty); }
            set { SetValue(UseShortestPathsProperty, value); }
        }

        public static readonly PropertyData UseShortestPathsProperty = RegisterProperty("UseShortestPaths", typeof(bool));

        public int Method
        {
            get { return GetValue<int>(MethodProperty); }
            set { SetValue(MethodProperty, value); }
        }

        public static readonly PropertyData MethodProperty = RegisterProperty("Method", typeof(int));


        public string GeometryJson
        {
            get { return GetValue<string>(GeometryJsonProperty); }
            set { SetValue(GeometryJsonProperty, value); }
        }

        public static readonly PropertyData GeometryJsonProperty = RegisterProperty("GeometryJson", typeof(string));

        public override string ToString()
        {
            return string.Format("ID: {0}-{1}-{2}-{3}", Method, UraTotalLength, SlopeMultiplier, WetnessMultiplier);
        }

    }
}
