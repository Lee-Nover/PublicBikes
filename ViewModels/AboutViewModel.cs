using Caliburn.Micro;
using Bicikelj.Model;
using System.Linq;
using Bicikelj.Views;
using BugSense;

namespace Bicikelj.ViewModels
{
    public class AboutViewModel : Screen
    {
        public string SupportedCities { get; set; }
        public string AppTitle { get; set; }
        public string VersionNumber { get; set; }

        protected override void OnActivate()
        {
            base.OnActivate();
            if (string.IsNullOrEmpty(SupportedCities))
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
            var cities = from city in BikeServiceProvider.GetAllCities() orderby city.Country select city;
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
            SupportedCities = list.Remove(list.Length - 2, 2);
            NotifyOfPropertyChange(() => SupportedCities);
        }
    }
}
