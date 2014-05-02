using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Bicikelj.Model;

namespace Bicikelj.Controls
{
    [TemplatePart(Name = "PART_Slider", Type = typeof(Slider))]
    public class TravelSpeedControl : ContentControl
    {
        public TravelType TravelType
        {
            get { return (TravelType)GetValue(TravelTypeProperty); }
            set { SetValue(TravelTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for TravelType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TravelTypeProperty =
            DependencyProperty.Register("TravelType", typeof(TravelType), typeof(TravelSpeedControl), new PropertyMetadata(TravelType.Walking));



        public TravelSpeed Speed
        {
            get { return (TravelSpeed)GetValue(SpeedProperty); }
            set { SetValue(SpeedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Speed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpeedProperty =
            DependencyProperty.Register("Speed", typeof(TravelSpeed), typeof(TravelSpeedControl), new PropertyMetadata(TravelSpeed.Normal));



        public bool ImperialUnits
        {
            get { return (bool)GetValue(ImperialUnitsProperty); }
            set { SetValue(ImperialUnitsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ImperialUnits.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImperialUnitsProperty =
            DependencyProperty.Register("ImperialUnits", typeof(bool), typeof(TravelSpeedControl), new PropertyMetadata(false));



        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(TravelSpeedControl), new PropertyMetadata(""));


        private Slider slider;
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            slider = GetTemplateChild("PART_Slider") as Slider;
            if (slider != null)
                slider.ValueChanged += (sender, e) => {
                    var pos = e.NewValue / slider.Maximum;
                    var nv = e.NewValue - slider.Minimum;
                    nv = nv / (slider.Maximum - slider.Minimum);
                    if (nv <= 0.33)
                        slider.Value = slider.Minimum;
                    else if (nv >= 0.66)
                        slider.Value = slider.Maximum;
                    else
                        slider.Value = Math.Round(slider.Minimum + (slider.Maximum - slider.Minimum) / 2);
                };
        }
    }
}