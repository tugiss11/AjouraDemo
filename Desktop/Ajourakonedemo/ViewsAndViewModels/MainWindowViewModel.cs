using System.Collections.ObjectModel;
using System.Windows.Input;
using ArcGISRuntime.Samples.DesktopViewer.Model;
using Catel.Data;
using Catel.MVVM;
using Esri.ArcGISRuntime.Layers;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using ArcGISRuntime.Samples.DesktopViewer.Utils;
using ArcGISRuntime.Samples.DesktopViewer.Utils.GoogleTSP;
using ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2;
using Catel;
using Catel.Collections;
using Catel.IoC;
using Catel.Logging;
using Catel.Messaging;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Symbology;
using IronPython.Hosting;

using Microsoft.Scripting.Hosting;
using Geometry = Esri.ArcGISRuntime.Geometry.Geometry;


namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public Command<FeatureLayerMenuItem> OpenFeaturesCommand { get; private set; }
        public Command UpdateMenuCommand { get; private set; }

        public Command ShortestPathOnAllCommand { get; private set; }

        public Command TraceLineCommand { get; set; }

        public Command GetRoutesCommand { get; set; }


        public Command LoadLayersCommand { get; private set; }

        public Command ShortestPathCommand { get; private set; }

        public Command RunAllCommand { get; private set; }

        public Command UnionCommand { get; private set; }

        public Command PythonCommand { get; private set; }

        public Command LoadGraphCommand { get; private set; }

        public Command MinimumSpanningTreeCommand { get; private set; }
        public Command ClearHightlightCommand { get; private set; }
        public Command QueryMethod { get; private set; }

        public Command TspCommand { get; private set; }

        public Command SetTspNodesCommand { get; set; }

        public Command AddOptimizationModelCommand { get; set; }

        public Command ToggleGraphicsCommand { get; set; }

        public Command GeneralizeCommand { get; set; }

        public Command ClearGeneralizationCommand { get; set; }
        public Command SmoothenCommand { get; set; }
        public Command GoogleTspCommand { get; set; }
        public Command DrawResultCommand { get; set; }

        public Command<object> ResultsCheckedCommand { get; private set; }

        public static TSPVertices TspVertexList { get; set; }

        public List<GraphVertexClass> GraphVertexList
        {
            get { return GetValue<List<GraphVertexClass>>(GraphVertexListProperty); }
            set { SetValue(GraphVertexListProperty, value); }
        }

        public static readonly PropertyData GraphVertexListProperty = RegisterProperty("GraphVertexList", typeof(List<GraphVertexClass>));

        public FastObservableCollection<OptimizationRunModel> Optimizations
        {
            get { return GetValue<FastObservableCollection<OptimizationRunModel>>(OptimizationsProperty); }
            set { SetValue(OptimizationsProperty, value); }
        }

        public static readonly PropertyData OptimizationsProperty = RegisterProperty("Optimizations", typeof(FastObservableCollection<OptimizationRunModel>));

        public FastObservableCollection<RouteModel> Routes
        {
            get { return GetValue<FastObservableCollection<RouteModel>>(RoutesProperty); }
            set { SetValue(RoutesProperty, value); }
        }

        public static readonly PropertyData RoutesProperty = RegisterProperty("Routes", typeof(FastObservableCollection<RouteModel>));

        public RouteModel ValittuRoute
        {
            get { return GetValue<RouteModel>(ValittuRouteProperty); }
            set { SetValue(ValittuRouteProperty, value); }
        }

        public static readonly PropertyData ValittuRouteProperty = RegisterProperty("ValittuRoute", typeof(RouteModel));

        public KookoajauraTyyppi ValittuKokoojauratyyppi
        {
            get { return GetValue<KookoajauraTyyppi>(ValittuKokoojauratyyppiProperty); }
            set { SetValue(ValittuKokoojauratyyppiProperty, value); }
        }

        public static readonly PropertyData ValittuKokoojauratyyppiProperty = RegisterProperty("ValittuKokoojauratyyppi", typeof(KookoajauraTyyppi), KookoajauraTyyppi.UsingShortestPaths);

        public Graphic SelectedResult
        {
            get { return GetValue<Graphic>(SelectedResultProperty); }
            set
            {
                SetValue(SelectedResultProperty, value);
                if (value != null)
                {
                    foreach (var graphic in ResultGraphics)
                    {
                        graphic.IsVisible = false;
                    }
                    value.IsVisible = true;
                    if (Routes != null)
                    {
                        ValittuRoute = Routes.FirstOrDefault(o => o.Id == Convert.ToInt32(value.Attributes["ID"]));
                    }
                }
                else
                {
                    foreach (var graphic in ResultGraphics)
                    {
                        graphic.IsVisible = true;
                    }
                }
            }
        }

        public static readonly PropertyData SelectedResultProperty = RegisterProperty("SelectedResult", typeof(Graphic));




        public List<Tuple<int, int>> TourPaths
        {
            get { return GetValue<List<Tuple<int, int>>>(TourPathsProperty); }
            set { SetValue(TourPathsProperty, value); }
        }

        public static readonly PropertyData TourPathsProperty = RegisterProperty("TourPaths", typeof(List<Tuple<int, int>>));

        public TspEventArgs SelectedEvent
        {
            get { return GetValue<TspEventArgs>(SelectedEventProperty); }
            set
            {

                SetValue(SelectedEventProperty, value);
                SelectedEventPropertyChangedEventHandler(value);
            }
        }

        public static readonly PropertyData SelectedEventProperty = RegisterProperty("SelectedEvent", typeof(TspEventArgs), null);


        public List<TspEventArgs> EventList
        {
            get { return GetValue<List<TspEventArgs>>(EventListProperty); }
            set { SetValue(EventListProperty, value); }
        }

        public static readonly PropertyData EventListProperty = RegisterProperty("EventList", typeof(List<TspEventArgs>));

        public Tuple<int, int> SelectedPath
        {
            get { return GetValue<Tuple<int, int>>(SelectedPathProperty); }
            set { SetValue(SelectedPathProperty, value); }
        }

        public static readonly PropertyData SelectedPathProperty = RegisterProperty("SelectedPath", typeof(Tuple<int, int>), null, SelectedPathPropertyChangedEventHandler);

        public LayerCollection Layers
        {
            get { return GetValue<LayerCollection>(LayersProperty); }
            set { SetValue(LayersProperty, value); }
        }

        public static readonly PropertyData LayersProperty = RegisterProperty("Layers", typeof(LayerCollection));

        public string MainWindowStatusBarText
        {
            get { return GetValue<string>(MainWindowStatusBarTextProperty); }
            set { SetValue(MainWindowStatusBarTextProperty, value); }
        }

        public static readonly PropertyData MainWindowStatusBarTextProperty = RegisterProperty("MainWindowStatusBarText", typeof(string));

        public int InitialPopulation
        {
            get { return GetValue<int>(InitialPopulationProperty); }
            set { SetValue(InitialPopulationProperty, value); }
        }

        public static readonly PropertyData InitialPopulationProperty = RegisterProperty("InitialPopulation", typeof(int));

        public int VisitedCount
        {
            get { return GetValue<int>(VisitedCountProperty); }
            set { SetValue(VisitedCountProperty, value); }
        }

        public static readonly PropertyData VisitedCountProperty = RegisterProperty("VisitedCount", typeof(int));

        public int MaxAllowedSlope
        {
            get { return GetValue<int>(MaxAllowedSlopeProperty); }
            set
            {
                SetValue(MaxAllowedSlopeProperty, value);
                MapUtils.Instance.MaxAllowedSlope = value;
            }
        }

        public static readonly PropertyData MaxAllowedSlopeProperty = RegisterProperty("MaxAllowedSlope", typeof(int), 0);

        public double VisitedMultiplier
        {
            get { return GetValue<double>(VisitedMultiplierProperty); }
            set
            {
                SetValue(VisitedMultiplierProperty, value);
                GraphUtils.Instance.VisitedMultiplier = value;
            }
        }

        public static readonly PropertyData VisitedMultiplierProperty = RegisterProperty("VisitedMultiplier", typeof(double), 0.25);



        public int MaxAllowedForwardSlope
        {
            get { return GetValue<int>(MaxAllowedForwardSlopeProperty); }
            set
            {
                SetValue(MaxAllowedForwardSlopeProperty, value);
                MapUtils.Instance.MaxAllowedForwardSlope = value;
            }
        }

        public static readonly PropertyData MaxAllowedForwardSlopeProperty = RegisterProperty("MaxAllowedForwardSlope", typeof(int));




        public int VertexGroupSize
        {
            get { return GetValue<int>(VertexGroupSizeProperty); }
            set { SetValue(VertexGroupSizeProperty, value); }
        }

        public static readonly PropertyData VertexGroupSizeProperty = RegisterProperty("VertexGroupSize", typeof(int), 10000);


        public int KokoajauraBufferValue
        {
            get { return GetValue<int>(KokoajauraBufferValueProperty); }
            set { SetValue(KokoajauraBufferValueProperty, value); }
        }

        public static readonly PropertyData KokoajauraBufferValueProperty = RegisterProperty("KokoajauraBufferValue", typeof(int));

        public int TotalGenerations
        {
            get { return GetValue<int>(TotalGenerationsProperty); }
            set { SetValue(TotalGenerationsProperty, value); }
        }

        public static readonly PropertyData TotalGenerationsProperty = RegisterProperty("TotalGenerations", typeof(int));

        public double AjouraTotalLength
        {
            get { return GetValue<double>(TotalLengthProperty); }
            set { SetValue(TotalLengthProperty, value); }
        }

        public static readonly PropertyData TotalLengthProperty = RegisterProperty("AjouraTotalLength", typeof(double));

        public double KokooajaUraTotalLength
        {
            get { return GetValue<double>(KokooajaUraTotalLengthProperty); }
            set { SetValue(KokooajaUraTotalLengthProperty, value); }
        }

        public static readonly PropertyData KokooajaUraTotalLengthProperty = RegisterProperty("KokooajaUraTotalLength", typeof(double));

        public double AjouraTotalArea
        {
            get { return GetValue<double>(AjouraTotalAreaProperty); }
            set { SetValue(AjouraTotalAreaProperty, value); }
        }

        public static readonly PropertyData AjouraTotalAreaProperty = RegisterProperty("AjouraTotalArea", typeof(double));

        public double OldTotalLength
        {
            get { return GetValue<double>(OldTotalLengthProperty); }
            set { SetValue(OldTotalLengthProperty, value); }
        }

        public static readonly PropertyData OldTotalLengthProperty = RegisterProperty("OldTotalLength", typeof(double));

        public double KokoajauraTotalLength
        {
            get { return GetValue<double>(KokoajauraTotalLengthProperty); }
            set { SetValue(KokoajauraTotalLengthProperty, value); }
        }

        public static readonly PropertyData KokoajauraTotalLengthProperty = RegisterProperty("KokoajauraTotalLength", typeof(double));

        public double KokoajauraTotalArea
        {
            get { return GetValue<double>(KokoajauraTotalAreaProperty); }
            set { SetValue(KokoajauraTotalAreaProperty, value); }
        }

        public static readonly PropertyData KokoajauraTotalAreaProperty = RegisterProperty("KokoajauraTotalArea", typeof(double));

        public bool UseShortestPaths
        {
            get { return GetValue<bool>(UseShortestPathsProperty); }
            set { SetValue(UseShortestPathsProperty, value); }
        }

        public static readonly PropertyData UseShortestPathsProperty = RegisterProperty("UseShortestPaths", typeof(bool), true);


        public int Scale
        {
            get { return GetValue<int>(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        public static readonly PropertyData ScaleProperty = RegisterProperty("Scale", typeof(int), 100000, ScalePropertyChangedEventHandler);

        public int WetnessWeightMultiplier
        {
            get { return GetValue<int>(WetnessWeightMultiplierProperty); }
            set { SetValue(WetnessWeightMultiplierProperty, value); }
        }

        public static readonly PropertyData WetnessWeightMultiplierProperty = RegisterProperty("WetnessWeightMultiplier", typeof(int), 0, WetnessWeightMultiplierPropertyChangedEventHandler);

        public int SlopeWeightMultiplier
        {
            get { return GetValue<int>(SlopeWeightMultiplierProperty); }
            set { SetValue(SlopeWeightMultiplierProperty, value); }
        }

        public static readonly PropertyData SlopeWeightMultiplierProperty = RegisterProperty("SlopeWeightMultiplier", typeof(int), 0, SlopeWeightMultiplierPropertyChangedEventHandler);

        private static void SlopeWeightMultiplierPropertyChangedEventHandler(object sender, AdvancedPropertyChangedEventArgs e)
        {
            GraphUtils.Instance.SlopeWeightMultiplier = (int)e.NewValue;
        }

        private static void WetnessWeightMultiplierPropertyChangedEventHandler(object sender, AdvancedPropertyChangedEventArgs e)
        {
            GraphUtils.Instance.WetnessWeightMultiplier = (int)e.NewValue;
        }

        public bool UseVisitedEdges
        {
            get { return GetValue<bool>(UseVisitedEdgesProperty); }
            set
            {
                SetValue(UseVisitedEdgesProperty, value);
                GraphUtils.Instance.UseVisitedEdges = value;
            }
        }

        public static readonly PropertyData UseVisitedEdgesProperty = RegisterProperty("UseVisitedEdges", typeof(bool), true);

        public bool Debugging
        {
            get { return GetValue<bool>(DebuggingProperty); }
            set
            {
                SetValue(DebuggingProperty, value);
            }
        }

        public static readonly PropertyData DebuggingProperty = RegisterProperty("Debugging", typeof(bool), true);


        public bool CalculateOnlyNeighbors
        {
            get { return GetValue<bool>(CalculateOnlyNeighborsProperty); }
            set { SetValue(CalculateOnlyNeighborsProperty, value); }
        }

        public static readonly PropertyData CalculateOnlyNeighborsProperty = RegisterProperty("CalculateOnlyNeighbors", typeof(bool), true);


        public static bool IsLoaded { get; set; }

        public GraphVertexClass StartingVertex { get; set; }

        public MainWindowViewModel()
        {
            InitCommands();
            InitMessages();

            TourPaths = new List<Tuple<int, int>>();
            InitParameters();
        }

        private void InitParameters()
        {
            InitialPopulation = 10000;
            TotalGenerations = 210000;
            KokoajauraBufferValue = 46;
            MaxAllowedSlope = 20;
            MaxAllowedForwardSlope = 25;
            UseVisitedEdges = true;
            VisitedCount = 5;
            CalculateOnlyNeighbors = false;
            VisitedMultiplier = 0.25;
            Debugging = true;
        }

        private void InitMessages()
        {
            MessageMediator.Register<TspEventArgs>(this, UpdatePathsCombobox, "UpdateRoutesOnMap");
            MessageMediator.Register<string>(this, OnUpdateStatusBar, "UpdateStatusBar");
            MessageMediator.Register<FeatureLayerMenuItem>(this, OnAddMenuItem, "AddMenuItem");
            MessageMediator.Register<int>(this, OnScaleChanged, "ScaleChanged");
            MessageMediator.Register<bool>(this, OnIsLoaded, "OnLoaded");
        }

        private void OnIsLoaded(bool obj)
        {
            IsLoaded = obj;
        }

        private void OnScaleChanged(int obj)
        {
            Scale = obj;
        }

        private void InitCommands()
        {
            OpenFeaturesCommand = new Command<FeatureLayerMenuItem>(OnOpenFeaturesCommand);
            GoogleTspCommand = new Command(OnGoogleTspCommand);
            UpdateMenuCommand = new Command(OnUpdateMenuCommand);
            TspCommand = new Command(OnTspCommand, CanTspCommand);
            TspOnAllCommand = new Command(OnTspOnAllCommand, CanTspOnAllCommand);
            LoadLayersCommand = new Command(OnLoadLayersCommand, CanLoadGraphCommand);
            LoadGraphCommand = new Command(OnLoadGraphCommand, CanLoadGraphCommand);
            MinimumSpanningTreeCommand = new Command(OnMinimumSpanningTreeCommand, CanGraphCommand);
            ClearHightlightCommand = new Command(OnClearHightlightCommand, CanClearHighlightCommand);
            SetTspNodesCommand = new Command(OnSetTspNodesCommand, CanGraphCommand);
            MenuItems = new ObservableCollection<FeatureLayerMenuItem>();
            ToggleGraphicsCommand = new Command(OnToggleGraphicsCommand, CanGraphCommand);
            DrawResultCommand = new Command(OnDrawResultCommand, CanGraphCommand);
            GeneralizeCommand = new Command(OnGeneralizeCommand, CanGraphCommand);
            ClearGeneralizationCommand = new Command(OnClearGeneralizationCommand, HasTspGraphics);
            SmoothenCommand = new Command(OnSmoothenCommand, CanGraphCommand);
            PythonCommand = new Command(OnPythonCommand);
            ShortestPathOnAllCommand = new Command(OnShortestPathOnAllCommand);
            VehicleRoutingCommand = new Command(OnVehicleRoutingCommand);
            ResultsCheckedCommand = new Command<object>(OnResultsCheckedCommand);
            AddOptimizationModelCommand = new Command(OnAddOptimizationModelCommand);
            RunAllCommand = new Command(OnRunAllCommand);
            TraceLineCommand = new Command(OnTraceLineCommand);
            GetRoutesCommand = new Command(OnGetRoutesCommand);
            UnionCommand = new Command(OnUnionCommand);
            SimplifyResultCommand = new Command(OnSimplifyResultCommand);
            LoadAllCommand = new Command(OnLoadAllCommand);

        }

        private void OnLoadAllCommand()
        {
            MapUtils.Instance.ResultGraphics.Graphics.Clear();
            
            var optRuns = SqliteUtils.Instance.Query<OptimizationRunModel>("select * from OptimizationRunModel");
            foreach (var optRun in optRuns)
            {
                MapUtils.Instance.AddOptRunToResultGraphics(optRun);
            }
            ResultGraphics = MapUtils.Instance.ResultGraphics.Graphics;
        }

        public Command LoadAllCommand { get; set; }

        private void OnSimplifyResultCommand()
        {
            foreach (var graphic in MapUtils.Instance.ResultGraphics.Graphics)
            {
                graphic.Geometry = GeometryEngine.Generalize(graphic.Geometry, 10, false);
            }

        }

        public Command SimplifyResultCommand { get; set; }

        private void OnUnionCommand()
        {
            if (MapUtils.Instance.ResultGraphics.Graphics.Any())
            {
                var symbol = new SimpleLineSymbol
                {
                    Color = Colors.Red,
                    Style = SimpleLineStyle.Solid,
                    Width = 5
                };
                var geo = GeometryEngine.Union(MapUtils.Instance.ResultGraphics.Graphics.Where(g => g.Geometry != null).Select(o => o.Geometry));
                var newGraphic = new Graphic(geo, symbol);
                newGraphic.Geometry = ConnectLooseEnds(geo);

                MapUtils.Instance.ResultGraphics.Graphics.Clear();
                MapUtils.Instance.ResultGraphics.Graphics.Add(newGraphic);



            }
        }

        private Geometry ConnectLooseEnds(Geometry geo)
        {
            var polyline = geo as Polyline;
            PolylineBuilder polylineBuilder = null;
            if (polyline != null)
            {
                polylineBuilder = new PolylineBuilder(polyline.Parts);
                foreach (var part in polyline.Parts)
                {
                    bool disconnected = false;
                    var otherParts = new PolylineBuilder(polyline.Parts.Where(o => (o.StartPoint != part.StartPoint) && (o.EndPoint != part.EndPoint))).ToGeometry();
                    MapPoint disconnectPoint = null;
                    if (!GeometryEngine.Intersects(part.StartPoint, otherParts))
                    {
                        disconnected = true;
                        disconnectPoint = part.StartPoint;

                    }
                    else if (!GeometryEngine.Intersects(part.EndPoint, otherParts))
                    {
                        disconnected = true;
                        disconnectPoint = part.EndPoint;

                    }


                    if (disconnected)
                    {
                        var proximityResult = GeometryEngine.NearestVertex(otherParts, disconnectPoint);
                        polylineBuilder.AddPart(new List<MapPoint>() { proximityResult.Point, disconnectPoint });
                    }

                }
            }
            if (polylineBuilder != null) return polylineBuilder.ToGeometry();
            return geo;
        }

        private async void OnGetRoutesCommand()
        {
            try
            {
                await MapUtils.Instance.GetRoutesAsync();
                ResultGraphics = MapUtils.Instance.ResultGraphics.Graphics;
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        private async void OnTraceLineCommand()
        {
            try
            {
                await MapUtils.Instance.LineTracingAsync();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }

        }

        private async void OnRunAllCommand()
        {
            //var taskList = new List<Task<OptimizationRunModel>>();
            //foreach (var optRun in Optimizations)
            //{
            //    var task = Task.Run(() => StartOptRunAndCalculations(optRun));
            //    taskList.Add(task);
            //}

            //var resultList = await Task.WhenAll(taskList);
            //OptimizationRunModel smallest = null;
            //foreach (var result in resultList)
            //{
            //    if (smallest == null || result.UraTotalLength < smallest.UraTotalLength)
            //    {
            //        smallest = result;
            //    }
            //}
            Debugging = false;
            var startingVertex = await InitShortestPathCommand();
            while (WetnessWeightMultiplier < 30)
            {

                var methods = Enum.GetValues(typeof(KookoajauraTyyppi)).Cast<KookoajauraTyyppi>();
                ;
                foreach (var method in methods)
                {
                    var optRun = new OptimizationRunModel();
                    GraphUtils.Instance.KokoojauraList = new List<List<GraphEdgeClass>>();
                    GraphUtils.Instance.ResetVisited();
                    MapUtils.Instance.GraphicsLayer.ClearSelection();

                    await RunKokoojauraMethod(method, startingVertex);
                    var geo = DrawResult(false);
                    optRun.UraTotalLength = KokooajaUraTotalLength;
                    optRun.GeometryJson = geo.ToJson();
                    optRun.SlopeMultiplier = SlopeWeightMultiplier;
                    optRun.WetnessMultiplier = WetnessWeightMultiplier;
                    optRun.Method = (int) method;
                    SqliteUtils.Instance.Insert(optRun);
                }
                WetnessWeightMultiplier = WetnessWeightMultiplier + 1;
            }

        }


        private async Task<OptimizationRunModel> StartOptRunAndCalculations(OptimizationRunModel optRun)
        {
            optRun = await StartOptRun(optRun);
            return optRun;
        }

        private async void OnAddOptimizationModelCommand()
        {



            if (Optimizations == null)
            {
                Optimizations = new FastObservableCollection<OptimizationRunModel>();
            }
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => OnUpdateStatusBar("Adding new optimization run...")));

            var optRun = await InitOptRun();
            if (optRun.StartVertice > 0)
            {
                Optimizations.Add(optRun);
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => OnUpdateStatusBar("Added new optimization run")));
            }
            else
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => OnUpdateStatusBar("Failed adding new optimization run")));
            }


        }




        private void OnResultsCheckedCommand(object param)
        {
            var pointList = new List<Graphic>();
            foreach (var graphic in ResultGraphics)
            {
                var correspondingPoints = MapUtils.Instance.TspVerticesLayer.Graphics.Where(o => ((SimpleMarkerSymbol)o.Symbol).Color == ((SimpleLineSymbol)graphic.Symbol).Color);
                foreach (var point in correspondingPoints)
                {
                    point.IsVisible = graphic.IsVisible;
                    pointList.Add(point);
                }
            }

            GetDetailsFromPointList(pointList);

        }

        private void GetDetailsFromPointList(List<Graphic> pointList)
        {

            for (int index = 0; index < pointList.Count; ++index)
            {

            }
            //Maksimikuorma 95000 dm3
            //Matka
            //Ajotyhjänä
            //Ajoaika
            //Ajotäydellä

        }


        private async void OnVehicleRoutingCommand()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => OnUpdateStatusBar("New routing started!")));

            var optRun = await InitOptRun();

            await StartOptRun(optRun);

            if (optRun.OrderLists == null || !optRun.OrderLists.Any())
            {
                OnUpdateStatusBar("Vehicle routing failed!");
            }
            else
            {
                OnUpdateStatusBar("Drawing routes...");
                Routes = new FastObservableCollection<RouteModel>();
                var id = 1;
                foreach (var orderlist in optRun.OrderLists)
                {
                    if (orderlist.Any())
                    {
                        var color = MapUtils.Instance.GetRandomColor();
                        MapUtils.Instance.GraphicsLayer.ClearSelection();
                        var matkat = MapUtils.Instance.DrawRouteFromOrderList(orderlist.ToArray(), optRun.Vertices.ToArray(), optRun.UseShortestPaths, color);
                        var geo = MapUtils.Instance.ShowGeneralizedRoutes(MapUtils.Instance.GraphicsLayer.SelectedGraphics, false, color, true, id);
                        CalculateRouteDetails(orderlist, id, geo, optRun.Vertices.ToArray(), matkat);
                    }
                    id++;
                }

                ResultGraphics = MapUtils.Instance.ResultGraphics.Graphics;
                if (!MapUtils.Instance.Map.Layers.ContainsLayer(MapUtils.Instance.ResultGraphics))
                {
                    MapUtils.Instance.Map.Layers.Add(MapUtils.Instance.ResultGraphics);
                }
                if (!MapUtils.Instance.Map.Layers.ContainsLayer(MapUtils.Instance.TspVerticesLayer))
                {
                    MapUtils.Instance.Map.Layers.Add(MapUtils.Instance.TspVerticesLayer);
                }

                OnUpdateStatusBar("Vehicle routing success!");
                CalculateTotalLength();
                optRun.UraTotalLength = AjouraTotalLength;
                optRun.KokoajauraTotalLength = KokoajauraTotalLength;
                SqliteUtils.Instance.Insert(optRun);

                GraphUtils.Instance.ResetVisited();
            }
        }

        private void CalculateRouteDetails(List<long> orderlist, int id, Geometry geo, GraphVertexClass[] vertices, Tuple<double, double> matkat)
        {
            var puumaara = 0;
            var route = new RouteModel
            {
                Id = id,
                Geometry = geo,
                Pituus = Math.Round(Math.Abs(GeometryEngine.Length(geo)), 1)
            };
            route.MatkaTyhjana = matkat.Item1;
            route.MatkaTaytena = matkat.Item2;

            foreach (var order in orderlist)
            {
                var vertex = vertices[order];
                puumaara = vertex.Puumaara + puumaara;
            }
            route.Puumaaraa = puumaara;

            Routes.Add(route);
        }

        private async Task<OptimizationRunModel> StartOptRun(OptimizationRunModel optRun)
        {
            if (optRun.StartVertice > 0)
            {
                var vertices = optRun.Vertices;
                var root = optRun.Root;
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => OnUpdateStatusBar("Calculating distances")));
                if (UseShortestPaths)
                {
                    await Task.Run(() => GraphUtils.Instance.CalculateShortestPaths(vertices, root, CalculateOnlyNeighbors));
                }
                else
                {
                    vertices.Add(root);
                }

                OnUpdateStatusBar("Running routing optimizer");

                var optimzer = new CapacitatedVehicleRoutingProblemWithTimeWindows();
                optRun = optimzer.Start(optRun);
            }
            return optRun;
        }



        private async Task<OptimizationRunModel> InitOptRun()
        {
            List<GraphVertexClass> vertices = null;
            GraphVertexClass root = null;
            MapUtils.Instance.ResultGraphics.Graphics.Clear();
            var optRun = new OptimizationRunModel();
            optRun.UseVisitedEdges = UseVisitedEdges;
            optRun.Capacity = VertexGroupSize;
            optRun.UseLocalDistances = CalculateOnlyNeighbors;
            optRun.UseShortestPaths = UseShortestPaths;

            MessageMediator.SendMessage("Set storage location", "NaytaInfoboksiKayttajalle");
            var startingVertex = await MapUtils.Instance.GetPointFromMap();

            if (startingVertex != null)
            {
                if (UseVisitedEdges)
                {
                    if (GraphUtils.Instance.KokoojauraList == null || !GraphUtils.Instance.KokoojauraList.Any())
                    {
                        var edges = GetEdgesFromKokooajaUrat(startingVertex);
                        GraphUtils.Instance.KokoojauraList = new List<List<GraphEdgeClass>> { edges.ToList() };
                    }
                    GraphUtils.Instance.SetKokoajauratAsVisitedAndResetShortestPaths();
                }

                root = GraphUtils.Instance.Graph.Vertices.FirstOrDefault(o => Convert.ToInt32(o.X) == Convert.ToInt32(startingVertex.X) && Convert.ToInt32(o.Y) == Convert.ToInt32(startingVertex.Y));
                optRun.StartVertice = root.ID;
                optRun.Root = root;
                if (GraphVertexList == null || GraphVertexList.Count < 4)
                {
                    vertices = GraphUtils.Instance.Graph.Vertices.ToList();
                }
                else
                {
                    vertices = GraphVertexList;
                }
                GraphUtils.Instance.RemoveKokoojauraVertices(vertices);

                optRun.Vertices = vertices;
                optRun.SlopeMultiplier = SlopeWeightMultiplier;
                optRun.WetnessMultiplier = WetnessWeightMultiplier;

            }
            return optRun;
        }


        public GraphicCollection ResultGraphics
        {
            get { return GetValue<GraphicCollection>(ResultGraphicsProperty); }
            set { SetValue(ResultGraphicsProperty, value); }
        }
        public static readonly PropertyData ResultGraphicsProperty = RegisterProperty("ResultGraphics", typeof(GraphicCollection));


        public Command VehicleRoutingCommand { get; set; }

        private async void OnShortestPathOnAllCommand()
        {
            var methodType = ValittuKokoojauratyyppi;
            this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage(string.Format("Started calculating Kokoojaurat...({0})", ValittuKokoojauratyyppi), "UpdateStatusBar");
            await RunKokoojauraMethod(methodType);
            MapUtils.Instance.ResultGraphics.Graphics.Clear();
            DrawResult(false);
            this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage(string.Format("Done calculating Kokoojaurat...({0})", ValittuKokoojauratyyppi), "UpdateStatusBar");

        }

        private async Task RunKokoojauraMethod(KookoajauraTyyppi methodType, MapPoint startingVertex = null)
        {
            if (startingVertex == null)
            {
                startingVertex = await InitShortestPathCommand();
            }
            switch (methodType)
            {
                case KookoajauraTyyppi.UsingShortestPaths:
                    CalculateKokoojatUsingShortestPathsAsync(startingVertex);
                    break;
                case KookoajauraTyyppi.UsingBuffers:
                    await CalculateKokoojatUratUsingBuffersAsync(startingVertex);
                    break;
                case KookoajauraTyyppi.UsingVisitedEdges:
                    await CalculateKokoojatUratUsingVisitedEdges(startingVertex);
                    break;
                case KookoajauraTyyppi.UsingBuffersAndVisited:
                    await CalculateKokoojatUratUsingBuffersAndVisitedAsync(startingVertex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            CalculateKokoajaUraTotalLength();
        }

        private async Task CalculateKokoojatUratUsingBuffersAndVisitedAsync(MapPoint startingVertex)
        {
            List<MapPoint> pisteJoukko = new List<MapPoint>(GraphUtils.Instance.GraphVerticesAsMapPoint);
            var showDialog = true;
         
            if (startingVertex == null)
            {
                return;
            }
            while (GraphUtils.Instance.CheckIfKokoajaUratCoverTheKuvio(ref pisteJoukko, KokoajauraBufferValue))
            {
                var endingVertex = MapUtils.Instance.FindFarmostPointInsideListOfPoint(startingVertex, pisteJoukko);
                if (endingVertex != null)
                {
                    GraphUtils.Instance.AddKokoajauraFromStartPointToEndPoint(startingVertex, endingVertex);
                    GraphUtils.Instance.SetKokoajauratAsVisitedAndResetShortestPaths();
                    if (Debugging)
                    {
                        showDialog = await ShowDialog(showDialog);
                    }
                }
                else
                {
                    break;
                }
            }

            IEnumerable<GraphEdgeClass> edges = GetEdgesWithVisitedCount();
            UpdateKokoojatUrat(edges);

            MapUtils.Instance.HighlightEdges(edges);
            CalculateKokoajaUraTotalLength();
        }

        private async Task CalculateKokoojatUratUsingVisitedEdges(MapPoint startingVertex)
        {
            List<MapPoint> pisteJoukko = new List<MapPoint>(GraphUtils.Instance.GraphVerticesAsMapPoint);
            var showDialog = true;
  
            if (startingVertex == null)
            {
                return;
            }
            while (pisteJoukko.Any())
            {
                var endingVertex = MapUtils.Instance.FindFarmostPointInsideListOfPoint(startingVertex, pisteJoukko);
                pisteJoukko.Remove(endingVertex);
                GraphUtils.Instance.AddKokoajauraFromStartPointToEndPoint(startingVertex, endingVertex);
                GraphUtils.Instance.SetKokoajauratAsVisitedAndResetShortestPaths();
                if (Debugging)
                {
                    showDialog = await ShowDialog(showDialog);
                }

            }
            MapUtils.Instance.HighlightEdges(GraphUtils.Instance.KokoojauraList.SelectMany(o => o));
            IEnumerable<GraphEdgeClass> edges = GetEdgesWithVisitedCount();
            UpdateKokoojatUrat(edges);

            MapUtils.Instance.HighlightEdges(edges);
        }

        private void UpdateKokoojatUrat(IEnumerable<GraphEdgeClass> edges)
        {
            GraphUtils.Instance.KokoojauraList = new List<List<GraphEdgeClass>>() { edges.ToList() };
        }

        private static async Task<bool> ShowDialog(bool showDialog)
        {
            bool retValue = false;
            if (showDialog)
            {
                MapUtils.Instance.HighlightEdges(GraphUtils.Instance.KokoojauraList.SelectMany(o => o));
                var vm = new NotificationViewModel();
                vm.ShowModalessDialog(vm, typeof(NotificationView));
                while (vm.NotifyViewModelResult == NotificationViewModel.NotifyResult.Wait)
                {
                    await Task.Delay(200);
                }
                if (vm.NotifyViewModelResult == NotificationViewModel.NotifyResult.NextLine || vm.NotifyViewModelResult == NotificationViewModel.NotifyResult.NextNode)
                {
                    retValue = true;
                }
            }
            return retValue;
        }

        private void CalculateKokoojatUsingShortestPathsAsync(MapPoint startingVertex)
        {
          

            var edges = GetEdgesFromKokooajaUrat(startingVertex);

            MapUtils.Instance.HighlightEdges(edges);


        }

        private IEnumerable<GraphEdgeClass> GetEdgesFromKokooajaUrat(MapPoint startingVertex)
        {
            List<MapPoint> pisteJoukko = new List<MapPoint>(GraphUtils.Instance.GraphVerticesAsMapPoint);

            if (startingVertex == null)
            {
                return new List<GraphEdgeClass>();
            }

            foreach (var piste in pisteJoukko)
            {
                GraphUtils.Instance.AddKokoajauraFromStartPointToEndPoint(piste, startingVertex);
            }
            IEnumerable<GraphEdgeClass> edges = GetEdgesWithVisitedCount();
            UpdateKokoojatUrat(edges);
            return edges;
        }

        private IList<GraphEdgeClass> GetEdgesWithVisitedCount()
        {
            var visitedCountLimit = VisitedCount;
            GraphUtils.Instance.GetCountForEachEdgeInKokoajatUrat();


            var edges = GraphUtils.Instance.Graph.Edges.Where(c => c.VisitedCount >= visitedCountLimit);


            return edges.ToList();
        }

        private void OnPythonCommand()
        {

            string fullPath = @"C:\sources\IronPytho\helloworld.py";
            var engine = Python.CreateEngine();
            ScriptScope scope = engine.CreateScope();

            string importScript = "import sys" + Environment.NewLine +
                      "sys.path.append( r\"{0}\" )" + Environment.NewLine +
                      "from {1} import *";

            // import the module
            string scriptStr = string.Format(importScript,
                                             Path.GetDirectoryName(fullPath),
                                             Path.GetFileNameWithoutExtension(fullPath));
            var importSrc = engine.CreateScriptSourceFromString(scriptStr, Microsoft.Scripting.SourceCodeKind.File);
            importSrc.Execute(scope);

            string expr = "getDateTimeString()";
            var result = engine.Execute(expr, scope);


        }

        private bool CanTspOnAllCommand()
        {
            if (GraphUtils.Instance.KokoojauraList != null && GraphUtils.Instance.KokoojauraList.Any())
            {
                return true;
            }
            return false;
        }


        private void OnSmoothenCommand()
        {
            if (!MapUtils.Instance.GraphicsLayer.SelectedGraphics.Any())
            {
                this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage("Nothing to draw", "UpdateStatusBar");
                return;
            }
            MapUtils.Instance.ShowSmoothened(MapUtils.Instance.ResultGraphics.Graphics.ToList(), true);
            this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage("Smoothen done", "UpdateStatusBar");
        }

        private void OnGoogleTspCommand()
        {
            GoogleTsp.Run(new string[0]);
        }

        private void SelectedEventPropertyChangedEventHandler(TspEventArgs e)
        {
            if (e != null)
            {
                ShowSmoothenResultsFromTspEventArgs(e);
            }
        }

        private void ShowSmoothenResultsFromTspEventArgs(TspEventArgs e)
        {
            var graphEdges = MapUtils.Instance.GetGraphEdgeClassesFromEvents(e);
            var graphics = MapUtils.Instance.GetGraphicsFromGraphEdges(graphEdges);
            MapUtils.Instance.ShowSmoothened(graphics);
            UpdatePathsCombobox(e);
        }


        private void UpdatePathsCombobox(TspEventArgs tspEventArgs)
        {
            TspVertexList = tspEventArgs.TSPVertexList;
            TourPaths = MapUtils.Instance.GetTourOnOrder(tspEventArgs.BestTour);
        }

        private static async void ScalePropertyChangedEventHandler(object sender, AdvancedPropertyChangedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            await MapUtils.Instance.MapView.ZoomToScaleAsync(Convert.ToDouble(e.NewValue));
        }

        private bool HasTspGraphics()
        {
            if (!IsLoaded)
            {
                return false;
            }

            if (MapUtils.Instance.ResultGraphics != null && MapUtils.Instance.ResultGraphics.Graphics.Any())
            {
                return true;
            }
            return false;
        }

        private static void SelectedPathPropertyChangedEventHandler(object sender, AdvancedPropertyChangedEventArgs advancedPropertyChangedEventArgs)
        {
            var value = advancedPropertyChangedEventArgs.NewValue as Tuple<int, int>;
            MapUtils.Instance.DrawEdgeBetweenIdPair(value, TspVertexList);
        }

        private void OnClearGeneralizationCommand()
        {
            MapUtils.Instance.ResultGraphics.Graphics.Clear();
        }

        private void OnDrawResultCommand()
        {
            DrawResult(false);
        }

        private void OnGeneralizeCommand()
        {
            DrawResult(true);
        }

        private Geometry DrawResult(bool generalize)
        {
            if (!MapUtils.Instance.GraphicsLayer.SelectedGraphics.Any())
            {
                this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage("Nothing to draw", "UpdateStatusBar");
                return null;
            }
            var geo = MapUtils.Instance.ShowGeneralizedRoutes(MapUtils.Instance.GraphicsLayer.SelectedGraphics, generalize, Colors.Red);
            this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage("Drawing done", "UpdateStatusBar");
            return geo;
        }

        private void OnToggleGraphicsCommand()
        {
            MapUtils.Instance.GraphicsLayer.IsVisible = !MapUtils.Instance.GraphicsLayer.IsVisible;
        }

        private CancellationTokenSource _cancellation;
        private async void OnTspOnAllCommand()
        {
            await RunTspAsync(false);
        }

        public Command TspOnAllCommand { get; set; }

        private bool CanTspCommand()
        {
            if (CanGraphCommand() && GraphVertexList != null && GraphVertexList.Count > 5)
            {
                return true;
            }
            return false;
        }


        private async void OnSetTspNodesCommand()
        {
            GraphVertexList = new List<GraphVertexClass>();
            MapUtils.Instance.RemoveVerticesFromMap();
            while (!_tspIsRunning)
            {
                var vertex = await GraphUtils.Instance.AskUserForVertex();
                if (vertex != null)
                {
                    MapUtils.Instance.AddVertexToGraphicsLayer(vertex);
                    GraphVertexList.Add(vertex);
                }
                else
                {
                    break;
                }
            }
        }


        private async void OnTspCommand()
        {
            await RunTspAsync(true);
        }

        private bool _tspIsRunning;
        private async Task RunTspAsync(bool useList)
        {
            if (_cancellation != null && _cancellation.IsCancellationRequested)
            {
                this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage("Task Cancellation already requested", "UpdateStatusBar");
                return;
            }
            if (_cancellation != null && _tspIsRunning)
            {
                _cancellation.Cancel();
                return;
            }
            try
            {
                GraphUtils.Instance.SetKokoajauratAsVisitedAndResetShortestPaths();

                _cancellation = new CancellationTokenSource();
                var token = _cancellation.Token;
                _tspIsRunning = true;
                if (!useList)
                {
                    List<List<GraphVertexClass>> graphLists = GraphUtils.Instance.GetGraphVertexListsFromKokoajaurat(KokoajauraBufferValue, VertexGroupSize);
                    //var taskList = new List<Task<TspEventArgs>>();
                    var resultList = new List<TspEventArgs>();
                    foreach (var graphlist in graphLists)
                    {
                        if (graphlist != null && graphlist.Any())
                        {
                            if (StartingVertex == null)
                            {
                                var task = await Task.Run(() => GraphUtils.Instance.TspAlgorithm(token, InitialPopulation, TotalGenerations, graphlist), token);
                                resultList.Add(task);
                                GraphUtils.Instance.MarkEdgesVisited(task);
                                //taskList.Add(task);
                            }
                            else
                            {
                                graphlist.Add(StartingVertex);
                                var task = await Task.Run(() => GraphUtils.Instance.TspAlgorithm(token, InitialPopulation, TotalGenerations, graphlist, StartingVertex), token);
                                resultList.Add(task);
                                GraphUtils.Instance.MarkEdgesVisited(task);
                                //taskList.Add(task);
                            }
                        }
                    }

                    //await Task.WhenAll(taskList);
                    foreach (var evetArgs in resultList)
                    {
                        if (evetArgs != null)
                        {
                            ShowSmoothenResultsFromTspEventArgs(evetArgs);
                        }
                    }
                    EventList = resultList;
                    //CalculateTotalLength(resultList);
                }
                else
                {
                    await Task.Run(() => GraphUtils.Instance.TspAlgorithm(token, InitialPopulation, TotalGenerations, GraphVertexList), token);
                }
                _tspIsRunning = false;
                this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage("Algorithm done!", "UpdateStatusBar");
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException || ex is OperationCanceledException)
                {
                    this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage("Task Cancelled", "UpdateStatusBar");
                }
                else if (ex.GetAllInnerExceptions().OfType<OperationCanceledException>().Any())
                {
                    this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage("Task Cancelled", "UpdateStatusBar");
                }
                else throw;
            }
            finally
            {
                _tspIsRunning = false;
                _cancellation.Dispose();
                _cancellation = null;
            }
        }

        private void CalculateTotalLength()
        {
            try
            {
                OldTotalLength = AjouraTotalLength;
                AjouraTotalLength = Math.Round(CalculationsUtil.Instance.CalculateTotalLength(ResultGraphics));
                AjouraTotalArea = Math.Round(AjouraTotalLength * 4); //TODO Parametrisoi
                CalculateKokoajaUraTotalLength();
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }


        private void OnUpdateStatusBar(string message)
        {
            MainWindowStatusBarText = message;
        }

        private bool CanClearHighlightCommand()
        {
            if (!IsLoaded)
            {
                return false;
            }


            if (MapUtils.Instance.GraphicsLayer == null)
            {
                return false;
            }
            if (MapUtils.Instance.GraphicsLayer.SelectedGraphics.Any() || MapUtils.Instance.SavedHightlights.Any())
            {
                return true;
            }

            return false;
        }



        private void OnClearHightlightCommand()
        {

            if (MapUtils.Instance.GraphicsLayer != null && MapUtils.Instance.GraphicsLayer.SelectedGraphics.Any())
            {
                MapUtils.Instance.SavedHightlights = MapUtils.Instance.GraphicsLayer.SelectedGraphics.ToList();
                MapUtils.Instance.GraphicsLayer.ClearSelection();
            }
            else if (MapUtils.Instance.SavedHightlights != null && MapUtils.Instance.SavedHightlights.Any())
            {
                MapUtils.Instance.HighlightEdges(MapUtils.Instance.SavedHightlights);
            }

        }

        private bool CanGraphCommand()
        {
            if (GraphUtils.Instance.Graph == null)
            {
                return false;
            }
            return true;
        }

        private async Task CalculateKokoojatUratUsingBuffersAsync(MapPoint startingVertex)
        {
            List<MapPoint> pisteJoukko = GraphUtils.Instance.GraphVerticesAsMapPoint;

          
            if (startingVertex == null)
            {
                return;
            }
            bool showDialog = true;
            while (GraphUtils.Instance.CheckIfKokoajaUratCoverTheKuvio(ref pisteJoukko, KokoajauraBufferValue))
            {
                var endingVertex = MapUtils.Instance.FindFarmostPointInsideListOfPoint(startingVertex, pisteJoukko);
             
                if (endingVertex != null)
                {
                    var success = GraphUtils.Instance.AddKokoajauraFromStartPointToEndPoint(startingVertex, endingVertex);
                    if (!success)
                    {
                        pisteJoukko.Remove(endingVertex);
                    }
                    if (Debugging)
                    {
                        MapUtils.Instance.HighlightEdges(GraphUtils.Instance.KokoojauraList.SelectMany(o => o), false);
                        showDialog = await ShowDialog(showDialog);
                    }
                }
                else
                {
                    break;
                }
            }

            MapUtils.Instance.HighlightEdges(GraphUtils.Instance.KokoojauraList.SelectMany(o => o));
            CalculateKokoajaUraTotalLength();
        }

        private async Task<MapPoint> InitShortestPathCommand()
        {
            MessageMediator.SendMessage("Set storage location", "NaytaInfoboksiKayttajalle");
            GraphUtils.Instance.KokoojauraList = new List<List<GraphEdgeClass>>();
            GraphUtils.Instance.ResetVisited();
            MapUtils.Instance.GraphicsLayer.ClearSelection();
            var startingVertex = await MapUtils.Instance.GetPointFromMap();

            return startingVertex;
        }

        private void CalculateKokoajaUraTotalLength()
        {
            KokoajauraTotalLength = Math.Round(CalculationsUtil.Instance.CalculateKokoajaUraTotalLength(GraphUtils.Instance.KokoojauraList));
            KokoajauraTotalArea = Math.Round(KokoajauraTotalLength * 5, 2); //TODO Parametrisoi
        }


        private void OnMinimumSpanningTreeCommand()
        {
            GraphUtils.Instance.GetMinimumSpanningTree();
        }

        private bool CanLoadGraphCommand()
        {
            //if (MapUtils.Instance.Map != null && MapUtils.Instance.Map.Layers.OfType<FeatureLayer>().Any())
            //{
            return true;
            //}
            //return false;
        }

        private void OnLoadGraphCommand()
        {

            var result = GraphUtils.Instance.AddFeatureLayersToGraph();
            foreach (var graph in result)
            {
                ShowGraph(graph);
            }
        }

        private void ShowGraph(GraphClassBidirectional graph)
        {
            var viewmodel = new GraphViewModel(graph);
            ShowModalDialog(viewmodel, typeof(GraphView));
        }

        internal async void OnLoadLayersCommand()
        {

            var viewModelManager = (IViewModelManager)ServiceLocator.Default.ResolveType(typeof(IViewModelManager));
            var viewModel = (KarttaViewModel)viewModelManager.ActiveViewModels.FirstOrDefault(vm => vm is KarttaViewModel);
            if (viewModel != null) await viewModel.InitializeMapAsync();

            await MapUtils.Instance.AddFeatureLayersAsGraphic();
            GraphUtils.Instance.AddFeatureLayersToGraph();
            Layers = MapUtils.MapViewService.Map.Layers;
            if (ConfigurationManager.AppSettings["hasPuudata"] == "true")
            {
                await GraphUtils.Instance.UpdatePuutToGraphAsync();
            }

        }


        private void ShowNotificationExecute()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(
                () =>
                {
                    var notify = new NotificationView();
                    notify.Show();
                }));
        }

        private void OnUpdateMenuCommand()
        {
            var map = MapUtils.Instance.Map;
            foreach (FeatureLayer layer in map.Layers.OfType<FeatureLayer>())
            {
                MenuItems.Add(new FeatureLayerMenuItem { Header = layer.DisplayName, Layer = layer });
            }
        }

        private void OnAddMenuItem(FeatureLayerMenuItem menuItem)
        {
            MenuItems.Add(menuItem);
        }

        private async void OnOpenFeaturesCommand(FeatureLayerMenuItem layer)
        {
            var viewModel = new AluerajausAlueValintaViewModel();
            Mouse.OverrideCursor = Cursors.Wait;
            await viewModel.InitASync(layer.Layer);
            Mouse.OverrideCursor = null;
            ShowModalessDialog(viewModel, typeof(AluerajausAlueValintaView));
        }



        public ObservableCollection<FeatureLayerMenuItem> MenuItems
        {
            get { return GetValue<ObservableCollection<FeatureLayerMenuItem>>(MenuItemsProperty); }
            set { SetValue(MenuItemsProperty, value); }
        }



        public static readonly PropertyData MenuItemsProperty = RegisterProperty("MenuItems", typeof(ObservableCollection<FeatureLayerMenuItem>), null);

    }
}
