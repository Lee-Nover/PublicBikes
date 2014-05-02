using Bicikelj.ViewModels;
using System;
using System.Windows;
using System.Windows.Data;

namespace Bicikelj.Converters
{
    public class StationStyleConverter : IValueConverter
    {

        public Style SingleStyle { get; set; }
        public Style ClusterStyle { get; set; }

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var vm = value as ClusteredStationViewModel;
            if (vm != null && vm.IsClustered)
                return ClusterStyle;
            else
                return SingleStyle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}