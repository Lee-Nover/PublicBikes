using Caliburn.Micro;
using Microsoft.Phone.Controls;

namespace Bicikelj.ViewModels
{
    public class InfoViewModel : Screen
    {
        #region WP8 Panorama Hack
#if WP8
        public override bool Equals(object obj)
        {
            if ((obj != null) && (obj.GetType() == typeof(Microsoft.Phone.Controls.PanoramaItem)))
            {
                Microsoft.Phone.Controls.PanoramaItem thePanoItem = (Microsoft.Phone.Controls.PanoramaItem)obj;
                return base.Equals(thePanoItem.Header);
            }
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
#endif
        #endregion

        readonly IEventAggregator events;
        public InfoViewModel(IEventAggregator events)
        {
            this.events = events;
        }

        public void OpenConfig()
        {
            App.CurrentApp.LogAnalyticEvent("Opened Configuration");
            Bicikelj.NavigationExtension.NavigateTo(IoC.Get<SystemConfigViewModel>());
        }

        public void OpenAbout()
        {
            Bicikelj.NavigationExtension.NavigateTo(IoC.Get<AppInfoViewModel>());
        }

        public void OpenRentTimer()
        {
            App.CurrentApp.LogAnalyticEvent("Opened RentTimer");
            Bicikelj.NavigationExtension.NavigateTo(IoC.Get<RentTimerViewModel>());
        }
    }
}
