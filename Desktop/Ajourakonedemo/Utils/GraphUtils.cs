using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using ArcGISRuntime.Samples.DesktopViewer.Model;
using ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2;
using Catel.IoC;
using Catel.IO;
using Catel.Logging;
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
        protected readonly ILog Log = LogManager.GetCurrentClassLogger();

        public GraphUtils()
        {
            ShortestPathList = new List<ShortestPath>();
            KokoojauraList = new List<List<GraphEdgeClass>>();
        }

        public static GraphUtils Instance
        {
            get { return _instance; }
        }

        internal List<ShortestPath> ShortestPathList { get; set; }
        internal List<MapPoint> GraphVerticesAsMapPoint { get; set; }
        internal List<List<GraphEdgeClass>> KokoojauraList { get; set; }

        public int SlopeWeightMultiplier { get; set; }

        public int WetnessWeightMultiplier { get; set; }

        public int KelpoisuusWeightMultiplier { get; set; }

        private GraphClass _graph;

        public GraphClass Graph
        {
            get { return _graph; }
            set { _graph = value; }
        }

        public bool UseOnlyDistances { get; set; }
        public bool UseVisitedEdges { get; set; }
        public double VisitedMultiplier { get; set; }


        public List<GraphClassBidirectional> AddFeatureLayersToGraph()
        {
            List<GraphClassBidirectional> graphList = new List<GraphClassBidirectional>();
            IEnumerable<FeatureLayer> lineLayers = MapUtils.Instance.Map.Layers.OfType<FeatureLayer>().Where(o => o.FeatureTable.GeometryType == GeometryType.Polyline);
            foreach (FeatureLayer layer in lineLayers)
            {
                graphList.Add(AddFeaturesToGraph(MapUtils.Instance.GraphicsLayer.Graphics, layer.FeatureTable.ObjectIDField));
            }

            return graphList;

        }

        private GraphClassBidirectional AddFeaturesToGraph(IEnumerable<Graphic> features, string objectIdField)
        {
            Dictionary<int, MapPoint> mapPointDictionary = new Dictionary<int, MapPoint>();
            int counter = 1;
            GraphClass g = new GraphClass(false);

            foreach (Graphic feature in features)
            {
                if (feature.Geometry != null && feature.Geometry.GeometryType == GeometryType.Polyline)
                {
                    MapPoint startPoint = ((Polyline)feature.Geometry).Parts.FirstOrDefault().StartPoint;
                    MapPoint endPoint = ((Polyline)feature.Geometry).Parts.FirstOrDefault().EndPoint;

                    int startId = CheckIfPointExistsInDictionary(mapPointDictionary, startPoint, ref counter);
                    int endId = CheckIfPointExistsInDictionary(mapPointDictionary, endPoint, ref counter);

                    GraphVertexClass startVertex = g.Vertices.FirstOrDefault(o => o.ID == startId);
                    if (startVertex == null)
                    {


                        startVertex = new GraphVertexClass(startId, startPoint.X, startPoint.Y);
                    }

                    GraphVertexClass endVertex = g.Vertices.FirstOrDefault(o => o.ID == endId);
                    if (endVertex == null)
                    {
                        endVertex = new GraphVertexClass(endId, endPoint.X, endPoint.Y);
                    }
                    double sivukaltevuus = Convert.ToDouble(feature.Attributes[MapUtils.SivukaltString]);
                    int kosteus = Convert.ToInt32(feature.Attributes[MapUtils.KulkukelpoisuusString]);
                    double nousukaltevuus = Convert.ToDouble(feature.Attributes[MapUtils.NousukaltString]);

                    if (Math.Abs(sivukaltevuus) > MapUtils.Instance.MaxAllowedSlope)
                    {
                        continue;
                    }

                    if (Math.Abs(nousukaltevuus) > MapUtils.Instance.MaxAllowedForwardSlope)
                    {
                        continue;
                    }

                    GraphEdgeClass edge = new GraphEdgeClass(Convert.ToInt32(feature.Attributes[objectIdField]), sivukaltevuus, nousukaltevuus, kosteus, startVertex, endVertex);
                    g.AddVerticesAndEdge(edge);


                }
            }
            Graph = g;
            InitMapPointList(g);
            GraphClassBidirectional returnvalue = new GraphClassBidirectional();
            returnvalue.AddVertexRange(g.Vertices);
            returnvalue.AddEdgeRange(g.Edges);
            return returnvalue;
        }



        private void InitMapPointList(GraphClass graphClass)
        {
            GraphVerticesAsMapPoint = new List<MapPoint>();
            foreach (GraphVertexClass vertex in graphClass.Vertices)
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
            KeyValuePair<int, MapPoint> existingPoint = mapPointDictionary.FirstOrDefault(o => o.Value.X == startPoint.X && o.Value.Y == startPoint.Y);
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

            double weight = 17;



            weight = weight + (SlopeWeightMultiplier * Math.Abs(graphEdgeClass.Sivukaltevuus) + WetnessWeightMultiplier * graphEdgeClass.Kosteus);

            if (UseVisitedEdges && graphEdgeClass.IsVisited)
            {
                weight = weight * VisitedMultiplier;
            }


            if (graphEdgeClass.Sivukaltevuus > MapUtils.Instance.MaxAllowedSlope || graphEdgeClass.Nousukaltevuus > MapUtils.Instance.MaxAllowedForwardSlope)
            {
                weight = weight*30;
            }

            graphEdgeClass.Weight = weight;
            //Debug.WriteLine("Edge {0} Weight: {1}", graphEdgeClass.Id, graphEdgeClass.Weight);
            return weight;
        }



        public bool AddKokoajauraFromStartPointToEndPoint(MapPoint startingVertex, MapPoint endingVertex)
        {

            if (Graph == null)
            {
                MessageBox.Show("Graph is null");
                return false;
            }

            GraphVertexClass root = Graph.Vertices.FirstOrDefault(o => o.EqualsMapPointCoordinates(startingVertex));
            GraphVertexClass target = Graph.Vertices.FirstOrDefault(o => o.EqualsMapPointCoordinates(endingVertex));

            if (target == null || root == null)
            {
                return false;
            }

            List<GraphEdgeClass> edgeList = ShortestPathAlgorithm(root, target);
            if (!edgeList.Any() && (root.ID != target.ID))
            {
                //Graph.RemoveVertex(target);
                return false;
            }
            AddEdgeListToKokoajaUraList(edgeList);
            return true;


        }

        private void AddEdgeListToKokoajaUraList(List<GraphEdgeClass> edgeList)
        {
            KokoojauraList.Add(edgeList);
        }

        public List<GraphEdgeClass> ShortestPathAlgorithm(GraphVertexClass root, GraphVertexClass target)
        {
            Func<GraphEdgeClass, double> edgeWeights = EdgeWeights;

            // compute shortest paths
            TryFunc<GraphVertexClass, IEnumerable<GraphEdgeClass>> tryGetPaths = Graph.ShortestPathsDijkstra(edgeWeights, root);

            // query path for given vertices
            List<GraphEdgeClass> edgleList = new List<GraphEdgeClass>();

            IEnumerable<GraphEdgeClass> path;
            if (tryGetPaths(target, out path))
                edgleList.AddRange(path);
            return edgleList;
        }

        public TspEventArgs TspAlgorithm(CancellationToken token, int population, int maxgenerations, List<GraphVertexClass> graphVertexList, GraphVertexClass startingVertex = null)
        {

            // we are already running, so tell the tsp thread to halt.
            TSP2.Tsp tsp = new TSP2.Tsp();
            try
            {
                TSPVertices cityList = new TSPVertices();
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
                int populationSize = SetParameters(population, maxgenerations, out maxGenerations, out mutation, out groupSize, out seed, out numberOfCloseCities, out chanceUseCloseCity);
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
            List<GraphVertexClass> verticeList = Graph.Vertices.ToList();

            foreach (GraphVertexClass vertice in verticeList)
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
                MapPoint mapPoint = await MapUtils.Instance.GetPointFromMap();
                if (mapPoint != null)
                {
                    GraphVertexClass vertice = GetVerticeFromMapPoint(mapPoint);
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
            GraphVertexClass vertice = Graph.Vertices.FirstOrDefault(o => Convert.ToInt32(o.X) == Convert.ToInt32(mapPoint.X) && Convert.ToInt32(o.Y) == Convert.ToInt32(mapPoint.Y));
            return vertice;
        }

        internal void AddTspVerticesFromGraphVertices(TSPVertices tspVertexList, List<GraphVertexClass> graphVertices, GraphVertexClass startingVertex)
        {
            foreach (GraphVertexClass vertex in graphVertices)
            {
                tspVertexList.Add(new TSPVertice(Convert.ToInt32(vertex.X), Convert.ToInt32(vertex.Y), vertex.ID));
            }
            //Set starting vertex
            if (startingVertex != null)
            {
                int index = tspVertexList.FindIndex(o => o.Id == startingVertex.ID);
                TSPVertice item = tspVertexList[index];
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
            double fitnessToCompare = _oldFitness;
            double newFitness = e.BestTour.Fitness;
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
            GraphVertexClass graphStartVertice = Graph.Vertices.FirstOrDefault(o => o.ID == starVertice.Id);
            GraphVertexClass graphEndVertice = Graph.Vertices.FirstOrDefault(o => o.ID == endVertice.Id);
            List<GraphEdgeClass> edges = ShortestPathAlgorithm(graphStartVertice, graphEndVertice);
            int index = ShortestPathList.FindIndex(o => (o.VertexId1 == starVertice.Id && o.VertexId2 == endVertice.Id) || (o.VertexId1 == starVertice.Id && o.VertexId2 == endVertice.Id));
            ShortestPathList.RemoveAt(index);
            int distance = edges.Aggregate(0, (current, edge) => current + Convert.ToInt32(edge.Weight));
            ShortestPathList.Add(new ShortestPath { ShortestPathEdges = edges, VertexId1 = graphStartVertice.ID, VertexId2 = graphEndVertice.ID, Distance = distance });

            foreach (GraphEdgeClass edge in edges)
            {
                edge.IsVisited = true;
            }
            return distance;
        }

        public void ResetVisited()
        {
            foreach (GraphEdgeClass edge in Graph.Edges)
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
            foreach (List<GraphEdgeClass> kokoajaura in KokoojauraList)
            {
                List<Geometry> geometryList = MapUtils.Instance.GetGeometriesWithIdsFromGraphicLayer(kokoajaura.Select(o => o.Id));
                allKokoajaGeometries.AddRange(geometryList);
            }
            if (allKokoajaGeometries.Any())
            {
                unionGeometry = GeometryEngine.Buffer(GeometryEngine.Union(allKokoajaGeometries), kokoajauraBufferValue);
                List<MapPoint> pointsNotCovered = new List<MapPoint>();

                foreach (MapPoint point in pointSet)
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
            List<MapPoint> pointNotCovered = new List<MapPoint>(GraphVerticesAsMapPoint);
            List<List<GraphVertexClass>> result = new List<List<GraphVertexClass>>();
            foreach (List<GraphEdgeClass> kokoaja in KokoojauraList)
            {
                Polyline kokoajauraLineGeometry = GeometryEngine.Union(MapUtils.Instance.GetGeometriesWithIdsFromGraphicLayer(kokoaja.Select(o => o.Id))) as Polyline;
                Geometry kokoajaGeometry = GeometryEngine.Buffer(kokoajauraLineGeometry, kokoajauraBufferValue);
                List<MapPoint> kokoajauraPoints = new List<MapPoint>();
                List<MapPoint> tempPointList = new List<MapPoint>();
                foreach (MapPoint point in pointNotCovered)
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
                MapPoint startPoint = kokoajauraLineGeometry.Parts.FirstOrDefault().StartPoint;
                MapPoint endPoint = kokoajauraLineGeometry.Parts.FirstOrDefault().EndPoint;

                List<MapPoint> leftPoints = new List<MapPoint>();
                List<MapPoint> rightPoints = new List<MapPoint>();
                foreach (MapPoint point in kokoajauraPoints)
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
                List<MapPoint> points = new List<MapPoint>();
                List<MapPoint> points2 = new List<MapPoint>();
                if (kokoajauraPoints.Count > groupsize * 1.5)
                {
                    List<MapPoint> sorted = new List<MapPoint>();
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
            int index = points.FindIndex(o => GeometryEngine.Intersects(o, GeometryEngine.Project(kokoajauraLineGeometry, o.SpatialReference)));
            if (index != -1)
            {
                MapPoint item = points[index];
                points[index] = points[0];
                points[0] = item;
            }
            else
            {
                Log.Info("Index not found");
            }
        }

        private void AddPointsToResult(List<MapPoint> points, List<List<GraphVertexClass>> result)
        {
            List<GraphVertexClass> verticeList = new List<GraphVertexClass>();
            foreach (MapPoint point in points)
            {
                verticeList.Add(GetVerticeFromMapPoint(point));
            }
            result.Add(verticeList);
        }

        public void SetKokoajauratAsVisitedAndResetShortestPaths()
        {
            ShortestPathList = new List<ShortestPath>();
            foreach (GraphEdgeClass edge in Graph.Edges)
            {
                edge.IsVisited = false;
                if (KokoojauraList.Any(kokoaja => kokoaja.FirstOrDefault(o => o.Id == edge.Id) != null))
                {
                    edge.IsVisited = true;
                }
            }
        }

        public void MarkEdgesVisited(TspEventArgs tspEventArgs)
        {
            List<GraphEdgeClass> edges = MapUtils.Instance.GetGraphEdgeClassesFromEvents(tspEventArgs);
            foreach (GraphEdgeClass edge in edges)
            {
                GraphEdgeClass visited = Graph.Edges.FirstOrDefault(o => o.Id == edge.Id);
                if (visited != null)
                {
                    visited.IsVisited = true;
                }
            }
        }

        public void GetCountForEachEdgeInKokoajatUrat()
        {
            foreach (List<GraphEdgeClass> path in KokoojauraList)
            {
                foreach (GraphEdgeClass edge in path)
                {
                    GraphEdgeClass graphEdge = Graph.Edges.FirstOrDefault(o => o.Id == edge.Id);
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
                        Log.Error("Cannot find edge");
                    }
                }
            }

            //foreach (GraphEdgeClass edge in Graph.Edges)
            //{
            //    Debug.WriteLine(edge.VisitedCount);
            //}
        }

        public void CalculateShortestPaths(List<GraphVertexClass> vertices, GraphVertexClass root, bool calculateOnlyNeighbors)
        {
            ShortestPathList = new List<ShortestPath>();
            List<ShortestPath> shortestPathList = new List<ShortestPath>();
            vertices.Add(root);
            GraphVertexClass[] verticesArray = vertices.ToArray();
            Parallel.ForEach(vertices, vertice =>
            {
                List<ShortestPath> tempList = ShortestPaths(root, calculateOnlyNeighbors, vertice, verticesArray);
                shortestPathList.AddRange(tempList);
            });
            ShortestPathList = shortestPathList;
        }

        private List<ShortestPath> ShortestPaths(GraphVertexClass root, bool calculateOnlyNeighbors, GraphVertexClass vertice, GraphVertexClass[] verticesArray)
        {
            List<ShortestPath> tempList = new List<ShortestPath>();
            vertice.Neighbours = new int[verticesArray.Length];


            vertice.Distances = new long[verticesArray.Length];
            for (int index = 0; index < verticesArray.Length; ++index)
            {
                GraphVertexClass startVertice = vertice;
                GraphVertexClass endVertice = verticesArray[index];

                if (startVertice.ID == endVertice.ID)
                {
                    vertice.Neighbours[index] = endVertice.ID;
                    vertice.Distances[index] = 0;
                    continue;
                }


                bool isAdjacent = false;
                if (calculateOnlyNeighbors)
                {
                    foreach (GraphEdgeClass adjacentEdge in Graph.AdjacentEdges(startVertice))
                    {
                        if (adjacentEdge.IsAdjacent(endVertice))
                        {
                            isAdjacent = true;
                        }
                    }
                }
                if (!isAdjacent && calculateOnlyNeighbors && vertice.ID != root.ID && endVertice.ID != root.ID)
                {
                    vertice.Neighbours[index] = endVertice.ID;
                    vertice.Distances[index] = int.MaxValue;
                }
                else
                {
                    List<GraphEdgeClass> edges = ShortestPathAlgorithm(startVertice, endVertice);
                    if (edges.Any())
                    {
                        long distance = edges.Aggregate(0, (current, edge) => current + Convert.ToInt32(edge.Weight));
                        vertice.Neighbours[index] = endVertice.ID;
                        vertice.Distances[index] = distance;
                        ShortestPath path = new ShortestPath { ShortestPathEdges = edges, VertexId1 = startVertice.ID, VertexId2 = endVertice.ID, Distance = distance };
                        tempList.Add(path);
                    }
                    else
                    {
                        vertice.Neighbours[index] = endVertice.ID;
                        vertice.Distances[index] = int.MaxValue;
                    }
                }
            }
            return tempList;
        }


        public class ShortestPath
        {
            public int VertexId1 { get; set; }
            public int VertexId2 { get; set; }

            public double Distance { get; set; }

            public List<GraphEdgeClass> ShortestPathEdges { get; set; }

            public override string ToString()
            {
                return string.Format("From {0} to {1}. Cost: {2}", VertexId1, VertexId2, Distance);
            }
        }

        public async Task UpdatePuutToGraphAsync()
        {

            FeatureLayer nodesLayer = MapUtils.Instance.Map.Layers[Path.GetFileName(ConfigurationManager.AppSettings["Nodes"])] as FeatureLayer;


            if (nodesLayer == null)
                return;


            List<Feature> nodes = (await nodesLayer.FeatureTable.QueryAsync(new QueryFilter() { WhereClause = "1=1" })).ToList();
            foreach (GraphVertexClass vertice in Graph.Vertices)
            {
                Feature node = nodes.FirstOrDefault(o => GeometryEngine.Intersects(o.Geometry, new MapPoint((double)vertice.X, (double)vertice.Y, o.Geometry.SpatialReference)));
                if (node != null)
                {
                    vertice.Puumaara = Convert.ToInt32(Convert.ToDouble(node.Attributes["V"]) * 1000 * 0.2);
                }
                else
                {
                    Log.Error("Ei löydy nodea");
                }
            }



        }

        public void RemoveKokoojauraVertices(List<GraphVertexClass> vertices)
        {
            foreach (List<GraphEdgeClass> edgeList in KokoojauraList)
            {
                foreach (GraphEdgeClass edge in edgeList)
                {
                    vertices.Remove(edge.Source);
                    vertices.Remove(edge.Target);
                }
            }
        }
    }
}



