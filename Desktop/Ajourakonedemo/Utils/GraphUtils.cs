using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ArcGISRuntime.Samples.DesktopViewer.Model;
using ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2;
using Catel.IoC;
using Catel.Messaging;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using QuickGraph;
using QuickGraph.Algorithms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

// ReSharper disable CompareOfFloatsByEqualityOperator


namespace ArcGISRuntime.Samples.DesktopViewer.Utils
{
    public class GraphUtils
    {
        private static readonly GraphUtils _instance = new GraphUtils();
        TSP2.Tsp tsp;

        public static GraphUtils Instance
        {
            get { return _instance; }
        }

        internal List<ShortestPath> ShortestPathList { get; set; }
        internal List<MapPoint> GraphVerticesAsMapPoint { get; set; }
        internal List<List<GraphEdgeClass>> KokoajauraList { get; set; }

        private GraphClass _graph;

        public GraphClass Graph
        {
            get
            {
                return _graph;
            }
            set { _graph = value; }
        }

        public bool UseOnlyDistances { get; set; }
        public bool UseVisitedEdges { get; set; }

        public async Task<List<GraphClassBidirectional>> AddFeatureLayersToGraph()
        {
            var graphList = new List<GraphClassBidirectional>();
            var lineLayers = MapUtils.Instance.Map.Layers.OfType<FeatureLayer>().Where(o => o.FeatureTable.GeometryType == GeometryType.Polyline);
            foreach (var layer in lineLayers)
            {
                var features = await layer.FeatureTable.QueryAsync(new QueryFilter() { WhereClause = "1=1" });
                graphList.Add(AddFeaturesToGraph(features, layer.FeatureTable.ObjectIDField));
            }

            return graphList;

        }

        private GraphClassBidirectional AddFeaturesToGraph(IEnumerable<Feature> features, string objectIdField)
        {
            var mapPointDictionary = new Dictionary<int, MapPoint>();
            int counter = 1;
            var g = new GraphClass(false);

            foreach (var feature in features)
            {
                if (feature.Geometry != null && feature.Geometry.GeometryType == GeometryType.Polyline)
                {
                    var startPoint = ((Polyline)feature.Geometry).Parts.FirstOrDefault().StartPoint;
                    var endPoint = ((Polyline)feature.Geometry).Parts.FirstOrDefault().EndPoint;

                    var startId = CheckIfPointExistsInDictionary(mapPointDictionary, startPoint, ref counter);
                    var endId = CheckIfPointExistsInDictionary(mapPointDictionary, endPoint, ref counter);

                    var startVertex = g.Vertices.FirstOrDefault(o => o.ID == startId);
                    if (startVertex == null)
                    {
                        startVertex = new GraphVertexClass(startId, startPoint.X, startPoint.Y);
                    }

                    var endVertex = g.Vertices.FirstOrDefault(o => o.ID == endId);
                    if (endVertex == null)
                    {
                        endVertex = new GraphVertexClass(endId, endPoint.X, endPoint.Y);
                    }
                    var kaltevuus = Convert.ToDouble(feature.Attributes["sivukalt"]);

                    var edge = new GraphEdgeClass(Convert.ToInt32(feature.Attributes[objectIdField]), kaltevuus, startVertex, endVertex);
                    g.AddVerticesAndEdge(edge);
                }
            }
            Graph = g;
            InitMapPointList(g);
            var returnvalue = new GraphClassBidirectional();
            returnvalue.AddVertexRange(g.Vertices);
            returnvalue.AddEdgeRange(g.Edges);


            return returnvalue;
        }

        private void InitMapPointList(GraphClass graphClass)
        {
            GraphVerticesAsMapPoint = new List<MapPoint>();
            foreach (var vertex in graphClass.Vertices)
            {
                if (vertex.X != null && vertex.Y != null)
                {
                    GraphVerticesAsMapPoint.Add(new MapPoint((double)vertex.X, (double)vertex.Y, new SpatialReference(3067)));
                }
            }
        }

        private static int CheckIfPointExistsInDictionary(Dictionary<int, MapPoint> mapPointDictionary, MapPoint startPoint, ref int counter)
        {
            int startId;
            var existingPoint = mapPointDictionary.FirstOrDefault(o => o.Value.X == startPoint.X && o.Value.Y == startPoint.Y);
            if (existingPoint.Value != null)
            {
                startId = existingPoint.Key;
            }
            else
            {
                startId = counter;
                counter++;
                mapPointDictionary.Add(startId, startPoint);
            }
            return startId;
        }

        public void GetMinimumSpanningTree()
        {
            if (Graph == null)
            {
                return;
            }
            IUndirectedGraph<GraphVertexClass, GraphEdgeClass> g = Graph;
            Func<GraphEdgeClass, double> edgeWeights = EdgeWeights;
            IEnumerable<GraphEdgeClass> mst = g.MinimumSpanningTreePrim(edgeWeights);
            MapUtils.Instance.HighlightEdges(mst);

        }

        private double EdgeWeights(GraphEdgeClass graphEdgeClass)
        {
            var weight = 0.0;
            var weightMultiplier = 10;
            if (graphEdgeClass.Sivukaltevuus != null)
            {
                if (!UseOnlyDistances)
                {
                    weight = 17 + weightMultiplier * Math.Abs((double)graphEdgeClass.Sivukaltevuus);
                }
                else
                {
                    weight = 17;
                }

            }
            if (UseVisitedEdges && graphEdgeClass.IsVisited)
            {
                weight = weight * 0.25;
            }

            graphEdgeClass.Weight = weight;
            return weight;
        }

        public void AddKokoajauraFromStartPointToEndPoint(MapPoint startingVertex, MapPoint endingVertex)
        {

            if (Graph == null)
            {
                MessageBox.Show("Graph is null");
                return;
            }

            var root = Graph.Vertices.FirstOrDefault(o => Convert.ToInt32(o.X) == Convert.ToInt32(startingVertex.X) && Convert.ToInt32(o.Y) == Convert.ToInt32(startingVertex.Y));
            var target = Graph.Vertices.FirstOrDefault(o => Convert.ToInt32(o.X) == Convert.ToInt32(endingVertex.X) && Convert.ToInt32(o.Y) == Convert.ToInt32(endingVertex.Y));

            var edgeList = ShortestPathAlgorithm(root, target);
            AddEdgeListToKokoajaUraList(edgeList);


        }

        private void AddEdgeListToKokoajaUraList(List<GraphEdgeClass> edgeList)
        {
            KokoajauraList.Add(edgeList);
        }

        public List<GraphEdgeClass> ShortestPathAlgorithm(GraphVertexClass root, GraphVertexClass target)
        {
            Func<GraphEdgeClass, double> edgeWeights = EdgeWeights;

            // compute shortest paths
            TryFunc<GraphVertexClass, IEnumerable<GraphEdgeClass>> tryGetPaths = Graph.ShortestPathsDijkstra(edgeWeights, root);

            // query path for given vertices
            var edgleList = new List<GraphEdgeClass>();

            IEnumerable<GraphEdgeClass> path;
            if (tryGetPaths(target, out path))
                foreach (var edge in path)
                {
                    edgleList.Add(edge);
                }
            return edgleList;
        }

        public void TspAlgorithm(CancellationToken token, int population, int maxgenerations, List<GraphVertexClass> graphVertexList, GraphVertexClass startingVertex = null)
        {

            // we are already running, so tell the tsp thread to halt.
            if (tsp != null)
            {
                tsp.Halt = true;
            }
            try
            {
                var cityList = new TSPVertices();
                if (graphVertexList == null)
                {
                    graphVertexList = Graph.Vertices.ToList();
                }
                AddTspVerticesFromGraphVertices(cityList, graphVertexList, startingVertex);


                int maxGenerations;
                int mutation;
                int groupSize;
                int seed;
                int numberOfCloseCities;
                int chanceUseCloseCity;
                var populationSize = SetParameters(population, maxgenerations, out maxGenerations, out mutation, out groupSize, out seed, out numberOfCloseCities, out chanceUseCloseCity);
                cityList.CalculateVertexDistances(token, numberOfCloseCities);

                tsp = new TSP2.Tsp();
                tsp.FoundNewBestTour += tsp_foundNewBestTour;
                tsp.newCalcStarted += tsp_newCalcStarted;
                tsp.Begin(token, populationSize, maxGenerations, groupSize, mutation, seed, chanceUseCloseCity, cityList);
                tsp.FoundNewBestTour -= tsp_foundNewBestTour;
                tsp.newCalcStarted -= tsp_newCalcStarted;

            }
            finally
            {
                tsp = null;
                ResetFields();
            }

        }

        private static int SetParameters(int population, int maxgenerations, out int maxGenerations, out int mutation, out int groupSize, out int seed, out int numberOfCloseCities, out int chanceUseCloseCity)
        {
            int populationSize = population;
            maxGenerations = maxgenerations;
            mutation = 3;
            groupSize = 5;
            seed = 0;
            numberOfCloseCities = 6;
            chanceUseCloseCity = 90;
            return populationSize;
        }

        private void ResetFields()
        {
            _tourCount = 0;
            _oldThousand = 0;
            _oldFitness = 0.0;
            _tourToShowCount = 0;
        }

        private void GetVerticesAsTspVertices(TSPVertices tspVertexList)
        {
            var verticeList = Graph.Vertices.ToList();

            foreach (var vertice in verticeList)
            {
                if (vertice.ID < 10)
                {
                    tspVertexList.Add(new TSPVertice(Convert.ToInt32(vertice.X), Convert.ToInt32(vertice.Y), vertice.ID));
                }
            }
        }

        internal async Task<GraphVertexClass> AskUserForVertex()
        {
            try
            {

                this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage("Set point to visit ", "NaytaInfoboksiKayttajalle");
                var mapPoint = await MapUtils.Instance.GetPointFromMap();
                if (mapPoint != null)
                {

                    var vertice = Graph.Vertices.FirstOrDefault(o => Convert.ToInt32(o.X) == Convert.ToInt32(mapPoint.X) && Convert.ToInt32(o.Y) == Convert.ToInt32(mapPoint.Y));

                    return vertice;
                }
            }
            catch (TaskCanceledException)
            {
            }
            return null;
        }

        internal void AddTspVerticesFromGraphVertices(TSPVertices tspVertexList, List<GraphVertexClass> graphVertices, GraphVertexClass startingVertex)
        {
            foreach (var vertex in graphVertices)
            {
                tspVertexList.Add(new TSPVertice(Convert.ToInt32(vertex.X), Convert.ToInt32(vertex.Y), vertex.ID));
            }
            //Set starting vertex
            if (startingVertex != null)
            {
                var index = tspVertexList.FindIndex(o => o.Id == startingVertex.ID);
                var item = tspVertexList[index];
                tspVertexList[index] = tspVertexList[0];
                tspVertexList[0] = item;
            }

        }


        private int _tourCount;
        private double _oldFitness;
        private double _tourToShowCount;
        private void tsp_foundNewBestTour(object sender, TspEventArgs e)
        {

            _tourCount++;
            var fitnessToCompare = _oldFitness;
            var newFitness = e.BestTour.Fitness;
            if (_tourCount > _tourToShowCount)
            {
                _tourToShowCount = _tourToShowCount + 5;
                MapUtils.Instance.DrawRoutes(e);
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(() =>
                        this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage(string.Format("New best tour found: #{0} Fitness comparison: {1} < {2}", _tourCount, newFitness, fitnessToCompare), "NaytaInfoboksiKayttajalle")));

                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    new Action(() =>
                        this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage(e, "UpdateRoutesOnMap")));
            }
            _oldFitness = newFitness;
        }

        int _oldThousand;
        private void tsp_newCalcStarted(object sender, TspCalcEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Message))
            {
                if (e.OnGoing > _oldThousand)
                {
                    _oldThousand = _oldThousand + 10000;
                    this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage(string.Format("Finding tours with better fitness {0}/{1}", _oldThousand, e.Max), "UpdateStatusBar");
                }
            }
            else
            {
                this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage(e.Message, "UpdateStatusBar");
            }

        }

        public double GetDistance(TSPVertice starVertice, TSPVertice endVertice)
        {
            var graphStartVertice = Graph.Vertices.FirstOrDefault(o => o.ID == starVertice.Id);
            var graphEndVertice = Graph.Vertices.FirstOrDefault(o => o.ID == endVertice.Id);
            var edges = ShortestPathAlgorithm(graphStartVertice, graphEndVertice);
            var index = ShortestPathList.FindIndex(o => (o.VertexId1 == starVertice.Id && o.VertexId2 == endVertice.Id) || (o.VertexId1 == starVertice.Id && o.VertexId2 == endVertice.Id));
            ShortestPathList.RemoveAt(index);
            var distance = edges.Aggregate(0, (current, edge) => current + Convert.ToInt32(edge.Weight));
            ShortestPathList.Add(new ShortestPath { ShortestPathEdges = edges, VertexId1 = graphStartVertice.ID, VertexId2 = graphEndVertice.ID, Distance = distance });

            foreach (var edge in edges)
            {
                edge.IsVisited = true;
            }
            return distance;
        }

        public void ResetVisited()
        {
            foreach (var edge in Graph.Edges)
            {
                edge.IsVisited = false;
            }
        }

        public bool CheckIfKokoajaUratCoverTheKuvio(ref List<MapPoint> pointSet, int kokoajauraBufferValue)
        {
            pointSet = new List<MapPoint>(pointSet);
            Geometry unionGeometry = null;
            List<Geometry> allKokoajaGeometries = new List<Geometry>();
            foreach (var kokoajaura in KokoajauraList)
            {
                List<Geometry> geometryList = MapUtils.Instance.GetGeometriesWithIdsFromGraphicLayer(kokoajaura.Select(o => o.Id));
                allKokoajaGeometries.AddRange(geometryList);
            }
            if (allKokoajaGeometries.Any())
            {
                unionGeometry = GeometryEngine.Buffer(GeometryEngine.Union(allKokoajaGeometries), kokoajauraBufferValue);
                List<MapPoint> pointsNotCovered = new List<MapPoint>();

                foreach (var point in pointSet)
                {
                    if (!GeometryEngine.Intersects(point, GeometryEngine.Project(unionGeometry, point.SpatialReference)))
                    {
                        pointsNotCovered.Add(point);
                    }
                }
                pointSet = pointsNotCovered;
            }
            if (!pointSet.Any())
            {
                return false;
            }
            return true;

        }


    }


    public class ShortestPath
    {
        public int VertexId1 { get; set; }
        public int VertexId2 { get; set; }

        public double Distance { get; set; }

        public List<GraphEdgeClass> ShortestPathEdges { get; set; }
    }
}



