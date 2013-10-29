using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Text;
using ServiceStack.Text.Json;
using System.Runtime.Serialization;
using System.Device.Location;
using System.Xml.Linq;

namespace Bicikelj.Model
{
    public class BixiService : BikeServiceProvider
    {
        public class BixiStationJS
        {
            public int? id { get; set; }
            public string name { get; set; }
            public string address { get; set; }
            public double lat { get; set; }
            public double lng { get; set; }
            public int nbBikes { get; set; }
            public int nbEmptyDocks { get; set; }
            public bool installed { get; set; }
            public bool locked { get; set; }
        }

        public class BixiStationJSON
        {
            public int id { get; set; }
            public string stationName { get; set; }
            public int availableDocks { get; set; }
            public int totalDocks { get; set; }
            public double latitude { get; set; }
            public double longitude { get; set; }
            public string statusValue { get; set; }
            public int statusKey { get; set; }
            public int availableBikes { get; set; }
            public string stAddress1 { get; set; }
            public string stAddress2 { get; set; }
            public string city { get; set; }
            public bool testStation { get; set; }
            public string landMark { get; set; }
        }

        public class BixiStationListJSON
        {
            public List<BixiStationJSON> stationBeanList { get; set; }
        }

        public static BixiService Instance = new BixiService();

        private static string StationListUrl(string cityName)
        {
            switch (cityName)
            {
                case "london":
                    return "https://web.barclayscyclehire.tfl.gov.uk/maps"; // js
                case "washingtondc":
                    return "http://www.capitalbikeshare.com/data/stations/bikeStations.xml";
                case "minneapolis":
                    return "https://secure.niceridemn.org/data2/bikeStations.xml";
                case "boston":
                    return "http://www.thehubway.com/data/stations/bikeStations.xml";
                case "chattanooga":
                    return "http://www.bikechattanooga.com/stations/json";
                case "melbourne":
                    return "http://www.melbournebikeshare.com.au/stationmap/data"; // json
                default:
                    return string.Format("https://{0}.bixi.com/maps/statajax", cityName);// js
            }
        }

        // todo: add availability caching and read from cache until some time pases, eg. 5 min

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Montreal", Country = "Canada", ServiceName = "BIXI", UrlCityName = "montreal", Provider = Instance },
                new City(){ CityName = "Toronto", Country = "Canada", ServiceName = "BIXI", UrlCityName = "toronto", Provider = Instance },
                new City(){ CityName = "Ottawa", Country = "Canada", ServiceName = "BIXI", UrlCityName = "capital", AlternateCityName = "Gatineau", Provider = Instance },
                new City(){ CityName = "Washington DC", Country = "USA", ServiceName = "Capital Bike Share", UrlCityName = "washingtondc", AlternateCityName = "washington", Provider = Instance },
                new City(){ CityName = "Minneapolis", Country = "USA", ServiceName = "Nice Ride", UrlCityName = "minneapolis", Provider = Instance },
                new City(){ CityName = "Boston", Country = "USA", ServiceName = "Hubway", UrlCityName = "boston", Provider = Instance },
                new City(){ CityName = "Chattanooga", Country = "USA", ServiceName = "Bike Chattanooga", UrlCityName = "chattanooga", Provider = Instance },
                new City(){ CityName = "London", Country = "United Kingdom", ServiceName = "Barclays Cycle Hire", UrlCityName = "london", Provider = Instance },
                new City(){ CityName = "Melbourne", Country = "Australia", ServiceName = "Melbourne bike share", UrlCityName = "melbourne", Provider = Instance }
            };
            return result;
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            var availability = GetAvailabilityFromCache(station);
            if (availability.Availability != null)
                return Observable.Return<StationAndAvailability>(availability);
            else
                return DownloadStationsWithAvailability(station.City)
                    .Select(sl => sl.Where(sa => sa.Station.Number == station.Number).FirstOrDefault());
        }

        public override IObservable<List<StationAndAvailability>> DownloadStationsWithAvailability(string cityName)
        {
            var url = StationListUrl(cityName);
            return DownloadUrl.GetAsync(url)
                .Select(s =>
                {
                    List<StationAndAvailability> sl;

                    switch (cityName)
                    {
                        case "washingtondc":
                        case "minneapolis":
                        case "boston":
                            sl = LoadStationsFromXML(s, cityName);
                            break;

                        case "chattanooga":
                            sl = LoadStationsFromJSON(s, cityName);
                            break;

                        case "melbourne":
                            sl = LoadStationsFromJSON2(s, cityName);
                            break;

                        default:
                            sl = LoadStationsFromHTML(s, cityName);
                            break;
                    }

                    UpdateAvailabilityCache(sl);
                    return sl;
                });
        }

        private List<StationAndAvailability> LoadStationsFromHTML(string s, string cityName)
        {
            /*
             default: var station = {id:"1",name:"Notre Dame / Place Jacques Cartier",lat:"45.508183",long:"-73.554094",nbBikes:"2",nbEmptyDocks:"25",installed:"true",
               locked:"false",temporary:"false", sponsorName:null, sponsorLink:null, sponsorLogo:null};
             
             * london: var station;station={...};
             */
            var result = new List<StationAndAvailability>();
            string CDataStr = "var station = {";
            if (cityName == "london")
                CDataStr = "station={";
            int id = 1;
            int dataPos = s.IndexOf(CDataStr);
            //if (dataPos > 0)
            //    s = s.Substring(dataPos, s.IndexOf(CDataStr, dataPos) - dataPos);
            //dataPos = s.IndexOf(CDataStr);
            while (dataPos > -1)
            {
                ServiceStack.Text.JsConfig.ConvertObjectTypesIntoStringDictionary = true;
                dataPos += CDataStr.Length - 1;
                var dataEndPos = s.IndexOf("};", dataPos);
                var dataStr = s.Substring(dataPos, dataEndPos - dataPos + 1);
                var dataJson = Regex.Replace(dataStr, @"\t|\n|\r", "");
                dataJson = dataJson.Replace("id:", "\"id\":").Replace("name:", "\"name\":").Replace("address:", "\"address\":").Replace("lat:", "\"lat\":").Replace("long:", "\"lng\":")
                    .Replace("nbBikes:", "\"nbBikes\":").Replace("nbEmptyDocks:", "\"nbEmptyDocks\":").Replace("installed:", "\"installed\":").Replace("closed:", "\"closed\":");
                var bixiStation = dataJson.FromJson<BixiStationJS>();
                var station = new StationLocation();
                var availability = new StationAvailability();
                if (bixiStation.id == null)
                    bixiStation.id = id++;
                station.Number = bixiStation.id.GetValueOrDefault();
                station.Name = bixiStation.name;
                station.Address = bixiStation.address;
                station.Latitude = bixiStation.lat;
                station.Longitude = bixiStation.lng;
                station.City = cityName;
                availability.Available = bixiStation.nbBikes;
                availability.Free = bixiStation.nbEmptyDocks;
                availability.Open = bixiStation.installed;
                availability.Connected = !bixiStation.locked;
                availability.Total = availability.Available + availability.Free;
                result.Add(new StationAndAvailability(station, availability));

                dataPos = s.IndexOf(CDataStr, dataPos);
            }

            return result;
        }

        private List<StationAndAvailability> LoadStationsFromXML(string stationsStr, string cityName)
        {
            if (string.IsNullOrWhiteSpace(stationsStr))
                return null;
            XDocument doc = XDocument.Load(new System.IO.StringReader(stationsStr));
            var stations = (from s in doc.Descendants("station")
                            select new StationAndAvailability()
                            {
                                Station = new StationLocation()
                                {
                                    Number = (int)s.Descendants("id").FirstOrDefault(),
                                    Name = (string)s.Descendants("name").FirstOrDefault(),
                                    Latitude = (double)s.Descendants("lat").FirstOrDefault(),
                                    Longitude = (double)s.Descendants("long").FirstOrDefault(),
                                    Open = !(bool)s.Descendants("locked").FirstOrDefault(),
                                    City = cityName
                                },

                                Availability = new StationAvailability()
                                {
                                    Available = (int)s.Descendants("nbBikes").FirstOrDefault(),
                                    Free = (int)s.Descendants("nbEmptyDocks").FirstOrDefault(),
                                    Open = !(bool)s.Descendants("locked").FirstOrDefault(),
                                }
                            }).ToList();

            return stations;
        }

        private List<StationAndAvailability> LoadStationsFromJSON(string stationsStr, string cityName)
        {
            if (string.IsNullOrWhiteSpace(stationsStr))
                return null;

            var result = stationsStr.FromJson<BixiStationListJSON>().stationBeanList
                .Select(s => new StationAndAvailability()
                {
                    Station = new StationLocation()
                    {
                        Number = s.id,
                        Name = s.landMark,
                        Address = s.stAddress1,
                        Latitude = s.latitude,
                        Longitude = s.longitude,
                        Open = !s.testStation && s.statusKey == 1,
                        City = cityName
                    },

                    Availability = new StationAvailability()
                    {
                        Available = s.availableBikes,
                        Free = s.availableDocks,
                        Total = s.totalDocks,
                        Open = !s.testStation && s.statusKey == 1
                    }
                }).ToList();

            return result;
        }

        private List<StationAndAvailability> LoadStationsFromJSON2(string stationsStr, string cityName)
        {
            if (string.IsNullOrWhiteSpace(stationsStr))
                return null;

            var result = stationsStr.Replace("long", "lng").FromJson<List<BixiStationJS>>()
                .Select(s => new StationAndAvailability()
                {
                    Station = new StationLocation()
                    {
                        Number = s.id.GetValueOrDefault(),
                        Name = s.name,
                        Latitude = s.lat,
                        Longitude = s.lng,
                        Open = s.installed && !s.locked,
                        City = cityName
                    },

                    Availability = new StationAvailability()
                    {
                        Available = s.nbBikes,
                        Free = s.nbEmptyDocks,
                        Total = s.nbBikes + s.nbEmptyDocks,
                        Open = s.installed && !s.locked
                    }
                }).ToList();

            return result;
        }
    }
}
