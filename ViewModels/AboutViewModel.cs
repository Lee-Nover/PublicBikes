using Caliburn.Micro;
using Bicikelj.Model;
using System.Linq;

namespace Bicikelj.ViewModels
{
    public class AboutViewModel : Screen
    {
        public string SupportedCities { get; set; }

        protected override void OnActivate()
        {
            base.OnActivate();
            if (string.IsNullOrEmpty(SupportedCities))
                UpdateCities();
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
