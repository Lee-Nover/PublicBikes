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
            Bicikelj.NavigationExtension.NavigateTo(IoC.Get<SystemConfigViewModel>());
        }

        public void OpenAbout()
        {
            Bicikelj.NavigationExtension.NavigateTo(IoC.Get<AppInfoViewModel>());
        }

        public void OpenRentTimer()
        {
            Bicikelj.NavigationExtension.NavigateTo(IoC.Get<RentTimerViewModel>());
        }
    }
}
