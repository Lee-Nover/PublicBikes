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
				result = TravelSpeedToInt((TravelSpeed)value);
			else if (targetType == typeof(string))
				result = TravelSpeedToString((TravelSpeed)value);

			return result;
		}

		public static int TravelSpeedToInt(TravelSpeed speed)
		{
			return speed == TravelSpeed.Slow ? 0 : speed == TravelSpeed.Normal ? 1 : 2;
		}

		public static TravelSpeed TravelSpeedFromDouble(double speed)
		{
			if (speed <= 0.66)
				return TravelSpeed.Slow;
			else if (speed >= 1.33)
				return TravelSpeed.Fast;
			else
				return TravelSpeed.Normal;
		}

		public static string TravelSpeedToString(TravelSpeed speed)
		{
			return speed.ToString().ToLower();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			object result = DependencyProperty.UnsetValue;

			if (targetType == typeof(TravelSpeed) && (value is int) || (value is double))
				result = TravelSpeedFromDouble((double)value);
			
			return result;
		}

		#endregion
	}
}