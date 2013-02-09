using System;
using System.Windows.Data;
using Bicikelj.Model;

namespace Bicikelj.Converters
{
	public class FavoriteTypeToIconConverter : IValueConverter
	{

		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return FavoriteTypeToIconConverter.GetIcon(value);
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
			if (targetType == typeof(Uri))
				return FavoriteToIconUriConverter.GetIconUri(value, parameter);
			else
				return FavoriteToIconUriConverter.GetIconUriStr(value, parameter);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public static Uri GetIconUri(object value, object parameter)
		{
			var result = new Uri(GetIconUriStr(value, parameter), UriKind.RelativeOrAbsolute);
			return result;
		}

		public static string GetIconUriStr(object value, object parameter)
		{
			string result = null;
			if (value is bool)
			{
				bool op = parameter is bool ? (bool)parameter : false;
				if (parameter is string)
					bool.TryParse(parameter as string, out op);
				if ((bool)value ^ op)
					result = "/Images/appbar.star.add.png";
				else
					result = "/Images/appbar.star.png";
			}
			return result;
		}

		#endregion
	}
}