using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGISRuntime.Samples.DesktopViewer.Services;
using ArcGISRuntime.Samples.DesktopViewer.Utils;
using Catel.Data;
using Catel.IoC;
using Catel.Logging;
using Catel.Messaging;
using Catel.MVVM;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Geometry;

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
        public static readonly PropertyData InfoBoxiAnchorPointProperty = RegisterProperty("InfoBoxiAnchorPoint", typeof(Point));

        public string InfoBoxiKayttajalle
        {
            get { return GetValue<string>(InfoBoxiKayttajalleProperty); }
            set { SetValue(InfoBoxiKayttajalleProperty, value); }
        }
        public static readonly PropertyData InfoBoxiKayttajalleProperty = RegisterProperty("InfoBoxiKayttajalle", typeof(string));

        public string InfoTeksti
        {
            get { return GetValue<string>(InfoTekstiProperty); }
            set { SetValue(InfoTekstiProperty, value); }
        }
        public static readonly PropertyData InfoTekstiProperty = RegisterProperty("InfoTeksti", typeof(string));

        public Map Map
        {
            get
            {
                return GetValue<Map>(MapProperty);
            }
            set
            {
                SetValue(MapProperty, value);
            }
        }
        public static readonly PropertyData MapProperty = RegisterProperty("Map", typeof(Map));

        private MapViewService _mapViewService;
        public MapViewService MapViewService
        {
            get
            {
                if (_mapViewService == null)
                {
                    _mapViewService = new MapViewService();
                    Catel.IoC.ServiceLocator.Default.RegisterInstance(typeof(MapViewService), _mapViewService);
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
        }


        protected override async void Initialize()
        {
            base.Initialize();
            log.Info("Starting Initialize KarttaViewModelBase!");
            await InitializeAsync();
            log.Info("KarttaViewModelBase InitializeAsync done!");
        }

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


        public async Task InitializeAsync()
        {
            Mediator.SendMessage("Loading layers...", "UpdateStatusBar");

            InitializeMap();

            string basemapUri = ConfigurationManager.AppSettings["basemapUri"];
            await MapUtils.Instance.LoadBasemapAsync(basemapUri, "Taustakartta");

            string path = ConfigurationManager.AppSettings["GridPath"];
            await MapUtils.Instance.LoadArcGisShapefileLayerAsync(path, "Grid");

            //string path2 = ConfigurationManager.AppSettings["HexPath"];
            //await MapUtils.Instance.LoadArcGisShapefileLayerAsync(path2, "Hex");


            Mediator.SendMessage("Loading done", "UpdateStatusBar");


        }

        private void InitializeMap()
        {
            Map = new Map
            {
                SpatialReference = SpatialReference.Create(3067),
                InitialViewpoint =
                    new ViewpointExtent(new Envelope(46167.837979319, 6629395.36843484, 762231.492641991,
                        7810900.49878568))
            };

            MapViewService.MapView.Map = Map;
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
            if (MapViewService.MapView.Overlays.Items.Count > 0)
            {
                ViewBase.SetViewOverlayAnchor((FrameworkElement)MapViewService.MapView.Overlays.Items[0], MapViewService.MapView.ScreenToLocation(InfoBoxiAnchorPoint));
            }
        }

        private void OnNaytaInfoTekstiKayttajalle(string infoteksti)
        {
            LuoInfoboxinAnchorpoint();
            InfoTeksti = infoteksti;
            RaisePropertyChanged("InfoTeksti");
            if (MapViewService.MapView.Overlays.Items.Count > 0)
            {
                ViewBase.SetViewOverlayAnchor((FrameworkElement)MapViewService.MapView.Overlays.Items[1], MapViewService.MapView.ScreenToLocation(InfoBoxiAnchorPoint));
            }
        }

        private void LuoInfoboxinAnchorpoint()
        {
            var y = MapViewService.MapView.ActualHeight - 25;
            var x = MapViewService.MapView.ActualWidth / 2 - 25;
            InfoBoxiAnchorPoint = new Point { X = x, Y = y };
        }

        #region Muut työkalut

        protected void OnMapViewNavigationCompletedCommand(EventArgs e)
        {
            var scale = (int)MapViewService.MapView.Scale;
            if (_oldscale == scale) return;
            _oldscale = scale;
            Mediator.SendMessage(scale, "ScaleChanged");
        }

        #endregion

        #endregion


    }
}
