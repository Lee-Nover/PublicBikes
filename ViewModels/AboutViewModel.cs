using Caliburn.Micro;
using Bicikelj.Model;
using System.Linq;
using Bicikelj.Views;
using BugSense;
using Microsoft.Phone.Tasks;

namespace Bicikelj.ViewModels
{
    public class AboutViewModel : Screen
    {
        public string SupportedServices { get; set; }
        public string SupportedCountries { get; set; }
        public string AppTitle { get; set; }
        public string VersionNumber { get; set; }
        SystemConfig config;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            config = IoC.Get<SystemConfig>();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            if (string.IsNullOrEmpty(SupportedServices))
                UpdateCities();
            if (string.IsNullOrEmpty(VersionNumber))
                UpdateVersionInfo();
        }

        private void UpdateVersionInfo()
        {
            var info = new BugSense.Internal.ManifestAppInfo();
            AppTitle = info.Title;
            VersionNumber = info.Version;
#if DEBUG
            VersionNumber += " (debug)";
#endif
            NotifyOfPropertyChange(() => VersionNumber);
        }

        private void UpdateCities()
        {
            var svclist = "";
            var services = from city in BikeServiceProvider.GetAllCities() orderby city.ServiceName select city.ServiceName;
            foreach (var service in services.Distinct())
                svclist += service + ", ";
            svclist = svclist.Remove(svclist.Length - 2, 2);

            var countrylist = "";
            var countries = from city in BikeServiceProvider.GetAllCities() orderby city.Country select city.Country;
            foreach (var country in countries.Distinct())
                countrylist += country + ", ";
            countrylist = countrylist.Remove(countrylist.Length - 2, 2);

            SupportedServices = svclist;
            SupportedCountries = countrylist;
            /*var cities = from city in BikeServiceProvider.GetAllCities() orderby city.Country select city;
            var country = "";
            var list = "";
            foreach (var city in cities)
            {
                if (!string.Equals(country, city.Country))
                {
                    list += city.Country + ": ";
                    country = city.Country;
                }
                list += city.CityName + ", ";
            }
            SupportedCities = list.Remove(list.Length - 2, 2);*/
            NotifyOfPropertyChange(() => SupportedServices);
            NotifyOfPropertyChange(() => SupportedCountries);
        }

        public void RateApp()
        {
            var marketplaceReviewTask = new MarketplaceReviewTask();
            marketplaceReviewTask.Show();
        }

        public bool IsUpdateAvailable
        {
            get { return config != null && !string.IsNullOrEmpty(config.UpdateAvailable); }
        }

        public void UpdateApp()
        {
            var markeplaceTask = new MarketplaceDetailTask();
            markeplaceTask.Show();
        }

        public void OpenAds()
        {
            var adsVM = IoC.Get<AdsViewModel>();
            NavigationExtension.NavigateTo(adsVM);
        }
    }
}
