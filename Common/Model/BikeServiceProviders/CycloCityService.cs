using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Xml.Linq;

namespace Bicikelj.Model
{
    public class CycloCityService : BikeServiceProvider
    {
        #region Static members
        
        public static CycloCityService Instance = new CycloCityService();

        private static List<StationAndAvailability> LoadStationsFromXML(string stationsStr, string city)
        {
            if (string.IsNullOrWhiteSpace(stationsStr))
                return null;
            XDocument doc = XDocument.Load(new System.IO.StringReader(stationsStr));
            var stations = (from s in doc.Descendants("marker")
                            select new StationAndAvailability()
                            {
                                Station = new StationLocation()
                                {
                                    Number = (int)s.Attribute("number"),
                                    Name = (string)s.Attribute("name"),
                                    Address = (string)s.Attribute("address"),
                                    FullAddress = (string)s.Attribute("fullAddress"),
                                    Latitude = (double)s.Attribute("lat"),
                                    Longitude = (double)s.Attribute("lng"),
                                    Open = (bool)s.Attribute("open"),
                                    City = city
                                }
                            }).ToList();

            return stations;
        }

        private static StationAvailability LoadAvailabilityFromXML(string availabilityStr)
        {
            XDocument doc = XDocument.Load(new System.IO.StringReader(availabilityStr));
            var stations = from s in doc.Descendants("station")
                           select new StationAvailability
                           {
                               Available = (int)s.Element("available"),
                               Free = (int)s.Element("free"),
                               Total = (int)s.Element("total"),
                               Connected = (bool)s.Element("connected"),
                               Open = (bool)s.Element("open")
                           };
            StationAvailability sa = stations.FirstOrDefault();
            return sa;
        }

        private static string GetStationListUri(string city)
        {
            return string.Format("https://abo-{0}.cyclocity.fr/service/carto", city);
        }

        private static string GetStationDetailsUri(string city)
        {
            return string.Format("https://abo-{0}.cyclocity.fr/service/stationdetails/{0}/", city);
        }

        private static IList<City> cityList = new List<City>() {
            new City(){ CityName = "Amiens", Country = "France", ServiceName = "Vélam", UrlCityName = "amiens", Provider = Instance },
            new City(){ CityName = "Besançon", Country = "France", ServiceName = "VéloCité", UrlCityName = "besancon", Provider = Instance  },
            new City(){ CityName = "Bruxelles", Country = "Belgium", ServiceName = "Villo!", UrlCityName = "bruxelles", AlternateCityName = "Brussels", Provider = Instance  },
            new City(){ CityName = "Brisbane", Country = "Australia", ServiceName = "CityCycle", UrlCityName = "brisbane", Provider = Instance  },
            new City(){ CityName = "Cergy-Pontoise", Country = "France", ServiceName = "vélO2", UrlCityName = "cergy", Provider = Instance  },
            new City(){ CityName = "Créteil", Country = "France", ServiceName = "Cristolib", UrlCityName = "creteil", Provider = Instance  },
            new City(){ CityName = "Dublin", Country = "Ireland", ServiceName = "dublinbikes", UrlCityName = "dublin", Provider = Instance  },
            new City(){ CityName = "Göteborg", Country = "Sweden", ServiceName = "styr & ställ", UrlCityName = "goteborg", AlternateCityName = "Gothenburg", Provider = Instance  },
            new City(){ CityName = "Lillestrøm", Country = "Norway", ServiceName = "Bysykkel", UrlCityName = "lillestrom", Provider = Instance  },
            new City(){ CityName = "Ljubljana", Country = "Slovenia", ServiceName = "Bicikelj", UrlCityName = "ljubljana", Provider = Instance  },
            new City(){ CityName = "Luxembourg", Country = "Luxembourg", ServiceName = "vel’oh!", UrlCityName = "luxembourg", Provider = Instance  },
            new City(){ CityName = "Lyon", Country = "France", ServiceName = "vélo'v", UrlCityName = "lyon", Provider = Instance  },
            new City(){ CityName = "Marseille", Country = "France", ServiceName = "le vélo", UrlCityName = "marseille", Provider = Instance  },
            new City(){ CityName = "Mulhouse", Country = "France", ServiceName = "Vélocité", UrlCityName = "mulhouse", Provider = Instance  },
            new City(){ CityName = "Namur", Country = "Belgium", ServiceName = "Li bia velo", UrlCityName = "namur", Provider = Instance  },
            new City(){ CityName = "Nancy", Country = "France", ServiceName = "vélOstan'lib", UrlCityName = "nancy", Provider = Instance  },
            new City(){ CityName = "Nantes", Country = "France", ServiceName = "bicloo", UrlCityName = "nantes", Provider = Instance  },
            new City(){ CityName = "Paris", Country = "France", ServiceName = "Vélib’", UrlCityName = "paris", Provider = Instance  },
            new City(){ CityName = "Rouen", Country = "France", ServiceName = "cy'clic", UrlCityName = "rouen", Provider = Instance  },
            new City(){ CityName = "Santander", Country = "Spain", ServiceName = "Tusbic", UrlCityName = "santander", Provider = Instance },
            new City(){ CityName = "Seville", Country = "Spain", ServiceName = "SEVici", UrlCityName = "seville", Provider = Instance  },
            new City(){ CityName = "Toulouse", Country = "France", ServiceName = "VélôToulouse", UrlCityName = "toulouse", Provider = Instance },
            new City(){ CityName = "Toyama", Country = "Japan", ServiceName = "CyclOcity", UrlCityName = "toyama", Provider = Instance  },
            new City(){ CityName = "Valencia", Country = "Spain", ServiceName = "valenbisi", UrlCityName = "valence", Provider = Instance  }
        };
        #endregion

        protected override IList<City> GetCities()
        {
            // todo: load the city list from the web service
            return cityList;
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station, bool forceUpdate = false)
        {
            if (!forceUpdate)
            {
                var availability = GetAvailabilityFromCache(station);
                if (availability.Availability != null)
                    return Observable.Return<StationAndAvailability>(availability);
            }
            
            return DownloadUrl.GetAsync(GetStationDetailsUri(station.City) + station.Number.ToString())
                .Retry(1)
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Select(s =>
                {
                    var sa = new StationAndAvailability(station, LoadAvailabilityFromXML(s));
                    UpdateAvailabilityCacheItem(sa);
                    return sa;
                });
        }

        public override IObservable<List<StationAndAvailability>> DownloadStationsWithAvailability(string cityName)
        {
            return DownloadUrl.GetAsync(GetStationListUri(cityName))
                .Retry(1)
                .Select(s =>
                {
                    var sl = LoadStationsFromXML(s, cityName);
                    return sl;
                });
        }
    }
}
