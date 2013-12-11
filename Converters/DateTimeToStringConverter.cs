using System;
using System.Windows.Data;

namespace Bicikelj.Converters
{
    public class DateTimeToStringConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return "";
            DateTime dt = (DateTime)value;
            string format = parameter as string;
            // don't use culture because it's always "en-US"
            if (!string.IsNullOrEmpty(format))
                return dt.ToString(format);
            else
                return dt.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}