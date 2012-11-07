using System;
using System.Windows;
using System.Windows.Data;
using System.Collections;

namespace Bicikelj.Converters
{
    public class ValueToVisibilityConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ValueToVisibilityConverter.GetVisibility(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static Visibility GetVisibility(object value)
        {
            if (value == null)
                return Visibility.Collapsed;
            else if (value is string)
                return (value as string).Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            else if (value is bool)
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            else if (value is int)
                return (int)value != 0 ? Visibility.Visible : Visibility.Collapsed;
			else if (value is DateTime)
                return (DateTime)value != DateTime.MinValue ? Visibility.Visible : Visibility.Collapsed;
            else if (value is IList)
                return (value as IList).Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            else if (value is ICollection)
                return (value as ICollection).Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            else
                return Visibility.Visible;
        }

        #endregion
    }

    public class ValueToVisibilityInvConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ValueToVisibilityInvConverter.GetVisibility(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static Visibility GetVisibility(object value)
        {
            if (value == null)
                return Visibility.Visible;
            else if (value is string)
                return (value as string).Length <= 0 ? Visibility.Visible : Visibility.Collapsed;
            else if (value is bool)
                return !(bool)value ? Visibility.Visible : Visibility.Collapsed;
            else if (value is int)
                return (int)value == 0 ? Visibility.Visible : Visibility.Collapsed;
            else if (value is IList)
                return (value as IList).Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            else if (value is ICollection)
                return (value as ICollection).Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            else
                return Visibility.Collapsed;
        }

        #endregion
    }
}
