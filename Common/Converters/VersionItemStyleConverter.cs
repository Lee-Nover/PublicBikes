using Bicikelj.ViewModels;
using System;
using System.Windows;
using System.Windows.Data;

namespace Bicikelj.Converters
{
    public class VersionItemStyleConverter : IValueConverter
    {

        public Style VersionStyle { get; set; }
        public Style ChangeStyle { get; set; }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var vm = value as VersionItemViewModel;
            if (vm != null && vm.Version != null)
                return VersionStyle;
            else
                return ChangeStyle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}