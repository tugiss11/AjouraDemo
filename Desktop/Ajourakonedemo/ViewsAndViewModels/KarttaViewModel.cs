using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ArcGISRuntime.Samples.DesktopViewer.Services;
using ArcGISRuntime.Samples.DesktopViewer.Utils;
using Catel.Data;
using Catel.IoC;
using Catel.Logging;
using Catel.Messaging;
using Catel.MVVM;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;

namespace ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels
{

    public class KarttaViewModel : Catel.MVVM.ViewModelBase
    {
        #region Fields

        private int _oldscale;
        protected readonly ILog log = LogManager.GetCurrentClassLogger();

        #endregion

        public IMessageMediator Mediator
        {
            get { return Catel.IoC.ServiceLocator.Default.ResolveType<IMessageMediator>(); }
        }

        public Point InfoBoxiAnchorPoint
        {
            get { return GetValue<Point>(InfoBoxiAnchorPointProperty); }
            set { SetValue(InfoBoxiAnchorPointProperty, value); }
        }

        public static readonly PropertyData InfoBoxiAnchorPointProperty = RegisterProperty("InfoBoxiAnchorPoint", typeof (Point));

        public string InfoBoxiKayttajalle
        {
            get { return GetValue<string>(InfoBoxiKayttajalleProperty); }
            set { SetValue(InfoBoxiKayttajalleProperty, value); }
        }

        public static readonly PropertyData InfoBoxiKayttajalleProperty = RegisterProperty("InfoBoxiKayttajalle", typeof (string));

        public string InfoTeksti
        {
            get { return GetValue<string>(InfoTekstiProperty); }
            set { SetValue(InfoTekstiProperty, value); }
        }

        public static readonly PropertyData InfoTekstiProperty = RegisterProperty("InfoTeksti", typeof (string));

        public MapView MyMapView
        {
            get
            {
                var service = Catel.IoC.ServiceLocator.Default.GetService(typeof (MapViewService)) as MapViewService;
                if (service != null)
                    return service.MapView;

                return null;
            }
        }

        public Map Map
        {
            get { return GetValue<Map>(MapProperty); }
            set { SetValue(MapProperty, value); }
        }

        public static readonly PropertyData MapProperty = RegisterProperty("Map", typeof (Map));

        private MapViewService _mapViewService;

        public MapViewService KarttaMapViewService
        {
            get
            {
                if (_mapViewService == null)
                {
                    _mapViewService = new MapViewService();
                    Catel.IoC.ServiceLocator.Default.RegisterInstance(typeof (MapViewService), _mapViewService);
                }

                return _mapViewService;
            }
        }

        #region Constructors

        public KarttaViewModel()
        {
            Mediator.Register<string>(this, OnNaytaInfoboksiKayttajalle, "NaytaInfoboksiKayttajalle");
            Mediator.Register<string>(this, OnNaytaInfoTekstiKayttajalle, "NaytaInfotekstiKayttajalle");
            MapViewNavigationCompletedCommand = new Command<EventArgs>(OnMapViewNavigationCompletedCommand);

            MapViewMouseMoveCommand = new Command<MouseEventArgs>(OnMapViewMouseMove);

            InitializeMap();
        }


        //protected override  InitializeAsync()
        //{

        //log.Info("Starting Initialize KarttaViewModelBase!");
        //await InitializeMapAsync();
        //log.Info("KarttaViewModelBase InitializeAsync done!");
        //}

        #endregion


        #region Commands

        public Command<MapViewInputEventArgs> MapTappedCommand { get; protected set; }
        public Command<MouseEventArgs> MapViewMouseMoveCommand { get; protected set; }
        public Command<EventArgs> MapViewNavigationCompletedCommand { get; protected set; }
        public Command ZoomInCommand { get; protected set; }
        public Command ZoomOutCommand { get; protected set; }
        public Command ClearSelectionCommand { get; protected set; }
        public Command<MouseEventArgs> MouseEnterCommand { get; protected set; }
        public Command MakeSelectionCommand { get; protected set; }
        public Command<MapViewInputEventArgs> MapTappedQueryMethodCommand { get; protected set; }
        public Command ZoomWithExtentCommand { get; protected set; }

        #endregion

        #region Methods


        public async Task InitializeMapAsync()
        {

            Mediator.SendMessage("Loading layers...", "UpdateStatusBar");
            Map.Layers.Clear();

            string kuviorajatPath = ConfigurationManager.AppSettings["kuviot"];
            await MapUtils.Instance.LoadKuvioRajatFeatureTableAsync(kuviorajatPath);

            //string basemapUri = ConfigurationManager.AppSettings["basemapUri"];
            //await MapUtils.Instance.LoadBasemapAsync(basemapUri, "Taustakartta");

            string tpkPath2 = ConfigurationManager.AppSettings["wetness"];
            await MapUtils.Instance.LoadArcGisLocalTiledLayerAsync(tpkPath2, Path.GetFileName(tpkPath2), false);


            string wmsPath = ConfigurationManager.AppSettings["taustakarttaWms"];
            await MapUtils.Instance.LoadWmsLayerAsync(wmsPath, "Taustakartta", string.Empty, false);

            string tpkPath = ConfigurationManager.AppSettings["countours"];
            await MapUtils.Instance.LoadArcGisLocalTiledLayerAsync(tpkPath, Path.GetFileName(tpkPath), false);

          
            string tpkPath4 = ConfigurationManager.AppSettings["borders"];
            await MapUtils.Instance.LoadRuntimeContentLayerAsync(tpkPath4, Path.GetFileName(tpkPath4));

            string tpkPath3 = ConfigurationManager.AppSettings["linedata"];
            // await MapUtils.Instance.LoadArcGisLocalTiledLayerAsync(tpkPath3, Path.GetFileName(tpkPath3), false);
            await MapUtils.Instance.LoadArcGisShapefileLayerAsync(tpkPath3, Path.GetFileName(tpkPath3), false, false, Colors.MediumVioletRed);
     
            string path = ConfigurationManager.AppSettings["Edges"];
            await MapUtils.Instance.LoadArcGisShapefileLayerAsync(path, Path.GetFileName(path), true, true);

            string nodesPath = ConfigurationManager.AppSettings["Nodes"];
            await MapUtils.Instance.LoadArcGisShapefileLayerAsync(nodesPath, Path.GetFileName(nodesPath), true, true);

           
            //string path2 = ConfigurationManager.AppSettings["HexPath"];
            //await MapUtils.Instance.LoadArcGisShapefileLayerAsync(path2, "Hex");


            Mediator.SendMessage("Loading done!", "UpdateStatusBar");
            Mediator.SendMessage(true, "OnLoaded");


        }

        private void InitializeMap()
        {
            var sr = SpatialReference.Create(3067);
            var map = new Map
            {
                SpatialReference = sr,
                InitialViewpoint = new ViewpointExtent(new Envelope(46167.837979319, 6629395.36843484, 762231.492641991,
                        7810900.49878568, sr))
            };
            Map = map;

            var viewModelManager = (IViewModelManager)Catel.IoC.ServiceLocator.Default.ResolveType(typeof(IViewModelManager));
            var viewModel = (MainWindowViewModel)viewModelManager.ActiveViewModels.FirstOrDefault(vm => vm is MainWindowViewModel);
            if (viewModel != null)
            {
                try
                {
                    viewModel.OnLoadLayersCommand();
                }
                catch (Exception ex)
                {
                    NaytaInfoboksiKayttajalle(ex.Message);
                    log.Error(ex);
                }
               
            }
        }

        private void OnNaytaInfoboksiKayttajalle(string infoteksti)
        {
            NaytaInfoboksiKayttajalle(infoteksti);
        }

        private void NaytaInfoboksiKayttajalle(string infoteksti)
        {
            LuoInfoboxinAnchorpoint();
            InfoBoxiKayttajalle = infoteksti;
            RaisePropertyChanged("InfoBoxiKayttajalle");
            if (KarttaMapViewService.MapView.Overlays.Items.Count > 0)
            {
                ViewBase.SetViewOverlayAnchor((FrameworkElement) KarttaMapViewService.MapView.Overlays.Items[0], KarttaMapViewService.MapView.ScreenToLocation(InfoBoxiAnchorPoint));
            }
        }

        private void OnNaytaInfoTekstiKayttajalle(string infoteksti)
        {
            LuoInfoboxinAnchorpoint();
            InfoTeksti = infoteksti;
            RaisePropertyChanged("InfoTeksti");
            if (KarttaMapViewService.MapView.Overlays.Items.Count > 0)
            {
                ViewBase.SetViewOverlayAnchor((FrameworkElement) KarttaMapViewService.MapView.Overlays.Items[0], KarttaMapViewService.MapView.ScreenToLocation(InfoBoxiAnchorPoint));
            }
        }

        private void LuoInfoboxinAnchorpoint()
        {
            var y = KarttaMapViewService.MapView.ActualHeight - 25;
            var x = KarttaMapViewService.MapView.ActualWidth/2 - 25;
            InfoBoxiAnchorPoint = new Point {X = x, Y = y};
        }



        protected void OnMapViewNavigationCompletedCommand(EventArgs e)
        {
            var scale = (int) KarttaMapViewService.MapView.Scale;
            if (_oldscale == scale) return;
            _oldscale = scale;
            Mediator.SendMessage(scale, "ScaleChanged");
        }

        #endregion

        #region MapViewTooltip

        public bool OnkoMapTooltipVisible
        {
            get { return GetValue<bool>(OnkoMapTooltipVisibleProperty); }
            set { SetValue(OnkoMapTooltipVisibleProperty, value); }
        }


        


        /// <summary>
        /// Register the OnkoMapTooltipVisible property so it is known in the class.
        /// </summary>
        public static readonly PropertyData OnkoMapTooltipVisibleProperty = RegisterProperty("OnkoMapTooltipVisible", typeof (bool), false);

        public object OverlayDataContext
        {
            get { return GetValue<object>(OverlayDataContextProperty); }
            set { SetValue(OverlayDataContextProperty, value); }
        }

        public static readonly PropertyData OverlayDataContextProperty = RegisterProperty("OverlayDataContext", typeof (object));


        private Point? _pendinglocation;
        private Dictionary<string, object> _oldAttributes;
        private long _oldrow;

        protected async void OnMapViewMouseMove(MouseEventArgs e)
        {
            try
            {
                var iscalculating = (_pendinglocation != null);
                _pendinglocation = e.GetPosition(KarttaMapViewService.MapView);
                if (iscalculating)
                    return;
                var attr = await GetFeatureUnderPoint(e);
                if (attr == null)
                {
                    OnkoMapTooltipVisible = false;

                }
                else
                {
                    OverlayDataContext = attr;

                    OnkoMapTooltipVisible = true;
                    var anchorpoint = new Point {X = _pendinglocation.Value.X - 25, Y = _pendinglocation.Value.Y - 25};

                    if (KarttaMapViewService.MapView.Overlays.Items.Count > 0)
                        ViewBase.SetViewOverlayAnchor((FrameworkElement) KarttaMapViewService.MapView.Overlays.Items[1], KarttaMapViewService.MapView.ScreenToLocation(anchorpoint));
                }
                _pendinglocation = null;
            }
            catch (Exception ex)
            {
                NaytaInfoboksiKayttajalle(ex.Message);
                log.Error(ex);
            }


        }

        public async Task<Dictionary<string, object>> GetFeatureUnderPoint(MouseEventArgs e)
        {
            try
            {
                var edgesFeatureLayer = Map.Layers[Path.GetFileName(ConfigurationManager.AppSettings["Edges"])] as FeatureLayer;
                if (edgesFeatureLayer == null)
                    return null;
                var nodesLayer = Map.Layers[Path.GetFileName(ConfigurationManager.AppSettings["Nodes"])] as FeatureLayer;
                if (nodesLayer == null)
                    return null;

                var screenPoint = _pendinglocation;
                if (screenPoint != null)
                {
                    var rows = await edgesFeatureLayer.HitTestAsync(KarttaMapViewService.MapView, (Point) screenPoint);
                   
                    var rows2 = await nodesLayer.HitTestAsync(KarttaMapViewService.MapView, (Point)screenPoint);

                    if (!rows.Any() && !rows2.Any())
                    {
                        return null;
                    }

                    if (rows.FirstOrDefault() == _oldrow)
                    {
                        return _oldAttributes;
                    }
                    _oldrow = rows.FirstOrDefault();
                    var edgeFeatures = await edgesFeatureLayer.FeatureTable.QueryAsync(rows);
                    var feature1 = edgeFeatures.FirstOrDefault();
                   

                    var nodeFeatures = await nodesLayer.FeatureTable.QueryAsync(rows2);
                    var feature2 = nodeFeatures.FirstOrDefault();
                   

                    var result = CreateTooltipDictionary(feature1, feature2);
                    _oldAttributes = result;
                    return result;
                }
            }
            catch (Exception ex)
            {
                NaytaInfoboksiKayttajalle(ex.Message);
                log.Error(ex);
            }
            return null;
        }

        private Dictionary<string, object> CreateTooltipDictionary(Feature feature1, Feature feature2)
        {
            var result = new Dictionary<string, object>();
            if (feature1 !=null)
            {
                result.Add("Sivukaltevuus:", feature1.Attributes["sivukalt"]);
                result.Add("Nousekaltevuus:", feature1.Attributes["nousukalt"]);
                result.Add("Kulkukelpoisuus", feature1.Attributes["kulkukelp"]);
            }
            if (ConfigurationManager.AppSettings["hasPuudata"] == "true")
            {
                if (feature2 != null)
                {
                    result.Add("T (ikä)", feature2.Attributes["T"]);
                    result.Add("H (korkeus)", feature2.Attributes["H"]);
                    result.Add("N (rl)", feature2.Attributes["N"]);
                    result.Add("D (lpm)", feature2.Attributes["D"]);
                    result.Add("V (til)", feature2.Attributes["V"]);
                    result.Add("G (ppa)", feature2.Attributes["G"]);

                }
            }
            return result;
           
        }

        #endregion
    }
}
