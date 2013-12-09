using Caliburn.Micro;
using Bicikelj.Model;
using System.Linq;
using Bicikelj.Views;
using BugSense;
using Microsoft.Phone.Tasks;
using System;
using Bicikelj.AzureService;
using System.Windows.Navigation;

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
