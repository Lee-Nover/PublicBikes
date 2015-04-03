using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Bicikelj.Model;
using System;

namespace Bicikelj.Controls
{
    public partial class Compass : UserControl, INotifyPropertyChanged
    {
        public Compass()
        {
            // Required to initialize variables
            InitializeComponent();
        }



        public double? FixedHeading
        {
            get { return (double?)GetValue(FixedHeadingProperty); }
            set { SetValue(FixedHeadingProperty, value); }
        }

        public static readonly DependencyProperty FixedHeadingProperty =
            DependencyProperty.Register("FixedHeading", typeof(double?), typeof(Compass), new PropertyMetadata(null, FixedHeadingChanged));

        private static void FixedHeadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var compass = d as Compass;
            if (compass == null)
                return;

            var oldHeading = (double?)e.OldValue;
            var newHeading = (double?)e.NewValue;
            if (newHeading.HasValue)
                SetNewHeading(compass, oldHeading.GetValueOrDefault(), newHeading.Value);
            else
                SetNewHeading(compass, oldHeading.GetValueOrDefault(), compass.Heading);
        }

        public double Heading
        {
            get { return (double)GetValue(HeadingProperty); }
            set { SetValue(HeadingProperty, value); }
        }
        public static readonly DependencyProperty HeadingProperty =
            DependencyProperty.Register("Heading", typeof(double), typeof(Compass), new PropertyMetadata(0d, HeadingChanged));

        private static void HeadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var compass = d as Compass;
            if (compass == null || compass.FixedHeading != null)
                return;
            
            var oldHeading = (double)e.OldValue;
            var newHeading = (double)e.NewValue;
            SetNewHeading(compass, oldHeading, newHeading);
        }

        private static double SetNewHeading(Compass compass, double oldHeading, double newHeading)
        {
            var delta = newHeading - oldHeading;
            if (Math.Abs(delta) > 180)
                oldHeading += delta < 0 ? -360 : 360;

            compass.AnimateHeadingAnimation.From = oldHeading;
            compass.AnimateHeadingAnimation.To = newHeading;
            if (compass.AnimateHeadingStoryboard.GetCurrentState() == ClockState.Stopped)
                compass.AnimateHeadingStoryboard.Stop();
            compass.AnimateHeadingStoryboard.Begin();
            return oldHeading;
        }

        public double HeadingAccuracy
        {
            get { return (double)GetValue(HeadingAccuracyProperty); }
            set { SetValue(HeadingAccuracyProperty, value); }
        }
        public static readonly DependencyProperty HeadingAccuracyProperty =
            DependencyProperty.Register("HeadingAccuracy", typeof(double), typeof(Compass), new PropertyMetadata(0d, HeadingAccuracyChanged));

        private static void HeadingAccuracyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var compass = d as Compass;
            if (d == null)
                return;

            compass.HeadingAccuracyText.Text = ((int)compass.HeadingAccuracy).ToString() + "°";
            compass.NotifyOfPropertyChange("IsHeadingAccurate");
            if (compass.IsHeadingAccurate)
                VisualStateManager.GoToState(compass, "IsAccurate", true);
            else
                VisualStateManager.GoToState(compass, "IsInaccurate", true);
        }


        public ICompassProvider CompassProvider
        {
            get { return (ICompassProvider)GetValue(CompassProviderProperty); }
            set {
                if (CompassProvider != null)
                    CompassProvider.HeadingChanged -= HandleCompass;
                SetValue(CompassProviderProperty, value);
                if (CompassProvider != null)
                    CompassProvider.HeadingChanged += HandleCompass;
                if (CompassProvider.IsSupported)
                    this.Visibility = Visibility.Visible;
                else
                    this.Visibility = Visibility.Collapsed;
            }
        }
        public static readonly DependencyProperty CompassProviderProperty =
            DependencyProperty.Register("CompassProvider", typeof(ICompassProvider), typeof(Compass), new PropertyMetadata(null));

        
        private void HandleCompass(object sender, HeadingAndAccuracy haa)
        {
            Heading = haa.Heading;
            HeadingAccuracy = haa.Accuracy;
        }

        public bool IsHeadingAccurate
        {
            get { return HeadingAccuracy <= 20; }
        }

        private void ShowCalibration(object sender, GestureEventArgs e)
        {
            CalibrationView.Visibility = Visibility.Visible;
            HeadingView.Visibility = Visibility.Collapsed;
        }

        private void HideCalibration(object sender, RoutedEventArgs e)
        {
            CalibrationView.Visibility = Visibility.Collapsed;
            HeadingView.Visibility = Visibility.Visible;
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