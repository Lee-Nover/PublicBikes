using System;
using System.Windows;
using System.Windows.Data;
using System.Collections;
using Bicikelj.Model;

namespace Bicikelj.Converters
{
	public class FavoriteToIconConverter : IValueConverter
	{

		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return FavoriteToIconConverter.GetIcon(value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public static object GetIcon(object value)
		{
			object result = null;
			if (value is FavoriteType)
			{
				switch ((FavoriteType)value)
				{
					case FavoriteType.Station:
						result = App.CurrentApp.Resources["CyclingIconData"];
						break;
					case FavoriteType.Coordinate:
						result = App.CurrentApp.Resources["MapLocationIconData"];
						break;
					case FavoriteType.Name:
						result = App.CurrentApp.Resources["MapLocationIconData"];
						break;
					default:
						break;
				}
			}
			return result;
		}

		#endregion
	}

	public class FavoriteToIconUriConverter : IValueConverter
	{

		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return FavoriteToIconUriConverter.GetIconUri(value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public static Uri GetIconUri(object value)
		{
			Uri result = null;
			if (value is FavoriteType)
			{
				switch ((FavoriteType)value)
				{
					case FavoriteType.Station:
						result = new Uri("/Images/Cycle racer 24.png", UriKind.Relative);
						break;
					case FavoriteType.Coordinate:
						result = new Uri("/Images/Map and location 24.png", UriKind.Relative);
						break;
					case FavoriteType.Name:
						result = new Uri("/Images/Map and location 24.png", UriKind.Relative);
						break;
					default:
						break;
				}
			}
			return result;
		}

		public static string GetIconUriStr(object value)
		{
			string result = null;
			if (value is FavoriteType)
			{
				switch ((FavoriteType)value)
				{
					case FavoriteType.Station:
						result = "/Images/Cycle racer 24.png";
						break;
					case FavoriteType.Coordinate:
						result = "/Images/Map and location 24.png";
						break;
					case FavoriteType.Name:
						result = "/Images/Map and location 24.png";
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