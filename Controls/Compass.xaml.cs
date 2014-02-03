using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Bicikelj.Controls
{
    public partial class Compass : UserControl, INotifyPropertyChanged
    {
        public Compass()
        {
            // Required to initialize variables
            InitializeComponent();
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
            if (d == null)
                return;

            compass.Rotation.Angle = compass.Heading;
            compass.AnimateHeadingAnimation.To = compass.Heading;
            if (compass.AnimateHeadingStoryboard.GetCurrentState() == ClockState.Stopped)
                compass.AnimateHeadingStoryboard.Stop();
            compass.AnimateHeadingStoryboard.Begin();
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

            compass.HeadingAccuracyText.Text = compass.HeadingAccuracy.ToString("0.0") + "°";
            compass.NotifyOfPropertyChange("IsHeadingAccurate");
            if (compass.IsHeadingAccurate)
                VisualStateManager.GoToState(compass, "IsAccurate", true);
            else
                VisualStateManager.GoToState(compass, "IsInaccurate", true);
        }


        public Visibility CalibrationVisibility
        {
            get { return (Visibility)GetValue(CalibrationVisibilityProperty); }
            set { SetValue(CalibrationVisibilityProperty, value); }
        }
        public static readonly DependencyProperty CalibrationVisibilityProperty =
            DependencyProperty.Register("CalibrationVisibility", typeof(Visibility), typeof(Compass), new PropertyMetadata(Visibility.Collapsed));


        public bool IsHeadingAccurate
        {
            get { return HeadingAccuracy < 20; }
        }

        public void ShowCalibration(object sender, RoutedEventArgs e)
        {
            CalibrationVisibility = Visibility.Visible;
        }

        public void HideCalibration(object sender, RoutedEventArgs e)
        {
            CalibrationVisibility = Visibility.Collapsed;
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