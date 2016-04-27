using System.Collections.ObjectModel;
using System.Windows.Input;
using ArcGISRuntime.Samples.DesktopViewer.Model;
using Catel.Data;
using Catel.MVVM;
using Esri.ArcGISRuntime.Layers;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArcGISRuntime.Samples.DesktopViewer.Utils;
using ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2;
using Catel;
using Catel.IoC;
using Catel.Messaging;

namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public Command<FeatureLayerMenuItem> OpenFeaturesCommand { get; private set; }
        public Command UpdateMenuCommand { get; private set; }
        public Command LoadLayersCommand { get; private set; }

        public Command ShortestPathCommand { get; private set; }

        public Command LoadGraphCommand { get; private set; }

        public Command MinimumSpanningTreeCommand { get; private set; }

        public Command ClearHightlightCommand { get; private set; }

        public Command QueryMethod { get; private set; }

        public Command TspCommand { get; private set; }

        public Command SetTspNodesCommand { get; set; }

        public Command ToggleGraphicsCommand { get; set; }

        public Command GeneralizeCommand { get; set; }

        public Command ClearGeneralizationCommand { get; set; }

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

        public Tuple<int, int> SelectedPath
        {
            get { return GetValue<Tuple<int, int>>(SelectedPathProperty); }
            set { SetValue(SelectedPathProperty, value); }
        }
        public static readonly PropertyData SelectedPathProperty = RegisterProperty("SelectedPath", typeof(Tuple<int, int>), null, SelectedPathPropertyChangedEventHandler);



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

        public int TotalGenerations
        {
            get { return GetValue<int>(TotalGenerationsProperty); }
            set { SetValue(TotalGenerationsProperty, value); }
        }
        public static readonly PropertyData TotalGenerationsProperty = RegisterProperty("TotalGenerations", typeof(int));

        public int Scale
        {
            get { return GetValue<int>(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }
        public static readonly PropertyData ScaleProperty = RegisterProperty("Scale", typeof(int), 100000, ScalePropertyChangedEventHandler);

        public bool UseOnlyDistances
        {
            get { return GetValue<bool>(UseOnlyDistancesProperty); }
            set { SetValue(UseOnlyDistancesProperty, value); }
        }
        public static readonly PropertyData UseOnlyDistancesProperty = RegisterProperty("UseOnlyDistances", typeof(bool), false, UseOnlyDistancesPropertyChangedEventHandler);

        private static void UseOnlyDistancesPropertyChangedEventHandler(object sender, AdvancedPropertyChangedEventArgs e)
        {
            GraphUtils.Instance.UseOnlyDistances = (bool) e.NewValue;
        }

        public bool UseVisitedEdges
        {
            get { return GetValue<bool>(UseVisitedEdgesProperty); }
            set { SetValue(UseVisitedEdgesProperty, value); }
        }
        public static readonly PropertyData UseVisitedEdgesProperty = RegisterProperty("UseVisitedEdges", typeof(bool), false, UseVisitedEdgesPropertyChangedEventHandler);

        private static void UseVisitedEdgesPropertyChangedEventHandler(object sender, AdvancedPropertyChangedEventArgs e)
        {
            GraphUtils.Instance.UseVisitedEdges = (bool)e.NewValue;
        }


        public MainWindowViewModel()
        {
            InitCommands();
            InitMessages();

            TourPaths = new List<Tuple<int, int>>();
            InitialPopulation = 10000;
            TotalGenerations = 100000;
        }

        private void InitMessages()
        {
            MessageMediator.Register<TspEventArgs>(this, UpdatePathsCombobox, "UpdateRoutesOnMap");
            MessageMediator.Register<string>(this, OnUpdateStatusBar, "UpdateStatusBar");
            MessageMediator.Register<FeatureLayerMenuItem>(this, OnAddMenuItem, "AddMenuItem");
            MessageMediator.Register<int>(this, OnScaleChanged, "ScaleChanged");
        }

        private void OnScaleChanged(int obj)
        {
            Scale = obj;
        }

        private void InitCommands()
        {
            OpenFeaturesCommand = new Command<FeatureLayerMenuItem>(OnOpenFeaturesCommand);
            UpdateMenuCommand = new Command(OnUpdateMenuCommand);
            TspCommand = new Command(OnTspCommand, CanTspCommand);
            TspOnAllCommand = new Command(OnTspOnAllCommand, CanGraphCommand);
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
        }

     

      

        private void UpdatePathsCombobox(TspEventArgs tspEventArgs)
        {
            TspVertexList = tspEventArgs.TSPVertexList;
            TourPaths = MapUtils.Instance.GetTourOnOrder(tspEventArgs.BestTour);
        }

        private static async void ScalePropertyChangedEventHandler(object sender, AdvancedPropertyChangedEventArgs e)
        {
            await MapUtils.Instance.MapView.ZoomToScaleAsync(Convert.ToDouble(e.NewValue));
        }

        private bool HasTspGraphics()
        {
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
            MapUtils.Instance.RemoveVerticesFromMap();
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
                _cancellation = new CancellationTokenSource();
                var token = _cancellation.Token;
                _tspIsRunning = true;
                if (!useList)
                {
                    var startingVertex = await GraphUtils.Instance.AskUserForVertex();
                    await Task.Run(() => GraphUtils.Instance.TspAlgorithm(token, InitialPopulation, TotalGenerations, null, startingVertex), token);
                }
                else
                {
                    await Task.Run(() => GraphUtils.Instance.TspAlgorithm(token, InitialPopulation, TotalGenerations, GraphVertexList), token);
                }
                _tspIsRunning = false;
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

        private void OnUpdateStatusBar(string message)
        {
            MainWindowStatusBarText = message;
        }

        private bool CanClearHighlightCommand()
        {
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
            MessageMediator.SendMessage("Set storage location", "NaytaInfoboksiKayttajalle");
            var startingVertex = await MapUtils.Instance.GetPointFromMap();
            //MessageMediator.SendMessage("Set route ending point", "NaytaInfoboksiKayttajalle");
            var endingVertex = MapUtils.Instance.FindFarmostVertexInsideGraph(startingVertex, GraphUtils.Instance.Graph);
            if (startingVertex != null && endingVertex != null)
            {
                GraphUtils.Instance.GetShortestPathFromVertexToVertex(startingVertex, endingVertex);
            }

        }

        private void OnMinimumSpanningTreeCommand()
        {
            GraphUtils.Instance.GetMinimumSpanningTree();
        }

        private bool CanLoadGraphCommand()
        {
            if (MapUtils.Instance.Map.Layers.OfType<FeatureLayer>().Any())
            {
                return true;
            }
            return false;
        }

        private async void OnLoadGraphCommand()
        {
            var result = await GraphUtils.Instance.AddFeatureLayersToGraph();
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

        private async void OnLoadLayersCommand()
        {
            MapUtils.Instance.AddFeatureLayersAsGraphic();
            await GraphUtils.Instance.AddFeatureLayersToGraph();
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
