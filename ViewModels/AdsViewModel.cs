using System;
using Caliburn.Micro;

namespace Bicikelj.ViewModels
{
    public class AdsViewModel : Screen
    {
        public void OpenMoreAdDeals()
        {
            var nav = IoC.Get<INavigationService>();
            nav.Navigate(new Uri("/AdDealsSDKWP7;component/Views/MoreAdDeals.xaml", UriKind.Relative));
        }
    }
}
