﻿using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Bicikelj.Model;
using System;
using System.Device.Location;

namespace Bicikelj.Controls
{
    public partial class PositionIndicator : UserControl, INotifyPropertyChanged
    {
        private const double EARTH_RADIUS_METERS = 6378137;

        public PositionIndicator()
        {
            // Required to initialize variables
            InitializeComponent();
            this.AccuracyCircle.DataContext = this;
        }


        public double? FixedHeading
        {
            get { return (double?)GetValue(FixedHeadingProperty); }
            set { SetValue(FixedHeadingProperty, value); }
        }

        public static readonly DependencyProperty FixedHeadingProperty =
            DependencyProperty.Register("FixedHeading", typeof(double?), typeof(PositionIndicator), new PropertyMetadata(null, FixedHeadingChanged));

        private static void FixedHeadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var indicator = d as PositionIndicator;
            if (indicator == null)
                return;

            var oldHeading = (double?)e.OldValue;
            var newHeading = (double?)e.NewValue;
            if (newHeading.HasValue)
                SetNewHeading(indicator, oldHeading.GetValueOrDefault(), newHeading.Value);
            else
                SetNewHeading(indicator, oldHeading.GetValueOrDefault(), indicator.Heading);
        }

        public double Heading
        {
            get { return (double)GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }
        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register("Heading", typeof(double), typeof(PositionIndicator), new PropertyMetadata(0d, HeadingChanged));

        private static void HeadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var indicator = d as PositionIndicator;
            if (indicator == null || indicator.FixedHeading != null)
                return;

            var oldHeading = (double)e.OldValue;
            var newHeading = (double)e.NewValue;
            SetNewHeading(indicator, oldHeading, newHeading);
        }

        private static double SetNewHeading(PositionIndicator indicator, double oldHeading, double newHeading)
        {
            var delta = newHeading - oldHeading;
            if (Math.Abs(delta) > 180)
                oldHeading += delta < 0 ? -360 : 360;

            indicator.AnimateHeadingAnimation.From = oldHeading;
            indicator.AnimateHeadingAnimation.To = newHeading;
            if (indicator.AnimateHeadingStoryboard.GetCurrentState() == ClockState.Stopped)
                indicator.AnimateHeadingStoryboard.Stop();
            indicator.AnimateHeadingStoryboard.Begin();
            return oldHeading;
        }

        public double HeadingAccuracy
        {
            get { return (double)GetValue(HeadingAccuracyProperty); }
            set { SetValue(HeadingAccuracyProperty, value); }
        }
        public static readonly DependencyProperty HeadingAccuracyProperty =
            DependencyProperty.Register("HeadingAccuracy", typeof(double), typeof(PositionIndicator), new PropertyMetadata(0d, HeadingAccuracyChanged));

        private static void HeadingAccuracyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var indicator = d as PositionIndicator;
            if (indicator == null)
                return;
            indicator.CheckHeadingVisibility();
        }


        public double ZoomLevel
        {
            get { return (double)GetValue(ZoomLevelProperty); }
            set { SetValue(ZoomLevelProperty, value); }
        }
        public static readonly DependencyProperty ZoomLevelProperty =
            DependencyProperty.Register("ZoomLevel", typeof(double), typeof(PositionIndicator), new PropertyMetadata(14d, ZoomLevelChanged));

        private static void ZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var indicator = d as PositionIndicator;
            if (indicator == null)
                return;
            indicator.UpdateAccuracyCircle();
        }


        public ICompassProvider CompassProvider
        {
            get { return (ICompassProvider)GetValue(CompassProviderProperty); }
            set
            {
                if (CompassProvider != null)
                    CompassProvider.HeadingChanged -= HandleCompass;
                SetValue(CompassProviderProperty, value);
                if (CompassProvider != null)
                    CompassProvider.HeadingChanged += HandleCompass;
                CheckHeadingVisibility();
            }
        }
        public static readonly DependencyProperty CompassProviderProperty =
            DependencyProperty.Register("CompassProvider", typeof(ICompassProvider), typeof(PositionIndicator), new PropertyMetadata(null));

        
        private void HandleCompass(object sender, HeadingAndAccuracy haa)
        {
            Heading = haa.Heading;
            HeadingAccuracy = haa.Accuracy;
        }


        public bool IsHeadingVisible
        {
            get { return (bool)GetValue(IsHeadingVisibleProperty); }
            set { SetValue(IsHeadingVisibleProperty, value); }
        }
        public static readonly DependencyProperty IsHeadingVisibleProperty =
            DependencyProperty.Register("IsHeadingVisible", typeof(bool), typeof(PositionIndicator), new PropertyMetadata(true, IsHeadingVisibleChanged));

        private static void IsHeadingVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var indicator = d as PositionIndicator;
            if (indicator == null)
                return;

            indicator.CheckHeadingVisibility();
        }


        public void CheckHeadingVisibility()
        {
            if (!CompassProvider.IsSupported || !IsHeadingVisible)
                VisualStateManager.GoToState(this, "IsUnavailable", true);
            else if (IsHeadingAccurate)
                VisualStateManager.GoToState(this, "IsAccurate", true);
            else
                VisualStateManager.GoToState(this, "IsInaccurate", true);
        }

        public bool IsAccuracyVisible
        {
            get { return (bool)GetValue(IsAccuracyVisibleProperty); }
            set { SetValue(IsAccuracyVisibleProperty, value); }
        }
        public static readonly DependencyProperty IsAccuracyVisibleProperty =
            DependencyProperty.Register("IsAccuracyVisible", typeof(bool), typeof(PositionIndicator), new PropertyMetadata(true, IsAccuracyVisibleChanged));

        private static void IsAccuracyVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var indicator = d as PositionIndicator;
            if (indicator == null)
                return;
        }


        public bool IsHeadingAccurate
        {
            get { return HeadingAccuracy <= 20; }
        }

        private void ShowCalibration(object sender, GestureEventArgs e)
        {
            //CalibrationView.Visibility = Visibility.Visible;
            //HeadingView.Visibility = Visibility.Collapsed;
        }

        private void HideCalibration(object sender, RoutedEventArgs e)
        {
            //CalibrationView.Visibility = Visibility.Collapsed;
            //HeadingView.Visibility = Visibility.Visible;
        }



        public GeoCoordinate Coordinate
        {
            get { return (GeoCoordinate)GetValue(CoordinateProperty); }
            set { SetValue(CoordinateProperty, value); }
        }
        public static readonly DependencyProperty CoordinateProperty =
            DependencyProperty.Register("Coordinate", typeof(GeoCoordinate), typeof(PositionIndicator), new PropertyMetadata(null, GeoCoordinateChanged));

        private static void GeoCoordinateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var indicator = d as PositionIndicator;
            if (d == null)
                return;
            indicator.UpdateAccuracyCircle();
        }



        public double InternalAccuracyRadius
        {
            get { return (double)GetValue(InternalAccuracyRadiusProperty); }
            set { SetValue(InternalAccuracyRadiusProperty, value); }
        }
        public static readonly DependencyProperty InternalAccuracyRadiusProperty =
            DependencyProperty.Register("InternalAccuracyRadius", typeof(double), typeof(PositionIndicator), new PropertyMetadata(0d));

        private void UpdateAccuracyCircle()
        {
            if (Coordinate != null)
            {
                GeoCoordinate center = Coordinate;
                double zoomLevel = ZoomLevel;
                
                //Calculate the ground resolution in meters/pixel
                //Math based on http://msdn.microsoft.com/en-us/library/bb259689.aspx
                double groundResolution = Math.Cos(center.Latitude * Math.PI / 180) *
                    2 * Math.PI * EARTH_RADIUS_METERS / (256 * Math.Pow(2, zoomLevel));

                //Update the accuracy circle dimensions
                var hAcc = double.IsNaN(Coordinate.HorizontalAccuracy) ? 500 : Coordinate.HorizontalAccuracy;
                InternalAccuracyRadius = hAcc / groundResolution;
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyOfPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}