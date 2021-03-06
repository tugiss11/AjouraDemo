﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ArcGISRuntime.Samples.DesktopViewer.Extensions;
using ArcGISRuntime.Samples.DesktopViewer.Model;
using ArcGISRuntime.Samples.DesktopViewer.Services;
using ArcGISRuntime.Samples.DesktopViewer.Utils.TSP2;
using ArcGISRuntime.Samples.DesktopViewer.ViewsAndViewModels;
using Catel.IoC;
using Catel.Logging;
using Catel.Messaging;
using Catel.MVVM;
using Catel.Services;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using Tsp;
using Application = System.Windows.Application;
using Color = System.Windows.Media.Color;
using Geometry = Esri.ArcGISRuntime.Geometry.Geometry;

namespace ArcGISRuntime.Samples.DesktopViewer.Utils
{
    public class MapUtils
    {
        protected readonly ILog log = LogManager.GetCurrentClassLogger();
        private static readonly MapUtils _instance = new MapUtils();
        private IViewModelManager _viewModelManager;
        private IMessageMediator _mes;
        internal const string KulkukelpoisuusString = "kulkukelp";
        internal const string SivukaltString = "sivukalt";
        internal const string NousukaltString = "nousukalt";

        public MapUtils()
        {
            SavedHightlights = new List<Graphic>();
            //MessageMediator.Register<TspEventArgs>(this, SaveTspEventArgs, "UpdateRoutesOnMap");
        }

        private readonly SimpleFillSymbol _simpleFillSymbol = new SimpleFillSymbol
        {
            Color = Colors.Blue,
            Outline =
                new SimpleLineSymbol
                {
                    Color = Colors.Blue,
                    Style = SimpleLineStyle.Solid,
                    Width = 1
                }
        };

        private readonly SimpleLineSymbol _simpleLineSymbol = new SimpleLineSymbol
        {
            Color = Colors.Blue,
            Style = SimpleLineStyle.Solid,
            Width = 1
        };

        private GraphicsLayer _graphicsLayer;
        private GraphicsLayer _resultGraphics;
        private GraphicsLayer _tspVerticesLayer;


        public IViewModelManager ViewModelManager
        {
            get
            {
                if (_viewModelManager != null) return _viewModelManager;
                _viewModelManager = this.GetDependencyResolver().Resolve<IViewModelManager>();
                return _viewModelManager;
            }
        }

        public IMessageMediator MessageMediator
        {
            get
            {
                if (_mes != null) return _mes;
                _mes = this.GetDependencyResolver().Resolve<IMessageMediator>();
                return _mes;
            }
        }

        public static MapUtils Instance
        {
            get { return _instance; }
        }

        public IEnumerable<Feature> KuvioRajatTable { get; set; }

        public GraphicsLayer GraphicsLayer
        {
            get
            {
                if (_graphicsLayer == null)
                {
                    GraphicsLayer = new GraphicsLayer
                    {
                        DisplayName = "Feature Graphics",
                        SelectionColor = Colors.Red
                    };
                    Map.Layers.Add(GraphicsLayer);
                }
                return _graphicsLayer;
            }
            set { _graphicsLayer = value; }
        }

        public GraphicsLayer ResultGraphics
        {
            get
            {
                if (_resultGraphics == null)
                {
                    _resultGraphics = new GraphicsLayer { DisplayName = "Result Graphics" };
                    Map.Layers.Add(ResultGraphics);
                }
                return _resultGraphics;
            }
            set { _resultGraphics = value; }
        }

        public GraphicsLayer TspVerticesLayer
        {
            get
            {
                if (_tspVerticesLayer == null)
                {
                    _tspVerticesLayer = new GraphicsLayer { DisplayName = "Point Graphics" };
                    var labeling = new LabelProperties();
                    var labelClasses = new AttributeLabelClassCollection
                    {
                        new AttributeLabelClass()
                        {
                            IsVisible = true, TextExpression = "[ID]", LabelPlacement = LabelPlacement.PointAboveCenter, Symbol = new TextSymbol()
                            {
                                BorderLineColor = Colors.Black,
                                BorderLineSize = 0.1,
                                Font = new SymbolFont() {FontFamily = "Courier New", FontSize = 12, FontStyle = SymbolFontStyle.Normal}
                            }
                        }
                    };
                    labeling.LabelClasses = labelClasses;
                    _tspVerticesLayer.Labeling = labeling;
                    Map.Layers.Add(TspVerticesLayer);
                }


                return _tspVerticesLayer;
            }
            set { _tspVerticesLayer = value; }
        }

        public TspEventArgs LatestTspEventArgs { get; set; }

        public static MapViewService MapViewService
        {
            get { return ServiceLocator.Default.GetService(typeof(MapViewService)) as MapViewService; }
        }

        public MapView MapView
        {
            get { return MapViewService.MapView; }
        }

        public Map Map
        {
            get { return MapViewService.MapView.Map; }
        }

        public IEnumerable<Graphic> SavedHightlights { get; set; }
        public int MaxAllowedSlope { get; set; }

        public int MaxAllowedForwardSlope { get; set; }

        #region Methods

        public async Task ZoomToGeometryAsync(Geometry geometry)
        {
            if (geometry != null && geometry.Extent != null)
                await MapView.SetViewAsync(geometry, new Thickness(100));
        }

        public async Task LoadFeatureLayerAsync(string displayName, string uri = null, string tablename = null)
        {
            log.Info(string.Format("OpenAsync {0}{1}", uri, Environment.NewLine));

            ArcGISFeatureTable ft = await ServiceFeatureTable.OpenAsync(new Uri(uri));
            log.Info(string.Format("OpenAsync done {0}{1}", uri, Environment.NewLine));


            if (ft == null || ft.GeometryType == GeometryType.Unknown)
            {
                return;
            }

            var layer = new FeatureLayer
            {
                FeatureTable = ft,
                DisplayName = displayName,
                ID = ft.Name,
            };
            layer.MinScale = 0;
            layer.MaxScale = 0;
            layer.IsVisible = true;

            await layer.InitializeAsync();

            await CreateSwatchAsync(layer.Renderer);
            Map.Layers.Add(layer);

            UpdateMenu(layer);
        }

        private void UpdateMenu(FeatureLayer layer)
        {

            var menuItem = new FeatureLayerMenuItem { Header = layer.DisplayName, Layer = layer };
            MessageMediator.SendMessage("AddMenuItem", menuItem);
        }

        private async Task CreateSwatchAsync(Renderer renderer)
        {
            var uniqueValueRenderer = renderer as UniqueValueRenderer;
            var classBreaksRenderer = renderer as ClassBreaksRenderer;

            var field = string.Empty;
            if (uniqueValueRenderer != null)
            {
                if (uniqueValueRenderer.Fields != null && uniqueValueRenderer.Fields.Count > 0)
                {
                    field = uniqueValueRenderer.Fields[0] ?? string.Empty;
                }
            }
            else if (classBreaksRenderer != null && classBreaksRenderer.Field != null)
            {
                field = classBreaksRenderer.Field;
            }

            if (uniqueValueRenderer != null)
            {
                foreach (var info in uniqueValueRenderer.Infos)
                {
                    UniqueValueInfo info1 = info;
                    var imageLabel = await GetImageLabelFromSymbol(info1.Symbol, field, info1.Label, Convert.ToString(info1.Values[0]));
                    if (imageLabel == null) continue;
                }
            }
            else if (classBreaksRenderer != null)
            {
                foreach (var info in classBreaksRenderer.Infos)
                {
                    ClassBreakInfo info1 = info;
                    var imageLabel = await GetImageLabelFromSymbol(info1.Symbol, field, info1.Label);
                    if (imageLabel == null) continue;

                }
            }
        }

        private async Task<WriteableBitmap> GetImageLabelFromSymbol(Symbol symbol, string field, string label, string value = "")
        {
            WriteableBitmap image = await GetWriteableBitMapFromSymbol(symbol);
            return image;
        }

        private async Task<WriteableBitmap> GetWriteableBitMapFromSymbol(Symbol symbol)
        {
            log.Info(string.Format("Creating swatch {0}", Environment.NewLine));
            var result = await symbol.CreateSwatchAsync();
            log.Info(string.Format("Creating swatch done {0}", Environment.NewLine));
            return result as WriteableBitmap;
        }


        public async Task LoadBasemapAsync(string uri, string displayname)
        {
            var basemap = new ArcGISDynamicMapServiceLayer(new Uri(uri))
            {
                DisplayName = displayname,
                ID = displayname,
                MaxScale = 0,
                MinScale = 0,
                IsVisible = true
            };

            log.Info(string.Format("InitAsync {0}{1}", uri, Environment.NewLine));
            await basemap.InitializeAsync();
            log.Info(string.Format("InitAsync done {0}{1}", uri, Environment.NewLine));
            Map.Layers.Insert(0, basemap);
        }

        public Envelope GetEnvelopeFromGeometries(IList<Geometry> geometryList)
        {
            if (geometryList == null || geometryList.Count <= 0) return null;

            geometryList = geometryList.Where(o => o != null).ToList();

            var hasActualGeometry = geometryList.Any(geometry => !geometry.IsEmpty);

            if (!hasActualGeometry) return null;


            double minX = geometryList.Min(g => g.Extent.XMin);
            double maxX = geometryList.Max(g => g.Extent.XMax);
            double minY = geometryList.Min(g => g.Extent.YMin);
            double maxY = geometryList.Max(g => g.Extent.YMax);


            return new Envelope(minX, minY, maxX, maxY);
        }

        public async Task ZoomToGeometryAsync(List<Geometry> geometryList, double scale = 0)
        {
            if (geometryList == null || geometryList.Count < 1)
            {
                return;
            }

            var envelope = GetEnvelopeFromGeometries(geometryList);
            if (envelope == null)
            {
                return;
            }

            if (scale > 0)
            {
                await MapViewService.MapView.SetViewAsync(GeometryEngine.LabelPoint(envelope), scale);
            }
            else
            {
                await MapViewService.MapView.SetViewAsync(envelope);
            }
        }

        public async Task LoadArcGisShapefileLayerAsync(string path, string displayname, bool isVisible = true, bool useFeatureLayer = false, Color color = default(Color))
        {
            try
            {
                if (File.Exists(path))
                {
                    ShapefileTable shapeFileTable = await ShapefileTable.OpenAsync(path);
                    Layer featureLayer;

                    if (!useFeatureLayer)
                    {

                        featureLayer = new GraphicsLayer()
                        {
                            DisplayName = displayname,
                            IsVisible = isVisible,
                            ID = displayname
                        };
                        featureLayer.MinScale = 5000;

                        var linesymbol = new SimpleLineSymbol
                        {
                            Color = color,
                            Style = SimpleLineStyle.Solid,
                            Width = 3
                        };
                        var kaikkiFeaturet = await shapeFileTable.QueryAsync(new QueryFilter() { WhereClause = "1=1" });
                        foreach (var feature in kaikkiFeaturet)
                        {
                            if (feature.Geometry.GeometryType == GeometryType.Polyline)
                            {
                                ((GraphicsLayer)featureLayer).Graphics.Add(new Graphic(GeometryEngine.Project(feature.Geometry, Map.SpatialReference), feature.Attributes, linesymbol));
                            }
                            else
                            {
                                ((GraphicsLayer)featureLayer).Graphics.Add(new Graphic(GeometryEngine.Project(feature.Geometry, Map.SpatialReference), feature.Attributes));
                            }
                        }
                    }
                    else
                    {
                        featureLayer = new FeatureLayer()
                        {
                            DisplayName = displayname,
                            IsVisible = isVisible,
                            ID = displayname
                        };
                        ((FeatureLayer)featureLayer).FeatureTable = shapeFileTable;
                    }
                    Map.Layers.Add(featureLayer);
                    await featureLayer.InitializeAsync();


                }
                else
                {
                    log.Error(string.Format("File does not exist {0}", path));
                }

            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        #endregion

        public async Task AddFeatureLayersAsGraphic()
        {
            MessageMediator.SendMessage("Adding graphics...", "UpdateStatusBar");
            if (GraphicsLayer == null)
            {
                GraphicsLayer = new GraphicsLayer();
                GraphicsLayer.DisplayName = "Feature graphics";
            }
            else
            {
                GraphicsLayer.Graphics.Clear();
            }
            GraphicsLayer.Labeling = new LabelProperties();
            GraphicsLayer.Labeling.IsEnabled = true;
            GraphicsLayer.Labeling.LabelClasses = new AttributeLabelClassCollection();
            var labelClass = GetLabelClass("FID");
            GraphicsLayer.Labeling.LabelClasses.Add(labelClass);

            if (Instance.Map.Layers.ContainsLayer(GraphicsLayer))
            {
                Map.Layers.Remove(GraphicsLayer);
            }

            GraphicsLayer.Renderer = CreateClassBreakRenderer();



            var featurelayer = Map.Layers[Path.GetFileName(ConfigurationManager.AppSettings["Edges"])] as FeatureLayer;
            if (featurelayer == null)
            {
                return;
            }
            Instance.Map.Layers.Add(GraphicsLayer);

            var features = await featurelayer.FeatureTable.QueryAsync(new QueryFilter { WhereClause = "1 = 1" });
            foreach (var feature in features)
            {

                var geometry = feature.Geometry;
                //if (!CheckIfPointsAreInsideKuvio(geometry))
                //{
                //    continue;
                //}

                var sivukaltevuus = Math.Abs(Convert.ToDouble(feature.Attributes[SivukaltString]));
                var nousu = Math.Abs(Convert.ToDouble(feature.Attributes[NousukaltString]));

                if (sivukaltevuus > MaxAllowedSlope || nousu > MaxAllowedForwardSlope)
                {
                    continue;
                }
           
                feature.Attributes[NousukaltString] = nousu;
                feature.Attributes[SivukaltString] = sivukaltevuus;

                try
                {
                    if (geometry is Polygon || geometry is Envelope)
                    {
                        GraphicsLayer.Graphics.Add(new Graphic(geometry, feature.Attributes));
                    }
                    else if (geometry is Polyline)
                    {
                        GraphicsLayer.Graphics.Add(new Graphic(geometry, feature.Attributes));
                    }
                    else if (geometry is MapPoint)
                    {
                        GraphicsLayer.Graphics.Add(new Graphic(geometry, feature.Attributes));
                    }

                    if (geometry != null)
                    {
                        MapView.SetView(GeometryEngine.Buffer(geometry, 25));
                    }
                }
                catch (Exception ex)
                {
                    await this.GetDependencyResolver().Resolve<IMessageService>().ShowErrorAsync(ex);
                }



                MessageMediator.SendMessage("Adding graphics done", "UpdateStatusBar");
            }
        }

        private bool CheckIfPointsAreInsideKuvio(Geometry geo)
        {
            var kuviot = KuvioRajatTable;
            foreach (var kuvio in kuviot)
            {
                if (GeometryEngine.Contains(kuvio.Geometry, geo))
                {
                    return true;
                }
            }
            return false;
        }

        private Renderer CreateClassBreakRenderer()
        {
            ClassBreaksRenderer renderer = new ClassBreaksRenderer();
            //renderer.Field = kulkukelpoisuusString;
            //renderer.Infos.Add(CreateClassBreakInfo(Colors.Green, 1, 1));
            //renderer.Infos.Add(CreateClassBreakInfo(Colors.YellowGreen, 2, 2));
            //renderer.Infos.Add(CreateClassBreakInfo(Colors.Yellow, 3, 3));
            //renderer.Infos.Add(CreateClassBreakInfo(Colors.OrangeRed, 4, 4));
            //renderer.Infos.Add(CreateClassBreakInfo(Colors.Red, 5, 5));

            renderer.Field = SivukaltString;
            renderer.Infos.Add(CreateClassBreakInfo(Colors.Green, -1, 1));
            renderer.Infos.Add(CreateClassBreakInfo(Colors.OrangeRed, -13, -10));
            renderer.Infos.Add(CreateClassBreakInfo(Colors.OrangeRed, 10, 13));
            renderer.Infos.Add(CreateClassBreakInfo(Colors.YellowGreen, -6, -1));
            renderer.Infos.Add(CreateClassBreakInfo(Colors.YellowGreen, 1, 6));
            renderer.Infos.Add(CreateClassBreakInfo(Colors.Yellow, -10, -6));
            renderer.Infos.Add(CreateClassBreakInfo(Colors.Yellow, 6, 10));
            renderer.Infos.Add(CreateClassBreakInfo(Colors.Red, 13, 50));
            renderer.Infos.Add(CreateClassBreakInfo(Colors.Red, -50, -13));

            renderer.DefaultSymbol = new SimpleLineSymbol
            {
                Color = Colors.Black,
                Style = SimpleLineStyle.Solid,
                Width = 3
            };
            return renderer;


        }

        private ClassBreakInfo CreateClassBreakInfo(Color color, int min, int max)
        {
            ClassBreakInfo classBreakInfo1 = new ClassBreakInfo();
            classBreakInfo1.Minimum = min;
            classBreakInfo1.Maximum = max;
            classBreakInfo1.Symbol = new SimpleLineSymbol
            {
                Color = color,
                Style = SimpleLineStyle.Solid,
                Width = 3
            };
            return classBreakInfo1;
        }

        private AttributeLabelClass GetLabelClass(string property)
        {
            // -------------------------------------------------------------------------
            // Set up the Classes and Properties useful for labeling the smaller TSPVertices.
            // -------------------------------------------------------------------------

            // Create an AttributeLabelClass. This contains the meat of the instructions for doing labeling. 
            Esri.ArcGISRuntime.Layers.AttributeLabelClass labelClass = new Esri.ArcGISRuntime.Layers.AttributeLabelClass();

            // This option is more useful for labeling PolyLine networks (i.e. roads, railroads, etc.). We don't mind having duplicate TSPVertices if they exist.
            // By default this value is set to .PreserveDuplicates.
            labelClass.DuplicateLabels = Esri.ArcGISRuntime.Layers.DuplicateLabels.PreserveDuplicates;

            // Allow the labeling to show in the map; so set this Property to True.
            // By default this value is set to True.
            labelClass.IsVisible = true;

            // Not interested in wrapping the text of the city name on a second line. 
            // NOTE: If this option were set to True, use the other .WordWrapLength Property to determine when to wrap the text on the 2nd line.  
            // By default this value is set to False.
            labelClass.IsWordWrapEnabled = false;

            // Define the placement of the Attribute text relative to the SimpleMarkerSymbol defined earlier for the smaller and larger TSPVertices. 
            // By default this value is set to .PointAboveCenter.
            labelClass.LabelPlacement = Esri.ArcGISRuntime.Layers.LabelPlacement.LineAboveAlong;

            // If multiple labels conflict for placement (i.e. they overlap), this defines how to handle displaying the conflicts.
            // By default this value is set to .FixedPositionOrRemove.
            labelClass.LabelPosition = Esri.ArcGISRuntime.Layers.LabelPosition.FixedPositionWithOverlaps;

            // Prioritizes which label appears in one AttributeLabelClass -vs- another AttributeLabelClass. Since this is the labeling for the 
            // smaller TSPVertices we will give this AttributeLabelClass the lower priority.
            // By default this value is set to .Medium.
            labelClass.LabelPriority = Esri.ArcGISRuntime.Layers.LabelPriority.Lowest;

            // Define the MaxScale at which the Attribute labels appear. The closer you zoom in on the map then they appear.
            // By default this value is 0.
            labelClass.MaxScale = 0;

            // Define the MinScale at which the Attribute labels appear. The father you zoom out on the map then they disappear. 
            // By default this value is 0.
            labelClass.MinScale = 5000000;

            // Define the text that will appear for the labeling. In this case we want the name of the city to display as the labeling text.
            // So the "[AREANAME]" is a field in the Attributes of the GraphicLayer.
            // This value must be provided or else no labeling will occur.
            labelClass.TextExpression = string.Format("[{0}]", property);


            // Create a new TextSymbol to define the appearance of the text that is displayed.
            Esri.ArcGISRuntime.Symbology.TextSymbol textSymbol = new Esri.ArcGISRuntime.Symbology.TextSymbol();
            textSymbol.Color = System.Windows.Media.Colors.White; // The color of the labeling text.
            textSymbol.BorderLineColor = System.Windows.Media.Colors.Black; // If you want a outline glow surrounding the labeling text.
            textSymbol.BorderLineSize = 0; // How big you want the outline glow around the labeling text.

            // Create a new SymbolFont to define the appearance of the text that is displayed.
            Esri.ArcGISRuntime.Symbology.SymbolFont symbolFont = new Esri.ArcGISRuntime.Symbology.SymbolFont();
            symbolFont.FontFamily = "Courier New"; // Use whatever FontFamily you have on your system. Ex: "Arial" "Verdana" "Times New Roman" "Courier New" "Cooper Black"
            symbolFont.FontSize = 3; // Define the point size of the labeling.
            symbolFont.FontStyle = Esri.ArcGISRuntime.Symbology.SymbolFontStyle.Normal; // Define the FontStyle. Others include: .Italics
            symbolFont.TextDecoration = Esri.ArcGISRuntime.Symbology.SymbolTextDecoration.None; // Supply a TextDecoration if desired. Others include: .LineThrough, .Underline.
            symbolFont.FontWeight = Esri.ArcGISRuntime.Symbology.SymbolFontWeight.Bold; // Supply a SymbolFontWeight if desired. Others include: .Normal.
            textSymbol.Font = symbolFont; // Apply the SymbolFont to the TextSymbol.Font Property.
            labelClass.Symbol = textSymbol;
            return labelClass;

        }

        public List<Graphic> HighlightEdges(IEnumerable<GraphEdgeClass> mst, bool resetSelection = true)
        {
            var idCollection = mst.Select(o => o.Id);

            var result = HightlightIds(idCollection, resetSelection);
            return result;
        }

        private List<Graphic> HightlightIds(IEnumerable<int> ids, bool resetSelection = true)
        {
            var result = new List<Graphic>();
            //MapViewService.MapView.SetView(new Envelope(366555.233146185, 6709824.6234944, 367256.720295555, 6710293.90540683));
            var grlayer = GraphicsLayer;
            if (grlayer != null)
            {
                if (resetSelection)
                {
                    grlayer.ClearSelection();
                }
                else
                {
                    log.Info("Not resetting selection!");
                }

                foreach (var id in ids)
                {
                    var graphic = grlayer.Graphics.FirstOrDefault(o => Convert.ToInt32(o.Attributes["FID"]) == id);
                    if (graphic != null)
                    {
                        result.Add(graphic);
                        graphic.IsSelected = true;
                    }
                    else
                    {
                        log.Info("Nothing to select!");
                    }
                }
            }
            return result;
            //_mes.SendMessage("Hightlights changed!", "UpdateStatusBar");
        }

        public void HighlightEdges(IEnumerable<Graphic> savedHightlights)
        {
            var idCollection = savedHightlights.Select(o => Convert.ToInt32(o.Attributes["FID"]));

            HightlightIds(idCollection);
        }


        public async Task<MapPoint> GetPointFromMap()
        {
            try
            {
                var myEditor = MapView.Editor;
                var myGeometry = await myEditor.RequestPointAsync();

                var unionGeometry = GeometryEngine.Union(GraphicsLayer.Graphics.Select(o => o.Geometry));
                var proximityResult = GeometryEngine.NearestVertex(GeometryEngine.Project(unionGeometry, myGeometry.SpatialReference), myGeometry);

                return proximityResult.Point;
            }
            catch (TaskCanceledException)
            {

            }
            return null;
        }


        public void DrawRoute(object sender, TspEventArgs tspEventArgs)
        {
            if (ResultGraphics == null)
            {
                ResultGraphics = new GraphicsLayer();

                Map.Layers.Add(ResultGraphics);
            }
            else
            {
                ResultGraphics.Graphics.Clear();
            }

            var symbol = new SimpleLineSymbol
            {
                Color = Colors.DeepPink,
                Style = SimpleLineStyle.Solid,
                Width = 2
            };

            var polylineList = new List<Polyline>();

            foreach (var connection in tspEventArgs.BestTour)
            {
                var city1 = tspEventArgs.TSPVertexList[connection.Connection1];
                var city2 = tspEventArgs.TSPVertexList[connection.Connection2];
                var mapPoint1 = new MapPoint(city1.Location.X, city1.Location.Y);
                var mapPoint2 = new MapPoint(city2.Location.X, city2.Location.Y);
                var polyline = new Polyline(new List<MapPoint> { mapPoint1, mapPoint2 }, new SpatialReference(3067));
                polylineList.Add(polyline);
            }

            var polylineUnion = GeometryEngine.Union(polylineList);
            MapViewService.MapView.SetView(polylineUnion);
            ResultGraphics.Graphics.Add(new Graphic(polylineUnion, symbol));
        }

        public void AddVertexToGraphicsLayer(GraphVertexClass vertex)
        {
            if (TspVerticesLayer == null)
            {
                TspVerticesLayer = new GraphicsLayer();
                TspVerticesLayer.DisplayName = "Point Graphics";
                var labeling = new LabelProperties();
                var labelClasses = new AttributeLabelClassCollection { new AttributeLabelClass() { IsVisible = true, TextExpression = "[ID]", LabelPlacement = LabelPlacement.PointAboveCenter } };
                labeling.LabelClasses = labelClasses;
                TspVerticesLayer.Labeling = labeling;
                Map.Layers.Add(TspVerticesLayer);
            }
            var symbol = new SimpleMarkerSymbol()
            {
                Color = Colors.DeepPink,
                Size = 12,
                Style = SimpleMarkerStyle.Circle
            };
            TspVerticesLayer.Graphics.Add(new Graphic(new MapPoint((double)vertex.X, (double)vertex.Y, new SpatialReference(3067)), symbol));
        }

        public void RemoveVerticesFromMap()
        {
            if (TspVerticesLayer != null)
            {
                TspVerticesLayer.Graphics.Clear();
            }
        }

        public void SaveTspEventArgs(TspEventArgs args)
        {
            LatestTspEventArgs = args;
        }

        public void DrawRoutes(TspEventArgs tspEventArgs)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() => DrawRoutesMethod(tspEventArgs)));
        }

        public void DrawRoutesMethod(TspEventArgs tspEventArgs, bool resetSelection = true)
        {
            var polylineList = GetGraphEdgeClassesFromEvents(tspEventArgs);
            HighlightEdges(polylineList, resetSelection);
        }

        internal List<GraphEdgeClass> GetGraphEdgeClassesFromEvents(TspEventArgs tspEventArgs)
        {
            if (tspEventArgs == null)
            {
                return new List<GraphEdgeClass>();
            }

            var polylineList = new List<GraphEdgeClass>();
            //var polylineList2 = new List<GraphEdgeClass>();
            var allVertices = tspEventArgs.TSPVertexList.ToList();
            List<Tuple<int, int>> orderedTour = GetTourOnOrder(tspEventArgs.BestTour);

            //List<Tuple<int, int>> orderedTour2 = new List<Tuple<int, int>>();
            //int lastCity2 = 0;
            //int nextCity2 = tspEventArgs.BestTour[0].Connection1;
            var i = 0;
            while (i < orderedTour.Count)
            {
                var lastCity = orderedTour[i].Item1;
                var nextCity = orderedTour[i].Item2;
                //ShortestPath path = GraphUtils.Instance.ShortestPathList.FirstOrDefault(o => (o.VertexId1 == allVertices[lastCity].Id && o.VertexId2 == allVertices[nextCity].Id) || (o.VertexId1 == allVertices[nextCity].Id && o.VertexId2 == allVertices[lastCity].Id));
                var path = GraphUtils.Instance.ShortestPathAlgorithm(GraphUtils.Instance.Graph.Vertices.FirstOrDefault(o => o.ID == allVertices[lastCity].Id), GraphUtils.Instance.Graph.Vertices.FirstOrDefault(o => o.ID == allVertices[nextCity].Id));
                if (path != null)
                {
                    polylineList.AddRange(path);
                }
                else
                {
                    log.Error("Path not found LastCity ID {0} NextCity ID {1}", lastCity, nextCity);
                }
                i++;
                ////Define next and last city
                //orderedTour2.Add(new Tuple<int, int>(lastCity2, nextCity2));
                //ShortestPath path2 = GraphUtils.Instance.ShortestPathList.FirstOrDefault(o => (o.VertexId1 == allVertices[lastCity2].Id && o.VertexId2 == allVertices[nextCity2].Id) || (o.VertexId1 == allVertices[nextCity2].Id && o.VertexId2 == allVertices[lastCity2].Id));
                //if (path2 != null)
                //{
                //    polylineList2.AddRange(path2.ShortestPathEdges);
                //}
                //if (lastCity2 != tspEventArgs.BestTour[nextCity2].Connection1)
                //{
                //    lastCity2 = nextCity2;
                //    nextCity2 = tspEventArgs.BestTour[nextCity2].Connection1;
                //}
                //else
                //{
                //    lastCity2 = nextCity2;
                //    nextCity2 = tspEventArgs.BestTour[nextCity2].Connection2;
                //}
            }


            //foreach (var connection in tspEventArgs.BestTour)
            //{
            //    var city1 = tspEventArgs.TSPVertexList[connection.Connection1];
            //    var city2 = tspEventArgs.TSPVertexList[connection.Connection2];

            //}
            return polylineList;
        }

        internal List<Tuple<int, int>> GetTourOnOrder(Tour bestTour)
        {
            var result = new List<Tuple<int, int>>();

            int lastCity = 0;
            int nextCity = bestTour[0].Connection1;
            result.Add(new Tuple<int, int>(lastCity, nextCity));

            while (result.Count < bestTour.Count)
            {
                if (lastCity != bestTour[nextCity].Connection1)
                {
                    lastCity = nextCity;
                    nextCity = bestTour[nextCity].Connection1;
                }
                else
                {
                    lastCity = nextCity;
                    nextCity = bestTour[nextCity].Connection2;
                }
                result.Add(new Tuple<int, int>(lastCity, nextCity));
            }

            return result;

        }


        public MapPoint FindFarmostPointInsideListOfPoint(MapPoint startingVertex, List<MapPoint> mapPointList)
        {
            var maxDistance = 0.0;
            MapPoint result = null;
            foreach (var mappoint in mapPointList)
            {
                if (mappoint != null)
                {
                    var distance = GeometryEngine.Distance(startingVertex, mappoint);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        result = mappoint;
                    }
                }
            }
            return result;
        }

        public Color GetRandomColor()
        {
            Random randonGen = new Random();
            var randomColor =
                Color.FromArgb(
                    255,
                    (byte)randonGen.Next(255),
                    (byte)randonGen.Next(255),
                    (byte)randonGen.Next(255));
            return randomColor;
        }

        public Geometry ShowGeneralizedRoutes(IEnumerable<Graphic> graphics, bool generalize, Color color = default(Color), bool smoothen = false, int id = 0)
        {
            var symbol = new SimpleLineSymbol
            {
                Color = color,
                Style = SimpleLineStyle.Solid,
                Width = 5
            };

            var geometries = graphics.Where(o => o.Geometry != null).Select(g => g.Geometry);
            var geometry =  GeometryEngine.Union(geometries).ToSr3067();
            if (generalize)
            {
                geometry = GeometryEngine.Generalize(geometry, 20, false);
            }
            if (smoothen)
            {
                geometry = SmoothUtils.Instance.SmoothPolyline((Polyline)geometry);
            }

            var graphic = new Graphic(geometry, new List<KeyValuePair<string, object>>() { new KeyValuePair<string, object>("ID", id)}, symbol) { IsVisible = true };

            ResultGraphics.Graphics.Add(graphic);
            return geometry;

        }

        public void DrawEdgeBetweenIdPair(Tuple<int, int> idPair, TSPVertices tspVertexList)
        {
            ResultGraphics.Graphics.Clear();
            if (idPair == null)
            {
                return;
            }
            var tspVertex1 = idPair.Item1;
            var tspVertex2 = idPair.Item2;

            var symbol = new SimpleLineSymbol
            {
                Color = Colors.DeepPink,
                Style = SimpleLineStyle.Solid,
                Width = 5
            };
            GraphUtils.ShortestPath path = GraphUtils.Instance.ShortestPathList.FirstOrDefault(o => (o.VertexId1 == tspVertexList[tspVertex1].Id && o.VertexId2 == tspVertexList[tspVertex2].Id) || (o.VertexId1 == tspVertexList[tspVertex2].Id && o.VertexId2 == tspVertexList[tspVertex1].Id));
            if (path != null)
            {
                var idCollection = path.ShortestPathEdges.Select(o => o.Id);
                var geometryList = (from id in idCollection select GraphicsLayer.Graphics.FirstOrDefault(o => Convert.ToInt32(o.Attributes["FID"]) == id) into graphic where graphic != null select graphic.Geometry).ToList();
                ResultGraphics.Graphics.Add(new Graphic(GeometryEngine.Union(geometryList), symbol));
            }


        }

        public List<Geometry> GetGeometriesWithIdsFromGraphicLayer(IEnumerable<int> ids)
        {
            var result = new List<Geometry>();
            foreach (var id in ids)
            {
                var graphic = GraphicsLayer.Graphics.FirstOrDefault(o => Convert.ToInt32(o.Attributes["FID"]) == id);
                if (graphic != null)
                {
                    result.Add(graphic.Geometry);
                }
            }
            return result;
        }

        public void ShowSmoothened(IEnumerable<Graphic> graphics, bool resetGraphics = false)
        {
            var symbol = new SimpleLineSymbol
            {
                Color = GetRandomColor(),
                Style = SimpleLineStyle.Solid,
                Width = 5
            };
            if (resetGraphics)
            {
                ResultGraphics.Graphics.Clear();
            }
            if (!graphics.Any())
            {
                return;
            }

            var geometries =  graphics.Where(o => o.Geometry != null).Select(g => g.Geometry);
            var geometry = GeometryEngine.Project(GeometryEngine.Union(geometries), new SpatialReference(3067));
            if (geometry.GeometryType == GeometryType.Polyline)
            {
                var geometryList = SmoothUtils.Instance.SmoothPolyline((Polyline)geometry);

                symbol.Color = GetRandomColor();
                ResultGraphics.Graphics.Add(new Graphic(geometryList, symbol));
            }


        }

        public async Task LoadArcGisLocalTiledLayerAsync(string path, string displayname, bool isVisible = true)
        {
            var localtiledlayer = new ArcGISLocalTiledLayer(path)
            {
                DisplayName = displayname,
                IsVisible = isVisible,
                ID = displayname
            };
            await localtiledlayer.InitializeAsync();
            Map.Layers.Add(localtiledlayer);
        }

        public async Task LoadWmsLayerAsync(string uri, string displayname, string tablename, bool isVisible = true)
        {
            var wmslayer = new WmsLayer(new Uri(uri))
            {
                DisplayName = displayname,
                IsVisible = isVisible,
            };
            if (!string.IsNullOrEmpty(tablename))
            {
                wmslayer.Layers = new[] { tablename };
            }
            wmslayer.MinScale = 0;
            wmslayer.MaxScale = 0;
            wmslayer.ImageFormat = "image/png";
            await wmslayer.InitializeAsync();
            Map.Layers.Add(wmslayer);
        }

        public async Task LoadKuvioRajatFeatureTableAsync(string kuviorajatPath)
        {
            if (File.Exists(kuviorajatPath))
            {
                var table = await ShapefileTable.OpenAsync(kuviorajatPath);
                KuvioRajatTable = await table.QueryAsync(new QueryFilter() { WhereClause = "1=1" });
            }
        }


        public List<Graphic> GetGraphicsFromGraphEdges(List<GraphEdgeClass> graphEdges)
        {
            var ids = graphEdges.Select(o => o.Id);
            var result = new List<Graphic>();
            foreach (var id in ids)
            {
                var graphic = GraphicsLayer.Graphics.FirstOrDefault(o => Convert.ToInt32(o.Attributes["FID"]) == id);
                if (graphic != null)
                {
                    result.Add(graphic);
                }
            }
            return result;
        }

        public Tuple<double, double> DrawRouteFromOrderList(long[] orderList, GraphVertexClass[] vertices, bool useShortestPaths = false, Color color = default(Color))
        {
            //DrawRoutesToStartLocation(orderList, vertices, useShortestPaths, startVertice);
            var matkaTyhjana = 0.0;
            var matkaTaytena = 0.0;

            for (int index = 0; index < orderList.Length - 1; ++index)
            {
                var startOrder = orderList[index];
                var startIndex = vertices[startOrder].ID;


                var endOrder = orderList[index + 1];
                var endPoint = vertices[endOrder].ID;
                DrawRouteEndpoints(vertices[startOrder], vertices[endOrder], index, color);
                if (startIndex != 0 && endPoint != 0 && startIndex != endPoint)
                {
                    IEnumerable<GraphEdgeClass> polylineList = null;

                    if (useShortestPaths)
                    {
                        try
                        {
                            polylineList = FindShortestPathFromSavedPaths(startIndex, endPoint);
                        }
                        catch (Exception ex)
                        {
                            log.Error("Cannot find route {0} -> {1}: {2}", endPoint, startIndex, ex.Message);
                        }
                    }
                    else
                    {
                        polylineList = GetGraphEdgeClassesFromIds(startIndex, endPoint);
                    }
                    if (polylineList == null)
                    {
                        polylineList = GetGraphEdgeClassesFromIds(startIndex, endPoint);
                        if (polylineList == null)
                        {
                            log.Error("Cannot find route {0} -> {1}", endPoint, startIndex);
                            continue;
                        }
                    }

                    var result = HighlightEdges(polylineList, false);
                    if (index == 0)
                    {
                        matkaTyhjana = Math.Abs(GeometryEngine.Length(GeometryEngine.Union(result.Select(o => o.Geometry))));
                    }
                    else if (index == orderList.Length - 2)
                    {
                        matkaTaytena = Math.Abs(GeometryEngine.Length(GeometryEngine.Union(result.Select(o => o.Geometry))));
                    }
                }

            }
            return new Tuple<double, double>(matkaTyhjana, matkaTaytena);
        }

        private void DrawRoutesToStartLocation(long[] orderList, GraphVertexClass[] vertices, bool useShortestPaths, GraphVertexClass startVertice)
        {
            if (startVertice != null)
            {
                if (useShortestPaths)
                {
                    if (startVertice.ID != vertices[orderList[0]].ID)
                    {
                        var polylineList = FindShortestPathFromSavedPaths(startVertice.ID, vertices[orderList[0]].ID);
                        HighlightEdges(polylineList, false);
                        polylineList = FindShortestPathFromSavedPaths(startVertice.ID, vertices[orderList[orderList.Length - 1]].ID);
                        HighlightEdges(polylineList, false);
                    }
                }
            }
        }

        private IEnumerable<GraphEdgeClass> FindShortestPathFromSavedPaths(int startIndex, int endPoint)
        {
            IEnumerable<GraphEdgeClass> polylineList = null;
            foreach (var o in GraphUtils.Instance.ShortestPathList)
            {
                if (o == null)
                {
                    //log.Error("Error finding path");
                    continue;
                }
                if ((o.VertexId1 == startIndex && o.VertexId2 == endPoint) || (o.VertexId1 == startIndex && o.VertexId2 == endPoint))
                {
                    polylineList = o.ShortestPathEdges;

                    break;
                }
            }
            return polylineList;
        }

        private void DrawRouteEndpoints(GraphVertexClass startVertex, GraphVertexClass endVertex, int index, Color color)
        {
            var symbol = new SimpleMarkerSymbol()
            {
                Size = 11,
                Style = SimpleMarkerStyle.Diamond,
                Color = color,
                Outline = new SimpleLineSymbol() { Style = SimpleLineStyle.Solid, Color = Colors.Black, Width = 0.1 }

            };

            TspVerticesLayer.Graphics.Add(new Graphic(new MapPoint((double)startVertex.X, (double)startVertex.Y, new SpatialReference(3067)), new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>("ID", string.Empty) }, symbol));
            TspVerticesLayer.Graphics.Add(new Graphic(new MapPoint((double)endVertex.X, (double)endVertex.Y, new SpatialReference(3067)), new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>("ID", index) }, symbol));
        }

        private IEnumerable<GraphEdgeClass> GetGraphEdgeClassesFromIds(long startIndex, long endPoint)
        {
            var edges = new List<GraphEdgeClass>();
            var path = GraphUtils.Instance.ShortestPathAlgorithm(GraphUtils.Instance.Graph.Vertices.FirstOrDefault(o => o.ID == startIndex), GraphUtils.Instance.Graph.Vertices.FirstOrDefault(o => o.ID == endPoint));
            if (path != null)
            {
                edges.AddRange(path);
            }
            else
            {
                log.Debug("Cannot find vertice");
            }
            return edges;

        }

        public async Task LineTracingAsync()
        {

            var displayname = Path.GetFileName(ConfigurationManager.AppSettings["linedata"]);

            var point = await MapView.Editor.RequestPointAsync();
            var bufferedPoint = GeometryEngine.Buffer(point, 5);
            var lineLayer = Map.Layers.FirstOrDefault(o => o.DisplayName == displayname) as GraphicsLayer;

            var symbol = new SimpleLineSymbol
            {
                Width = 3,
                Color = Colors.OrangeRed,
                Style = SimpleLineStyle.Solid
            };
            if (lineLayer == null)
            {
                return;
            }
            try
            {
                foreach (var graphic in lineLayer.Graphics)
                {
                    graphic.IsVisible = false;
                }

                var lines = lineLayer.Graphics.Where(o => GeometryEngine.Intersects(o.Geometry, bufferedPoint));
                foreach (var line in lines)
                {
                    line.IsVisible = true;
                    var polyline = line.Geometry as Polyline;
                    if (polyline == null) return;
                    foreach (var part in polyline.Parts)
                    {
                        foreach (var lineSegment in part)
                        {
                            ResultGraphics.Graphics.Clear();

                            var geo = new PolylineBuilder(new List<IEnumerable<Segment>>(), Map.SpatialReference);
                            geo.AddPart(new List<Segment>() { lineSegment });
                            var graphic = new Graphic(geo.ToGeometry(), symbol) { IsSelected = true };
                            ResultGraphics.Graphics.Add(graphic);


                            var vm = new NotificationViewModel();
                            vm.ShowModalessDialog(vm, typeof(NotificationView));
                            while (vm.NotifyViewModelResult == NotificationViewModel.NotifyResult.Wait)
                            {
                                await Task.Delay(200);
                            }

                            if (vm.NotifyViewModelResult == NotificationViewModel.NotifyResult.NextLine)
                            {
                                break;
                            }
                            else if (vm.NotifyViewModelResult == NotificationViewModel.NotifyResult.Close)
                            {
                                return;
                            }
                        }
                    }
                    line.IsSelected = false;
                    line.IsVisible = false;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);

            }
            finally
            {
                foreach (var graphic in lineLayer.Graphics)
                {
                    graphic.IsVisible = true;
                    graphic.IsSelected = false;
                }
            }



        }

        public async Task GetRoutesAsync()
        {

            var displayname = Path.GetFileName(ConfigurationManager.AppSettings["linedata"]);
            ResultGraphics.Graphics.Clear();
            var geometry = await MapView.Editor.RequestShapeAsync(DrawShape.Polygon);
            var lineLayer = Map.Layers.FirstOrDefault(o => o.DisplayName == displayname) as GraphicsLayer;
            if (lineLayer == null) return;

            var symbol = new SimpleLineSymbol
            {
                Width = 3,
                Color = Colors.DarkTurquoise,
                Style = SimpleLineStyle.Solid
            };



            var lines = lineLayer.Graphics.Where(o => GeometryEngine.Intersects(o.Geometry, geometry));

            foreach (var line in lines)
            {

                var polyline = line.Geometry as Polyline;
                if (polyline == null) return;
                foreach (var part in polyline.Parts)
                {
                    var geo = new PolylineBuilder(new List<IEnumerable<Segment>>(), Map.SpatialReference);
                    foreach (var lineSegment in part)
                    {
                        if (!GeometryEngine.Intersects(lineSegment.EndPoint, geometry))
                        {
                            geo.AddPart(new List<Segment>() { lineSegment });
                        }
                        else
                        {
                            if (geo.Parts.Count > 3)
                            {
                                var graphic = new Graphic(geo.ToGeometry(), symbol) { IsVisible = false };
                                ResultGraphics.Graphics.Add(graphic);
                                geo = new PolylineBuilder(new List<IEnumerable<Segment>>(), Map.SpatialReference);
                            }
                            else
                            {
                                geo = new PolylineBuilder(new List<IEnumerable<Segment>>(), Map.SpatialReference);
                            }
                        }
                    }
                    if (geo.Parts.Count > 3)
                    {
                        var graphic = new Graphic(geo.ToGeometry(), symbol) { IsVisible = false };
                        ResultGraphics.Graphics.Add(graphic);
                    }
                }
            }
        }

        public async Task LoadRuntimeContentLayerAsync(string tpkPath4, string getFileName)
        {
            var geodaabase = await Geodatabase.OpenAsync(tpkPath4);
            foreach (var table in geodaabase.FeatureTables)
            {
                var fl = new FeatureLayer
                {
                    DisplayName = table.Name,
                    FeatureTable = table
                };
                await fl.InitializeAsync();
                Map.Layers.Add(fl);
            }
        }

        public void AddOptRunToResultGraphics(OptimizationRunModel optRun)
        {
            var color = GetRandomColor();
            while (ResultGraphics.Graphics.Any(o => ((SimpleLineSymbol) o.Symbol).Color.Equals(color)))
            {
                color = GetRandomColor();
            }
            var symbol = new SimpleLineSymbol
            {
                Color = color,
                Style = SimpleLineStyle.Solid,
                Width = 5
            };
            var geometry = Geometry.FromJson(optRun.GeometryJson);
            var graphic = new Graphic(geometry, new List<KeyValuePair<string, object>>() { new KeyValuePair<string, object>("ID", optRun.ToString()) }, symbol) { IsVisible = true };
            ResultGraphics.Graphics.Add(graphic);
        }
    }
}
