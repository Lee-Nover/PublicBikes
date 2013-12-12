using Caliburn.Micro;
using Microsoft.Phone.Controls;

namespace Bicikelj.ViewModels
{
    public class InfoViewModel : Screen
    {
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
