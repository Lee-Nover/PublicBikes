using System;
using System.Windows.Data;
using Bicikelj.Model;
using System.Windows;

namespace Bicikelj.Converters
{
    public class TravelSpeedConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object result = DependencyProperty.UnsetValue;

            if ((targetType == typeof(int) || targetType == typeof(double)) && value is TravelSpeed)
                result = TravelSpeedToInt((TravelSpeed)value, parameter);
            else if (targetType == typeof(string))
                result = TravelSpeedToString((TravelSpeed)value);

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object result = DependencyProperty.UnsetValue;

            if (targetType == typeof(TravelSpeed) && (value is int) || (value is double))
                result = TravelSpeedFromDouble((double)value, parameter);

            return result;
        }

        public static int TravelSpeedToInt(TravelSpeed speed, object parameter)
        {
            int max = 2;
            if (parameter is double || parameter is int)
                max = (int)parameter;
            int mid = (int)Math.Round(max / 2d);
            return speed == TravelSpeed.Slow ? 0 : speed == TravelSpeed.Normal ? mid : max;
        }

        public static TravelSpeed TravelSpeedFromDouble(double speed, object parameter)
        {
            double max = 2;
            if (parameter is double || parameter is int)
                max = (double)parameter;
            speed = speed / max;

            if (speed <= 0.33)
                return TravelSpeed.Slow;
            else if (speed >= 0.66)
                return TravelSpeed.Fast;
            else
                return TravelSpeed.Normal;
        }

        public static string TravelSpeedToString(TravelSpeed speed)
        {
            return speed.ToString().ToLower();
        }

        #endregion
    }
}