using System;
using Caliburn.Micro;

namespace Bicikelj.ViewModels
{
    public class AdsViewModel : Screen
    {
        protected override void OnActivate()
        {
            base.OnActivate();
            App.CurrentApp.LogAnalyticEvent("Activated AdsView");
        }

        public void OpenMoreAdDeals()
        {
            App.CurrentApp.LogAnalyticEvent("Opened AdDeals");
            var nav = IoC.Get<INavigationService>();
            nav.Navigate(new Uri("/AdDealsSDKWP7;component/Views/MoreAdDeals.xaml", UriKind.Relative));
        }
    }
}
