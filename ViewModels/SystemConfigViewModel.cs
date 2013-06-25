using System;
using System.Collections.Generic;
using System.Linq;
using Bicikelj.Model;
using Caliburn.Micro;

namespace Bicikelj.ViewModels
{
    public class SystemConfigViewModel : Screen
    {
        private IEventAggregator events;
        private SystemConfig config;
        private CityContextViewModel cityContext;

        public SystemConfigViewModel(IEventAggregator events, SystemConfig config, CityContextViewModel cityContext)
        {
            this.events = events;
            this.config = config;
            this.cityContext = cityContext;
            this.Cities = new List<City>();
            Cities.Add(new City() { CityName = " - automatic - " });
            Cities.AddRange(BikeServiceProvider.GetAllCities().OrderBy(c => c.Country + c.CityName).Select(c => c));
            if (string.IsNullOrEmpty(config.City))
                selectedCity = Cities[0];
            else
                selectedCity = Cities.Where(c => config.City.Equals(c.UrlCityName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public bool LocationEnabled
        {
            get { return config != null ? config.LocationEnabled.GetValueOrDefault() : false; }
            set {
                if (config == null)
                    return;
                if (value == config.LocationEnabled)
                    return;
                config.LocationEnabled = value;
                NotifyOfPropertyChange(() => LocationEnabled);
                ObserveCurrentCity(value);
                events.Publish(config);
            }
        }

        public bool UseImperialUnits { 
            get { return config != null ? config.UseImperialUnits : false; }
            set
            {
                if (config == null)
                    return;
                if (value == config.UseImperialUnits)
                    return;
                config.UseImperialUnits = value;
                NotifyOfPropertyChange(() => UseImperialUnits);
                events.Publish(config);
            }
        }

        public TravelSpeed WalkingSpeed
        {
            get { return config != null ? config.WalkingSpeed : TravelSpeed.Normal; }
            set
            {
                if (config == null)
                    return;
                if (value == config.WalkingSpeed)
                    return;
                config.WalkingSpeed = value;
                NotifyOfPropertyChange(() => WalkingSpeed);
                events.Publish(config);
            }
        }

        public TravelSpeed CyclingSpeed
        {
            get { return config != null ? config.CyclingSpeed : TravelSpeed.Normal; }
            set
            {
                if (config == null)
                    return;
                if (value == config.CyclingSpeed)
                    return;
                config.CyclingSpeed = value;
                NotifyOfPropertyChange(() => CyclingSpeed);
                events.Publish(config);
            }
        }

        private City selectedCity;
        public City SelectedCity
        {
            get { return selectedCity; }
            set
            {
                if (value == selectedCity)
                    return;
                selectedCity = value;
                if (config != null)
                    if (string.IsNullOrEmpty(selectedCity.UrlCityName))
                        config.City = "";
                    else
                        config.City = selectedCity.UrlCityName;
                NotifyOfPropertyChange(() => SelectedCity);
                LocationUpdated();
            }
        }

        public string CurrentCity
        {
            get { return config != null ? config.CurrentCity : ""; }
        }

        public List<City> Cities { get; private set; }
        
        private IDisposable dispCity = null;
        public void ObserveCurrentCity(bool observe)
        {
            if (observe && config.LocationEnabled.GetValueOrDefault())
            {
                if (dispCity == null)
                    dispCity = LocationHelper.GetCurrentCity()
                        .Subscribe(city =>
                        {
                            config.CurrentCity = city;
                            NotifyOfPropertyChange(() => CurrentCity);
                        });
            }
            else
            {
                ReactiveExtensions.Dispose(ref dispCity);
                config.CurrentCity = "";
                NotifyOfPropertyChange(() => CurrentCity);
            }
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            ObserveCurrentCity(true);
        }

        protected override void OnDeactivate(bool close)
        {
            ObserveCurrentCity(false);
            base.OnDeactivate(close);
        }

        private void LocationUpdated()
        {
            cityContext.SetCity(config.UseCity);
            events.Publish(config);
        }
    }
}