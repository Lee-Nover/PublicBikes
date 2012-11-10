using System;
using System.Windows;
using System.Windows.Data;
using System.Collections;
using Bicikelj.Model;

namespace Bicikelj.Converters
{
	public class PinTypeToIconConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return PinTypeToIconConverter.GetPathData(value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public static object GetPathData(object value)
		{
			object result = null;
			if (value is PinType)
			{
				switch ((PinType)value)
				{
					case PinType.CurrentPosition:
						result = App.CurrentApp.Resources["CurrentPositionIconData"];
						break;
					case PinType.BikeStand:
						result = App.CurrentApp.Resources["CyclingIconData"];
						break;
					case PinType.Cycling:
						result = App.CurrentApp.Resources["CyclingIconData"];
						break;
					case PinType.Walking:
						result = App.CurrentApp.Resources["WalkIconData"];
						break;
					case PinType.Finish:
						result = App.CurrentApp.Resources["RaceFlagIconData"];
						break;
					default:
						break;
				}
			}
			return result;
		}

		#endregion
	}
}