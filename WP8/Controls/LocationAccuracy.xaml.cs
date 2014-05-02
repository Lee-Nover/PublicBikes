using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Device.Location;


namespace Bicikelj.Controls
{
    public partial class LocationAccuracy : UserControl
    {
#if WP7
        private Microsoft.Phone.Controls.Maps.Map map;
#else
        private Microsoft.Phone.Maps.Controls.Map map;
#endif

        private const double EARTH_RADIUS_METERS = 6378137;

        public LocationAccuracy()
        {
            InitializeComponent();
            this.Loaded += LocationAccuracy_Loaded;
        }

        void LocationAccuracy_Loaded(object sender, RoutedEventArgs e)
        {
            InitMapControl();
        }
#if WP7
        private void InitMapControl()
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is Microsoft.Phone.Controls.Maps.Map))
                parent = VisualTreeHelper.GetParent(parent);
            map = parent as Microsoft.Phone.Controls.Maps.Map;
            if (map == null)
                throw new Exception("LocationAccuracy must be placed inside a Map control");
            
            if (map != null)
            {
                map.ViewChanging += map_ViewChange;
                map.ViewChanged += map_ViewChange;
            }
        }

        void map_ViewChange(object sender, Microsoft.Phone.Maps.Controls.MapEventArgs e)
        {
            UpdateAccuracyCircle();
        }
#else
        private void InitMapControl()
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is Microsoft.Phone.Maps.Controls.Map))
                parent = VisualTreeHelper.GetParent(parent);
            map = parent as Microsoft.Phone.Maps.Controls.Map;
            if (map == null)
                throw new Exception("LocationAccuracy must be placed inside a Map control");
            
            if (map != null)
            {
                map.ViewChanging += map_ViewChange;
                map.ViewChanged += map_ViewChange;
                map.ResolveCompleted += map_ViewChange;
            }
        }

        void map_ViewChange(object sender, Microsoft.Phone.Maps.Controls.MapEventArgs e)
        {
            UpdateAccuracyCircle();
        }
#endif

        

        public GeoCoordinate Coordinate
        {
            get { return (GeoCoordinate)GetValue(CoordinateProperty); }
            set {
                SetValue(CoordinateProperty, value);
                UpdateAccuracyCircle();
            }
        }
        public static readonly DependencyProperty CoordinateProperty =
            DependencyProperty.Register("Coordinate", typeof(GeoCoordinate), typeof(LocationAccuracy), new PropertyMetadata(null));


        public bool CenterWithMargins
        {
            get { return (bool)GetValue(CenterWithMarginsProperty); }
            set {
                SetValue(CenterWithMarginsProperty, value);
                UpdateAccuracyCircle();
            }
        }
        public static readonly DependencyProperty CenterWithMarginsProperty =
            DependencyProperty.Register("CenterWithMargins", typeof(bool), typeof(LocationAccuracy), new PropertyMetadata(true));
        

        private void UpdateAccuracyCircle()
        {
            if ((map != null) && Coordinate != null)
            {
                GeoCoordinate center = null;
                double zoomLevel = 0;
                if (map != null)
                {
                    center = map.Center;
                    zoomLevel = map.ZoomLevel;
                }
                //Calculate the ground resolution in meters/pixel
                //Math based on http://msdn.microsoft.com/en-us/library/bb259689.aspx
                double groundResolution = Math.Cos(center.Latitude * Math.PI / 180) *
                    2 * Math.PI * EARTH_RADIUS_METERS / (256 * Math.Pow(2, zoomLevel));

                //Update the accuracy circle dimensions
                var hAcc = double.IsNaN(Coordinate.HorizontalAccuracy) ? 500 : Coordinate.HorizontalAccuracy;
                //var vAcc = double.IsNaN(Coordinate.VerticalAccuracy) ? hAcc : Coordinate.VerticalAccuracy;
                AccuracyCircle.Width = hAcc / groundResolution;
                AccuracyCircle.Height = hAcc / groundResolution;
                //MapLayer.SetPosition(this, Coordinate);
                //Use the margin property to center the accuracy circle
                if (CenterWithMargins)
                    AccuracyCircle.Margin = new Thickness(-AccuracyCircle.Width / 2, -AccuracyCircle.Height / 2, 0, 0);
                else
                    AccuracyCircle.Margin = new Thickness(0);
            }
        }
    }
}
