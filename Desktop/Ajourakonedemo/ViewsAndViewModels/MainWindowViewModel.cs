using System.Collections.ObjectModel;
using System.Windows.Input;
using ArcGISRuntime.Samples.DesktopViewer.Model;
using Catel.Data;
using Catel.MVVM;
using Esri.ArcGISRuntime.Layers;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ArcGISRuntime.Samples.DesktopViewer.Utils;
using ArcGISRuntime.Samples.DesktopViewer.Utils.GoogleTSP;
using ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2;
using Catel;
using Catel.IoC;
using Catel.Logging;
using Catel.Messaging;
using Esri.ArcGISRuntime.Geometry;
using IronPython.Hosting;

using Microsoft.Scripting.Hosting;


namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public Command<FeatureLayerMenuItem> OpenFeaturesCommand { get; private set; }
        public Command UpdateMenuCommand { get; private set; }

        public Command ShortestPathOnAllCommand { get; private set; }

        public Command LoadLayersCommand { get; private set; }

        public Command ShortestPathCommand { get; private set; }

        public Command PythonCommand { get; private set; }

        public Command LoadGraphCommand { get; private set; }

        public Command MinimumSpanningTreeCommand { get; private set; }

        public Command ClearHightlightCommand { get; private set; }

        public Command QueryMethod { get; private set; }

        public Command TspCommand { get; private set; }

        public Command SetTspNodesCommand { get; set; }

        public Command ToggleGraphicsCommand { get; set; }

        public Command GeneralizeCommand { get; set; }

        public Command ClearGeneralizationCommand { get; set; }
        public Command SmoothenCommand { get; set; }
        public Command GoogleTspCommand { get; set; }
        public Command DrawResultCommand { get; set; }

        public static TSPVertices TspVertexList { get; set; }
        public List<GraphVertexClass> GraphVertexList
        {
            get { return GetValue<List<GraphVertexClass>>(GraphVertexListProperty); }
            set { SetValue(GraphVertexListProperty, value); }
        }
        public static readonly PropertyData GraphVertexListProperty = RegisterProperty("GraphVertexList", typeof(List<GraphVertexClass>));

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

        public int VertexGroupSize
        {
            get { return GetValue<int>(VertexGroupSizeProperty); }
            set { SetValue(VertexGroupSizeProperty, value); }
        }

        public static readonly PropertyData VertexGroupSizeProperty = RegisterProperty("VertexGroupSize", typeof(int), 10);


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
            set { SetValue(UseVisitedEdgesProperty, value); }
        }
        public static readonly PropertyData UseVisitedEdgesProperty = RegisterProperty("UseVisitedEdges", typeof(bool), true, UseVisitedEdgesPropertyChangedEventHandler);

        private static void UseVisitedEdgesPropertyChangedEventHandler(object sender, AdvancedPropertyChangedEventArgs e)
        {
            GraphUtils.Instance.UseVisitedEdges = (bool)e.NewValue;
        }

        public static bool IsLoaded { get; set; }

        public GraphVertexClass StartingVertex { get; set; }

        public MainWindowViewModel()
        {
            InitCommands();
            InitMessages();

            TourPaths = new List<Tuple<int, int>>();
            InitialPopulation = 10000;
            TotalGenerations = 210000;
            KokoajauraBufferValue = 175;
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
            ShortestPathCommand = new Command(OnShortestPathCommand, CanGraphCommand);
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
        }

       
        private async void OnVehicleRoutingCommand()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => OnUpdateStatusBar("New routing started!")));
            var success = false;
            MessageMediator.SendMessage("Set storage location", "NaytaInfoboksiKayttajalle");
            var startingVertex = await MapUtils.Instance.GetPointFromMap();
            if (startingVertex != null)
            {

                if (UseVisitedEdges)
                {
                    GetEdgesFromKokooajaUrat(startingVertex);
                }


                var root = GraphUtils.Instance.Graph.Vertices.FirstOrDefault(o => Convert.ToInt32(o.X) == Convert.ToInt32(startingVertex.X) && Convert.ToInt32(o.Y) == Convert.ToInt32(startingVertex.Y));
                List<GraphVertexClass> vertices = null;
                if (GraphVertexList == null || GraphVertexList.Count < 4)
                {
                    vertices = GraphUtils.Instance.Graph.Vertices.ToList();
                }
                else
                {
                    vertices = GraphVertexList;
                }


                Application.Current.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => OnUpdateStatusBar("Calculating distances"))); 
                if (UseShortestPaths)
                {
                    GraphUtils.Instance.CalculateShortestPaths(vertices, root);
                }
                else
                {
                    vertices.Add(root);
                }
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => OnUpdateStatusBar("Running routing optimizer")));

                success = CapacitatedVehicleRoutingProblemWithTimeWindows.Start(vertices, VertexGroupSize, UseShortestPaths);
            }
            if (!success)
            {
                OnUpdateStatusBar("Vehicle routing failed!");
            }
            else
            {
                ResultGraphics = MapUtils.Instance.TspGraphicsLayer.Graphics;
                OnUpdateStatusBar("Vehicle routing success!");
                GraphUtils.Instance.ResetVisited();
            }
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
            var startingVertex = await InitShortestPathCommand();

            var edges = GetEdgesFromKokooajaUrat(startingVertex);

            MapUtils.Instance.HighlightEdges(edges);

            GraphUtils.Instance.ResetVisited();





        }

        private static IEnumerable<GraphEdgeClass> GetEdgesFromKokooajaUrat(MapPoint startingVertex)
        {
            List<MapPoint> pisteJoukko = GraphUtils.Instance.GraphVerticesAsMapPoint;


            if (startingVertex == null)
            {
                return new List<GraphEdgeClass>();
            }

            foreach (var piste in pisteJoukko)
            {
                GraphUtils.Instance.AddKokoajauraFromStartPointToEndPoint(piste, startingVertex);
            }
            var visitedCountLimit = 5;
            GraphUtils.Instance.GetCountForEachEdgeInKokoajatUrat();


            var edges = GraphUtils.Instance.Graph.Edges.Where(c => c.VisitedCount > visitedCountLimit);
            return edges;
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
            if (GraphUtils.Instance.KokoajauraList != null && GraphUtils.Instance.KokoajauraList.Any())
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
            MapUtils.Instance.ShowSmoothened(MapUtils.Instance.TspGraphicsLayer.Graphics.ToList(), true);
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

            if (MapUtils.Instance.TspGraphicsLayer != null && MapUtils.Instance.TspGraphicsLayer.Graphics.Any())
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
            MapUtils.Instance.TspGraphicsLayer.Graphics.Clear();
        }

        private void OnDrawResultCommand()
        {
            DrawResult(false);
        }

        private void OnGeneralizeCommand()
        {
            DrawResult(true);
        }

        private void DrawResult(bool generalize)
        {
            if (!MapUtils.Instance.GraphicsLayer.SelectedGraphics.Any())
            {
                this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage("Nothing to draw", "UpdateStatusBar");
                return;
            }
            MapUtils.Instance.ShowGeneralizedRoutes(MapUtils.Instance.GraphicsLayer.SelectedGraphics, generalize);
            this.GetDependencyResolver().Resolve<IMessageMediator>().SendMessage("Drawing done", "UpdateStatusBar");
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
                    CalculateTotalLength(resultList);
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

        private void CalculateTotalLength(List<TspEventArgs> resultList)
        {
            try
            {
                OldTotalLength = AjouraTotalLength;
                AjouraTotalLength = Math.Round(CalculationsUtil.Instance.CalculateTotalLength(resultList));
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

        private async void OnShortestPathCommand()
        {
            List<MapPoint> pisteJoukko = GraphUtils.Instance.GraphVerticesAsMapPoint;

            var startingVertex = await InitShortestPathCommand();
            if (startingVertex == null)
            {
                return;
            }

            while (GraphUtils.Instance.CheckIfKokoajaUratCoverTheKuvio(ref pisteJoukko, KokoajauraBufferValue))
            {
                var endingVertex = MapUtils.Instance.FindFarmostPointInsideListOfPoint(startingVertex, pisteJoukko);
                if (endingVertex != null)
                {
                    StartingVertex = GraphUtils.Instance.AddKokoajauraFromStartPointToEndPoint(startingVertex, endingVertex);
                }
                else
                {
                    break;
                }
            }

            MapUtils.Instance.HighlightEdges(GraphUtils.Instance.KokoajauraList.SelectMany(o => o));
            MapUtils.Instance.ShowSmoothened(MapUtils.Instance.GraphicsLayer.SelectedGraphics, true);
            CalculateKokoajaUraTotalLength();
        }

        private async Task<MapPoint> InitShortestPathCommand()
        {
            MessageMediator.SendMessage("Set storage location", "NaytaInfoboksiKayttajalle");
            GraphUtils.Instance.KokoajauraList = new List<List<GraphEdgeClass>>();
            var startingVertex = await MapUtils.Instance.GetPointFromMap();
            GraphUtils.Instance.SetKokoajauratAsVisitedAndResetShortestPaths();
            return startingVertex;
        }

        private void CalculateKokoajaUraTotalLength()
        {
            KokoajauraTotalLength = Math.Round(CalculationsUtil.Instance.CalculateKokoajaUraTotalLength(GraphUtils.Instance.KokoajauraList));
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
            var viewModelManager = (IViewModelManager)Catel.IoC.ServiceLocator.Default.ResolveType(typeof(IViewModelManager));
            var viewModel = (KarttaViewModel)viewModelManager.ActiveViewModels.FirstOrDefault(vm => vm is KarttaViewModel);
            if (viewModel != null) await viewModel.InitializeMapAsync();

            await MapUtils.Instance.AddFeatureLayersAsGraphic();
            GraphUtils.Instance.AddFeatureLayersToGraph();
            Layers = MapUtils.MapViewService.Map.Layers;
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
