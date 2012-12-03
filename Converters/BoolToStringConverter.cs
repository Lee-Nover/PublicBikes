using System;
using System.Windows.Data;
using Bicikelj.Model;

namespace Bicikelj.Converters
{
	public class BoolToStringConverter : IValueConverter
	{

		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return GetString(value, parameter);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		public static object GetString(object value, object parameter)
		{
			object result = null;
			if (value is bool)
			{
				var boolVal = (bool)value;
				var paramStr = parameter as string;
				if (!string.IsNullOrWhiteSpace(paramStr))
				{
					var boolStrs = paramStr.Split(';');
					if (boolStrs.Length > 1)
						result = boolStrs[boolVal ? 0 : 1];
				}
				if (result == null)
					result = boolVal.ToString();
			}
			return result;
		}

		#endregion
	}
}