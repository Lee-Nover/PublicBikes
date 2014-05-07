using Bicikelj.Model;
using Caliburn.Micro;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Controls.Maps;
using System.Windows.Controls;

namespace Bicikelj
{
    public class WP7Bootstrapper : WPBootstrapper
    {
        protected override void InitServices()
        {
            base.InitServices();
            var bingCred = App.Current.Resources["BingCredentials"];
            (bingCred as ApplicationIdCredentialsProvider).ApplicationId = BingMapsCredentials.Key;
        }

        protected override void AddCustomConventions()
        {
            base.AddCustomConventions();

            ConventionManager.AddElementConvention<LongListSelector>(LongListSelector.ItemsSourceProperty, "SelectedItem", "SelectionChanged").ApplyBinding =
                (viewModelType, path, property, element, convention) =>
                {
                    if (ConventionManager.GetElementConvention(typeof(Control)).ApplyBinding(viewModelType, path, property, element, convention))
                    {
                        ConventionManager.ConfigureSelectedItem(element, LongListSelector.SelectedItemProperty, viewModelType, path);
                        return true;
                    }
                    return false;
                };


            ConventionManager.AddElementConvention<MapItemsControl>(ItemsControl.ItemsSourceProperty, "DataContext", "Loaded");
            ConventionManager.AddElementConvention<Pushpin>(ContentControl.ContentProperty, "DataContext", "Tap");
            ConventionManager.AddElementConvention<Map>(Map.DataContextProperty, "DataContext", "Tap");
            ConventionManager.AddElementConvention<MapLayer>(MapLayer.DataContextProperty, "DataContext", "Tap");   
        }
    }
}
