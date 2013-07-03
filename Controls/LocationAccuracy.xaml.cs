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
using Microsoft.Phone.Controls.Maps;

namespace Bicikelj.Controls
{
    public partial class LocationAccuracy : UserControl
    {
        private Map map;
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

        private void InitMapControl()
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is Map))
                parent = VisualTreeHelper.GetParent(parent);
            map = parent as Map;
            if (map == null)
                throw new Exception("LocationAccuray must be placed inside a Map control");

            map.ViewChangeEnd += map_ViewChangeEnd;
        }

        void map_ViewChangeEnd(object sender, MapEventArgs e)
        {
            UpdateAccuracyCircle();
        }

        public GeoCoordinate Coordinate
        {
            get { return (GeoCoordinate)GetValue(CoordinateProperty); }
            set {
                SetValue(CoordinateProperty, value);
                UpdateAccuracyCircle();
            }
        }

        // Using a DependencyProperty as the backing store for Coordinate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CoordinateProperty =
            DependencyProperty.Register("Coordinate", typeof(GeoCoordinate), typeof(LocationAccuracy), new PropertyMetadata(null));


        private void UpdateAccuracyCircle()
        {
            if (map != null && Coordinate != null)
            {
                //Calculate the ground resolution in meters/pixel
                //Math based on http://msdn.microsoft.com/en-us/library/bb259689.aspx
                double groundResolution = Math.Cos(map.Center.Latitude * Math.PI / 180) *
                    2 * Math.PI * EARTH_RADIUS_METERS / (256 * Math.Pow(2, map.ZoomLevel));

                //Update the accuracy circle dimensions
                var hAcc = double.IsNaN(Coordinate.HorizontalAccuracy) ? 500 : Coordinate.HorizontalAccuracy;
                var vAcc = double.IsNaN(Coordinate.VerticalAccuracy) ? hAcc : Coordinate.VerticalAccuracy;
                AccuracyCircle.Width = hAcc / groundResolution;
                AccuracyCircle.Height = vAcc / groundResolution;
                MapLayer.SetPosition(this, Coordinate);
                //Use the margin property to center the accuracy circle
                AccuracyCircle.Margin = new Thickness(-AccuracyCircle.Width / 2, -AccuracyCircle.Height / 2, 0, 0);
            }
        }
    }
}
