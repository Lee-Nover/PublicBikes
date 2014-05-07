using Bicikelj.Model;
using Caliburn.Micro;
using System.Windows.Controls;
using Microsoft.Phone.Maps.Toolkit;
using Microsoft.Phone.Maps.Controls;

namespace Bicikelj
{
    public class WP8Bootstrapper : WPBootstrapper
    {
        protected override void InitServices()
        {
            base.InitServices();
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = MapServiceCredentials.ApplicationID;
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = MapServiceCredentials.AuthenticationToken;
        }

        protected override void AddCustomConventions()
        {
            base.AddCustomConventions();
            

            ConventionManager.AddElementConvention<PublicBikes.Controls.LongListSelector>(PublicBikes.Controls.LongListSelector.ItemsSourceProperty, "SelectedItem", "SelectionChanged").ApplyBinding =
                (viewModelType, path, property, element, convention) =>
                {
                    if (ConventionManager.GetElementConvention(typeof(Control)).ApplyBinding(viewModelType, path, property, element, convention))
                    {
                        ConventionManager.ConfigureSelectedItem(element, PublicBikes.Controls.LongListSelector.SelectedItemProperty, viewModelType, path);
                        return true;
                    }
                    return false;
                };

            ConventionManager.AddElementConvention<MapItemsControl>(ItemsControl.ItemsSourceProperty, "DataContext", "Loaded");
            ConventionManager.AddElementConvention<Pushpin>(ContentControl.ContentProperty, "DataContext", "Tap");
            ConventionManager.AddElementConvention<Map>(Map.DataContextProperty, "DataContext", "Tap");
            // no DataContext property so we have to set the bindings manually
            //ConventionManager.AddElementConvention<Microsoft.Phone.Maps.Controls.MapLayer>(Microsoft.Phone.Maps.Controls.MapLayer.DataContextProperty, "DataContext", "Tap");
        }
    }
}
