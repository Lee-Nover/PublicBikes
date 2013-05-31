using Bicikelj.Model;
using Caliburn.Micro;
using Bicikelj.Views;
using System.Linq;
using System.Collections.Generic;

namespace Bicikelj.ViewModels
{
    public class SystemConfigViewModel : Screen
    {
        private IEventAggregator events;
        private SystemConfig config;

        public SystemConfigViewModel(IEventAggregator events, SystemConfig config)
        {
            this.events = events;
            this.config = config;
            this.Cities = new List<string>();
            Cities.Add("");
            Cities.AddRange(BikeServiceProvider.GetAllCities().OrderBy(c => c.CityName).Select(c => c.CityName.ToLower()));
        }

        public bool LocationEnabled
        {
            get { return config != null ? config.LocationEnabled : false; }
            set {
                if (config == null)
                    return;
                if (value == config.LocationEnabled)
                    return;
                config.LocationEnabled = value;
                NotifyOfPropertyChange(() => LocationEnabled);
                UpdateLocation();
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

        public string SelectedCity
        {
            get { return config != null ? config.City : ""; }
            set
            {
                if (config == null)
                    return;
                if (value == config.City)
                    return;
                config.City = value;
                NotifyOfPropertyChange(() => SelectedCity);
                // check if we want to get the current city
                UpdateLocation();
            }
        }

        public string CurrentCity
        {
            get { return config != null ? config.CurrentCity : ""; }
        }

        public List<string> Cities { get; private set; }

        private void LocationUpdated()
        {
            var allStations = IoC.Get<StationLocationList>();
            if (allStations != null)
                allStations.City = config.UseCity;
            events.Publish(config);
        }

        private void UpdateLocation()
        {
            if (config == null)
                return;
            if (config.LocationEnabled)
                LocationHelper.GetCurrentCity((city, ex) =>
                {
                    if (!string.IsNullOrEmpty(city))
                    {
                        config.CurrentCity = city;
                        NotifyOfPropertyChange(() => CurrentCity);
                    }
                    LocationUpdated();
                });
            else
                LocationUpdated();
        }
    }
}