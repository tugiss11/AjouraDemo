using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Threading;
using ArcGISRuntime.Samples.DesktopViewer.Model;
using ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2;
using Catel.IoC;
using Catel.Logging;
using Catel.Messaging;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using QuickGraph;
using QuickGraph.Algorithms;
using Tsp;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

// ReSharper disable CompareOfFloatsByEqualityOperator


namespace ArcGISRuntime.Samples.DesktopViewer.Utils
{
    public class GraphUtils
    {
        private static readonly GraphUtils _instance = new GraphUtils();
        protected readonly ILog log = LogManager.GetCurrentClassLogger();

        public GraphUtils()
        {
            ShortestPathList = new List<ShortestPath>();
            KokoajauraList = new List<List<GraphEdgeClass>>();
        }

        public static GraphUtils Instance
        {
            get { return _instance; }
        }

        internal List<ShortestPath> ShortestPathList { get; set; }
        internal List<MapPoint> GraphVerticesAsMapPoint { get; set; }
        internal List<List<GraphEdgeClass>> KokoajauraList { get; set; }

        public int SlopeWeightMultiplier { get; set; }

        public int WetnessWeightMultiplier { get; set; }

        private GraphClass _graph;

        public GraphClass Graph
        {
            get { return _graph; }
            set { _graph = value; }
        }

        public bool UseOnlyDistances { get; set; }
        public bool UseVisitedEdges { get; set; }


        public List<GraphClassBidirectional> AddFeatureLayersToGraph()
        {
            var graphList = new List<GraphClassBidirectional>();
            var lineLayers = MapUtils.Instance.Map.Layers.OfType<FeatureLayer>().Where(o => o.FeatureTable.GeometryType == GeometryType.Polyline);
            foreach (var layer in lineLayers)
            {
                graphList.Add(AddFeaturesToGraph(MapUtils.Instance.GraphicsLayer.Graphics, layer.FeatureTable.ObjectIDField));
            }

            return graphList;

        }

        private GraphClassBidirectional AddFeaturesToGraph(IEnumerable<Graphic> features, string objectIdField)
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
                    var kosteus = Convert.ToInt32(feature.Attributes["kulkukelp"]);

                    var edge = new GraphEdgeClass(Convert.ToInt32(feature.Attributes[objectIdField]), kaltevuus, kosteus, startVertex, endVertex);
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

            weight = 17 + (SlopeWeightMultiplier * Math.Abs((double)graphEdgeClass.Sivukaltevuus) + (WetnessWeightMultiplier * graphEdgeClass.Kosteus));


            if (UseVisitedEdges && graphEdgeClass.IsVisited)
            {
                weight = weight * 0.25;
            }

            if (graphEdgeClass.IsVisited)
            {
                weight = weight * 0.25;
            }

            graphEdgeClass.Weight = weight;
            return weight;
        }



        public GraphVertexClass AddKokoajauraFromStartPointToEndPoint(MapPoint startingVertex, MapPoint endingVertex)
        {

            if (Graph == null)
            {
                MessageBox.Show("Graph is null");
                return null;
            }

            var root = Graph.Vertices.FirstOrDefault(o => Convert.ToInt32(o.X) == Convert.ToInt32(startingVertex.X) && Convert.ToInt32(o.Y) == Convert.ToInt32(startingVertex.Y));
            var target = Graph.Vertices.FirstOrDefault(o => Convert.ToInt32(o.X) == Convert.ToInt32(endingVertex.X) && Convert.ToInt32(o.Y) == Convert.ToInt32(endingVertex.Y));

            var edgeList = ShortestPathAlgorithm(root, target);
            AddEdgeListToKokoajaUraList(edgeList);
            return root;


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
                edgleList.AddRange(path);
            return edgleList;
        }

        public TspEventArgs TspAlgorithm(CancellationToken token, int population, int maxgenerations, List<GraphVertexClass> graphVertexList, GraphVertexClass startingVertex = null)
        {

            // we are already running, so tell the tsp thread to halt.
            var tsp = new TSP2.Tsp();
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


                tsp.FoundNewBestTour += tsp_foundNewBestTour;
                tsp.newCalcStarted += tsp_newCalcStarted;
                tsp.Begin(token, populationSize, maxGenerations, groupSize, mutation, seed, chanceUseCloseCity, cityList);
                tsp.FoundNewBestTour -= tsp_foundNewBestTour;
                tsp.newCalcStarted -= tsp_newCalcStarted;
                return tsp.Result;

            }
            finally
            {
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
                    var vertice = GetVerticeFromMapPoint(mapPoint);
                    return vertice;
                }
            }
            catch (TaskCanceledException)
            {
            }
            return null;
        }

        private GraphVertexClass GetVerticeFromMapPoint(MapPoint mapPoint)
        {
            var vertice = Graph.Vertices.FirstOrDefault(o => Convert.ToInt32(o.X) == Convert.ToInt32(mapPoint.X) && Convert.ToInt32(o.Y) == Convert.ToInt32(mapPoint.Y));
            return vertice;
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
                edge.VisitedCount = 0;
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


        public List<List<GraphVertexClass>> GetGraphVertexListsFromKokoajaurat(int kokoajauraBufferValue, int groupsize)
        {
            var pointNotCovered = new List<MapPoint>(GraphVerticesAsMapPoint);
            var result = new List<List<GraphVertexClass>>();
            foreach (var kokoaja in KokoajauraList)
            {
                var kokoajauraLineGeometry = GeometryEngine.Union(MapUtils.Instance.GetGeometriesWithIdsFromGraphicLayer(kokoaja.Select(o => o.Id))) as Polyline;
                var kokoajaGeometry = GeometryEngine.Buffer(kokoajauraLineGeometry, kokoajauraBufferValue);
                List<MapPoint> kokoajauraPoints = new List<MapPoint>();
                var tempPointList = new List<MapPoint>();
                foreach (var point in pointNotCovered)
                {
                    if (GeometryEngine.Intersects(point, GeometryEngine.Project(kokoajaGeometry, point.SpatialReference)))
                    {
                        kokoajauraPoints.Add(point);
                    }
                    else
                    {
                        tempPointList.Add(point);
                    }
                }
                bool isHorizontal = (kokoajaGeometry.Extent.XMax - kokoajaGeometry.Extent.XMin) > (kokoajaGeometry.Extent.YMax - kokoajaGeometry.Extent.YMin);
                var startPoint = kokoajauraLineGeometry.Parts.FirstOrDefault().StartPoint;
                var endPoint = kokoajauraLineGeometry.Parts.FirstOrDefault().EndPoint;

                var leftPoints = new List<MapPoint>();
                var rightPoints = new List<MapPoint>();
                foreach (var point in kokoajauraPoints)
                {

                    if (isLeft(startPoint, endPoint, point))
                    {
                        leftPoints.Add(point);
                    }
                    else
                    {
                        rightPoints.Add(point);
                    }

                }
                AddPointsToResult(groupsize, leftPoints, isHorizontal, kokoajauraLineGeometry, result);
                AddPointsToResult(groupsize, rightPoints, isHorizontal, kokoajauraLineGeometry, result);
                pointNotCovered = tempPointList;


            }
            return result;
        }

        private void AddPointsToResult(int groupsize, List<MapPoint> kokoajauraPoints, bool isHorizontal, Polyline kokoajauraLineGeometry, List<List<GraphVertexClass>> result)
        {
            while (kokoajauraPoints.Any())
            {
                var points = new List<MapPoint>();
                var points2 = new List<MapPoint>();
                if (kokoajauraPoints.Count > groupsize * 1.5)
                {
                    var sorted = new List<MapPoint>();
                    List<MapPoint> sortedSubgroups;
                    if (isHorizontal)
                    {
                        sorted.AddRange(kokoajauraPoints.OrderByDescending(o => o.X));
                        //sortedSubgroups = sorted.GetRange(0, groupsize*2).OrderByDescending(o => o.Y).ToList();
                    }
                    else
                    {
                        sorted.AddRange(kokoajauraPoints.OrderByDescending(o => o.Y));
                        //sortedSubgroups = sorted.GetRange(0, groupsize*2).OrderByDescending(o => o.X).ToList();
                    }
                    //points.AddRange(sortedSubgroups.GetRange(0, groupsize));
                    //points2.AddRange(sortedSubgroups.GetRange(groupsize, sortedSubgroups.Count - groupsize));
                    //kokoajauraPoints = sorted.GetRange(groupsize*2 - 1, sorted.Count - groupsize*2);

                    points.AddRange(sorted.GetRange(0, groupsize));
                    kokoajauraPoints = sorted.GetRange(groupsize - 1, sorted.Count - groupsize);
                }
                else
                {
                    points.AddRange(kokoajauraPoints);
                    kokoajauraPoints = new List<MapPoint>();
                }
                SetStartPointOnKokoajaUra(kokoajauraLineGeometry, points);
                AddPointsToResult(points, result);
                if (points2.Any())
                {
                    SetStartPointOnKokoajaUra(kokoajauraLineGeometry, points2);
                    AddPointsToResult(points2, result);
                }
            }
        }

        public bool isLeft(MapPoint a, MapPoint b, MapPoint c)
        {
            return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) > 0;
        }

        private void SetStartPointOnKokoajaUra(Geometry kokoajauraLineGeometry, List<MapPoint> points)
        {
            var index = points.FindIndex(o => GeometryEngine.Intersects(o, GeometryEngine.Project(kokoajauraLineGeometry, o.SpatialReference)));
            if (index != -1)
            {
                var item = points[index];
                points[index] = points[0];
                points[0] = item;
            }
            else
            {
                log.Info("Index not found");
            }
        }

        private void AddPointsToResult(List<MapPoint> points, List<List<GraphVertexClass>> result)
        {
            var verticeList = new List<GraphVertexClass>();
            foreach (var point in points)
            {
                verticeList.Add(GetVerticeFromMapPoint(point));
            }
            result.Add(verticeList);
        }

        public void SetKokoajauratAsVisitedAndResetShortestPaths()
        {
            ShortestPathList = new List<ShortestPath>();
            foreach (var edge in Graph.Edges)
            {
                edge.IsVisited = false;
                if (KokoajauraList.Any(kokoaja => kokoaja.FirstOrDefault(o => o.Id == edge.Id) != null))
                {
                    edge.IsVisited = true;
                }
            }
        }

        public void MarkEdgesVisited(TspEventArgs tspEventArgs)
        {
            var edges = MapUtils.Instance.GetGraphEdgeClassesFromEvents(tspEventArgs);
            foreach (var edge in edges)
            {
                var visited = Graph.Edges.FirstOrDefault(o => o.Id == edge.Id);
                if (visited != null)
                {
                    visited.IsVisited = true;
                }
            }
        }

        public void GetCountForEachEdgeInKokoajatUrat()
        {
            foreach (var path in KokoajauraList)
            {
                foreach (var edge in path)
                {
                    var graphEdge = Graph.Edges.FirstOrDefault(o => o.Id == edge.Id);
                    if (graphEdge != null)
                    {
                        graphEdge.VisitedCount = graphEdge.VisitedCount + 1;
                        if (graphEdge.VisitedCount >= 5)
                        {
                            graphEdge.IsVisited = true;
                        }
                    }
                    else
                    {
                        log.Error("Cannot find edge");
                    }
                }
            }

            foreach (var edge in Graph.Edges)
            {
                Debug.WriteLine(edge.VisitedCount);
            }
        }

        public void CalculateShortestPaths(List<GraphVertexClass> vertices, GraphVertexClass root)
        {
            ShortestPathList = new List<ShortestPath>();
            var shortestPathList = new List<ShortestPath>();
            vertices.Add(root);
            var verticesArray = vertices.ToArray();
            Parallel.ForEach(vertices, vertice =>
            {
                vertice.Neighbours = new int[verticesArray.Length];

                vertice.Distances = new long[verticesArray.Length];
                for (var index = 0; index < verticesArray.Length; ++index)
                {
                    var startVertice = vertice;
                    var endVertice = verticesArray[index];
                    var edges = ShortestPathAlgorithm(startVertice, endVertice);

                    if (edges.Any())
                    {
                        long distance = edges.Aggregate(0, (current, edge) => current + Convert.ToInt32(edge.Weight));
                        vertice.Neighbours[index] = endVertice.ID;
                        vertice.Distances[index] = distance;
                        shortestPathList.Add(new ShortestPath {ShortestPathEdges = edges, VertexId1 = startVertice.ID, VertexId2 = endVertice.ID, Distance = distance});
                    }
                    else
                    {
                        vertice.Neighbours[index] = 0;
                        vertice.Distances[index] = 0;
                    }

                }
            });
            ShortestPathList = shortestPathList;
        }


        public class ShortestPath
        {
            public int VertexId1 { get; set; }
            public int VertexId2 { get; set; }

            public double Distance { get; set; }

            public List<GraphEdgeClass> ShortestPathEdges { get; set; }
        }
    }
}



