using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Esri.ArcGISRuntime.Controls;
using Esri.ArcGISRuntime.Geometry;

namespace ArcGISRuntime.Samples.DesktopViewer.Services
{
    /// <summary>
    /// MapViewService based on Esri development summary
    /// </summary>
    public class MapViewService : INotifyPropertyChanged
    {
        private WeakReference<MapView> _mapView;

        public Task<bool> SetViewAsync(Geometry geometry)
        {
            if (MapView != null && geometry != null)
            {
                return MapView.SetViewAsync(geometry);
            }
            return Task.FromResult(false);
        }

        /// <summary>
        /// Returns MapPoint from given ScreenPoint.
        /// </summary>
        public MapPoint GetLocation(MouseEventArgs mouseEventArgs)
        {
            if (MapView != null)
            {
                var point = mouseEventArgs.GetPosition(MapView);
                return MapView.ScreenToLocation(point);
            }
            return null;
        }

        /// <summary>
        /// Gets <see cref="Esri.ArcGISRuntime.Geometry.SpatialReference"/> of the Map.
        /// </summary>
        public SpatialReference SpatialReference
        {
            get
            {
                if (MapView != null)
                {
                    return MapView.SpatialReference;
                }
                return null;
            }
        }

        private Cursor _currentCursor;
        public Cursor CurrentCursor
        {
            get { return _currentCursor; }
            set
            {
                _currentCursor = value;
                NotifyPropertyChanged();
            }

        }

        /// <summary>
        /// Gets <see>
        ///         <cref>Map.Extent</cref>
        ///     </see>
        ///     of the Map.
        /// </summary>
        public Envelope Extent
        {
            get
            {
                if (MapView != null)
                {
                    return MapView.Extent;
                }
                return null;
            }
        }

        public Map Map
        {
            get
            {
                if (MapView == null)
                    return null;

                return MapView.Map;
            }
        }

        public MapView MapView
        {
            get
            {
                MapView map;
                if (_mapView != null && _mapView.TryGetTarget(out map))
                    return map;
                return null;
            }
        }

        public static MapView GetMapView(DependencyObject obj)
        {
            return (MapView)obj.GetValue(MapViewProperty);
        }



        public static void SetMapView(DependencyObject obj, MapView value)
        {
            obj.SetValue(MapViewProperty, value);
        }

        public static readonly DependencyProperty MapViewProperty =
            DependencyProperty.RegisterAttached("MapView", typeof(MapViewService), typeof(MapViewService), new PropertyMetadata(null, OnMapViewPropertyChanged));

        private static void OnMapViewPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MapView && e.OldValue is MapViewService)
            {
                var controller = (e.OldValue as MapViewService);
                controller._mapView = null;
            }
            if (d is MapView && e.NewValue is MapViewService)
            {
                var controller = (e.NewValue as MapViewService);
                controller._mapView = new WeakReference<MapView>(d as MapView);

                var loadedListener
                    = new WeakEventListener<MapView, object, PropertyChangedEventArgs>(d as MapView)
                    {
                        OnEventAction =
                            (instance, source, eventArgs) =>
                                controller.MapViewController_PropertyChanged(source, eventArgs),
                        OnDetachAction = (instance, listener) =>
                        {
                            if (instance != null)
                                instance.PropertyChanged -= listener.OnEvent;
                        }
                    };

                // the instance passed to the action is referenced (i.e. instance.Loaded) so the lambda expression is 
                // compiled as a static method.  Otherwise it targets the map instance and holds it in memory.
                (d as MapView).PropertyChanged += loadedListener.OnEvent;
            }
        }

        private void MapViewController_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SpatialReference")
                NotifyPropertyChanged("SpatialReference");
            if (e.PropertyName == "Extent")
                NotifyPropertyChanged("Extent");


        }

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property. 
        // The CallerMemberName attribute that is applied to the optional propertyName 
        // parameter causes the property name of the caller to be substituted as an argument. 
        protected void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }



    }
}
