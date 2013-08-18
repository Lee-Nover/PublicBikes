using System.Collections.Generic;
using System.Linq;
using System;
using System.Xml.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Net;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ServiceStack.Text;

namespace Bicikelj.Model
{
    public class BikeServiceProvider
    {
        public string ServiceName { get; set; }
        public string ServiceUrl { get; set; }
        protected virtual IList<City> GetCities() { return null; }
        public virtual IObservable<StationAndAvailability> GetAvailability2(StationLocation station) { return null; }
        public virtual IObservable<StationAvailability> GetAvailability(StationLocation station) { return GetAvailability2(station).Select(a => a.Availability); }
        public virtual IObservable<List<StationLocation>> DownloadStations(string cityName) { return null; }

        #region Static members
        private static List<City> allCities = null;
        public static IList<City> GetAllCities()
        {
            if (allCities == null)
            {
                allCities = new List<City>();
                foreach (var provider in providers)
                {
                    var cities = provider.GetCities();
                    if (cities != null && cities.Count > 0)
                        allCities.AddRange(cities);
                }
            }
            return allCities;
        }
        private static BikeServiceProvider[] providers = new BikeServiceProvider[] {
            CycloCityService.Instance, VeloService.Instance, NextBikeService.Instance, SambaService.Instance, BicingService.Instance
        };

        public static City FindByCityName(string cityName)
        {
            City result = null;
            if (string.IsNullOrEmpty(cityName))
                return result;

            var allCities = GetAllCities();
            result = allCities.Where(c =>
                c.UrlCityName.Equals(cityName, StringComparison.InvariantCultureIgnoreCase)
                || c.CityName.Equals(cityName, StringComparison.InvariantCultureIgnoreCase)
                || (!string.IsNullOrEmpty(c.AlternateCityName) && cityName.ToLowerInvariant().Contains(c.AlternateCityName.ToLowerInvariant()))).FirstOrDefault();

            return result;
        }

        #endregion
    }

    public class CycloCityService : BikeServiceProvider
    {
        #region Static members
        
        public static CycloCityService Instance = new CycloCityService();

        private static List<StationLocation> LoadStationsFromXML(string stationsStr, string city)
        {
            if (string.IsNullOrWhiteSpace(stationsStr))
                return null;
            XDocument doc = XDocument.Load(new System.IO.StringReader(stationsStr));
            var stations = (from s in doc.Descendants("marker")
                            select new StationLocation
                            {
                                Number = (int)s.Attribute("number"),
                                Name = (string)s.Attribute("name"),
                                Address = (string)s.Attribute("address"),
                                FullAddress = (string)s.Attribute("fullAddress"),
                                Latitude = (double)s.Attribute("lat"),
                                Longitude = (double)s.Attribute("lng"),
                                Open = (bool)s.Attribute("open"),
                                City = city
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
            new City(){ CityName = "Valencia", Country = "Spain", ServiceName = "balenbisi", UrlCityName = "valencia", Provider = Instance  }
        };
        #endregion

        protected override IList<City> GetCities()
        {
            // todo: load the city list from the web service
            return cityList;
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            return DownloadUrl.GetAsync(GetStationDetailsUri(station.City) + station.Number.ToString())
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Select(s => new StationAndAvailability(station, LoadAvailabilityFromXML(s)));
        }

        public override IObservable<List<StationLocation>> DownloadStations(string cityName)
        {
            return DownloadUrl.GetAsync(GetStationListUri(cityName))
                .Select<string, List<StationLocation>>(s =>
                {
                    var sl = LoadStationsFromXML(s, cityName);
                    return sl;
                });
        }
    }

    public class PubliBikeService : BikeServiceProvider
    {
        public static PubliBikeService Instance = new PubliBikeService();

        //private static string StationListUrl = "https://www.publibike.ch/en/stations.html";

        
    }

    public class VeloService : BikeServiceProvider
    {
        public static VeloService Instance = new VeloService();

        private static string StationListUrl = "https://www.velo-antwerpen.be/localizaciones/station_map.php";
        private static string StationInfoUrl = "https://www.velo-antwerpen.be/CallWebService/StationBussinesStatus.php";

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Antwerpen", Country = "Belgium", ServiceName = "velo", UrlCityName = "antwerpen", Provider = Instance }
            };
            return result;
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            var urlData = string.Format("idStation={0}&s_id_idioma=en", station.Number);
            return DownloadUrl.PostAsync(StationInfoUrl, urlData, station)
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Select(s => new StationAndAvailability(station, LoadAvailabilityFromHTML(s.Object)));
        }

        public override IObservable<List<StationLocation>> DownloadStations(string cityName)
        {
            return DownloadUrl.GetAsync(StationListUrl)
                .Select<string, List<StationLocation>>(s =>
                {
                    var sl = LoadStationsFromHTML(s, cityName);
                    return sl;
                });
        }

        private List<StationLocation> LoadStationsFromHTML(string s, string cityName)
        {
            var result = new List<StationLocation>();

            const string CCoordStr = "point = new GLatLng(";
            const string CDataStr = "data:\"";

            int dataPos = 0;
            int coordPos = s.IndexOf(CCoordStr);
            if (coordPos > 0)
                s = s.Substring(coordPos, s.IndexOf("</script>", coordPos) - coordPos);
            coordPos = s.IndexOf(CCoordStr);
            while (coordPos > -1)
            {
                coordPos += CCoordStr.Length;
                // new GLatLng(51.199306000000000000,4.390135000000000000);
                var coordEndPos = s.IndexOf(");", coordPos);
                var coordStr = s.Substring(coordPos, coordEndPos - coordPos);
                var coords = coordStr.Split(',');
                // data:"idStation=12&addressnew=MDEyIC0gQnJ1c3NlbA==&s_id_idioma=en",
                dataPos = s.IndexOf(CDataStr, coordEndPos) + CDataStr.Length;
                var dataEndPos = s.IndexOf("\",", dataPos);
                var dataStr = s.Substring(dataPos, dataEndPos - dataPos);
                var dataValues = dataStr.Split('&');

                var station = new StationLocation();
                station.Latitude = double.Parse(coords[0], CultureInfo.InvariantCulture);
                station.Longitude = double.Parse(coords[1], CultureInfo.InvariantCulture);
                var value = dataValues[0].Split('=')[1];
                station.Number = int.Parse(value);
                value = dataValues[1].Remove(0, dataValues[1].IndexOf('=') + 1);
                var bytes = Convert.FromBase64String(value);
                station.Address = UTF8Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                station.City = cityName;
                station.Name = station.Address;
                result.Add(station);

                coordPos = s.IndexOf(CCoordStr, dataPos);
            }

            return result;
        }

        private StationAvailability LoadAvailabilityFromHTML(string s)
        {
            string availStr = "0";
            string slotsStr = "0";
            var bikePos = s.IndexOf("Bicycles");
            // todo: deleteing one char at a time is really stupid and slow
            if (bikePos > 0)
            {
                s = s.Remove(0, bikePos + 8);
                while (!char.IsNumber(s[0]))
                    s = s.Remove(0, 1);
                
                while (char.IsNumber(s[0]))
                {
                    availStr += s[0];
                    s = s.Remove(0, 1);
                }
                bikePos = s.IndexOf("Slots");
                s = s.Remove(0, bikePos + 5);
                while (!char.IsNumber(s[0]))
                    s = s.Remove(0, 1);
                
                while (char.IsNumber(s[0]))
                {
                    slotsStr += s[0];
                    s = s.Remove(0, 1);
                }
            }
            return new StationAvailability()
            {
                Available = int.Parse(availStr),
                Free = int.Parse(slotsStr)
            };
        }
    }

    public class NextBikeService : BikeServiceProvider
    {
        public static NextBikeService Instance = new NextBikeService();

        private static string StationListUrl = "http://nextbike.net/maps/nextbike-official.xml";
        private static string StationInfoUrl = "https://nextbike.net/reservation/?action=check&format=json&start_place_id={0}";
        //private static string referer = "https://nextbike.net/reservation/?city_id_focused=0&height=400&maponly=1&language=de";

        private static List<StationLocation> LoadStationsFromXML(string stationsStr, string cityName)
        {
            if (string.IsNullOrWhiteSpace(stationsStr))
                return null;
            List<StationLocation> stations = null;
            XDocument doc = XDocument.Load(new System.IO.StringReader(stationsStr));
            foreach (var xcountry in doc.Descendants("country"))
            {
                var country = (string)xcountry.Attribute("domain");
                country = DomainNameToCountry(country);
                foreach (var xcity in xcountry.Descendants("city"))
                {
                    var cname = (string)xcity.Attribute("name");
                    if (!string.Equals(cname, cityName, StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    var city = new City();
                    city.CityName = (string)xcity.Attribute("name");
                    if (!string.IsNullOrEmpty(xcity.Attribute("alias").Value))
                        city.AlternateCityName = (string)xcity.Attribute("alias");
                    city.Country = country;
                    city.ServiceName = (string)xcountry.Attribute("name");
                    city.Provider = Instance;

                    stations = (from s in xcity.Descendants("place")
                                select new StationLocation
                                {
                                    Number = (int)s.Attribute("number"),
                                    Name = (string)s.Attribute("name"),
                                    //Address = (string)s.Attribute("address"),
                                    //FullAddress = (string)s.Attribute("fullAddress"),
                                    Latitude = (double)s.Attribute("lat"),
                                    Longitude = (double)s.Attribute("lng"),
                                    City = city.CityName
                                }).ToList();

                    city.Stations = stations;
                }
            }
            

            return stations;
        }

        private static string DomainNameToCountry(string domain)
        {
            switch (domain)
            {
                case "de": return "Germany";
                case "nz": return "New Zealand";
                case "at": return "Austria";
                case "la": return "Austria";
                case "ch": return "Switzerland";
                case "lv": return "Latvia";
                case "pl": return "Poland";
                case "nb": return "Germany";
                case "tr": return "Turkey";
                case "mr": return "Germany";
                case "cy": return "Cyprus";
                case "bb": return "Poland";
                case "pb": return "Poland";
                case "ob": return "Poland";
                case "az": return "Azerbaijan";
                case "vp": return "Poland";
                case "nk": return "Turkey";
                case "me": return "Dubai";
                case "hr": return "Croatia";
                case "sz": return "Germany";
                case "eg": return "Germany";
                default:
                    return domain;
            }
        }

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Leipzig", Country = "Germany", ServiceName = "nextbike Germany", UrlCityName = "leipzig", Provider = Instance },
                new City(){ CityName = "Frankfurt", Country = "Germany", ServiceName = "nextbike Germany", UrlCityName = "frankfurt", Provider = Instance },
                new City(){ CityName = "Berlin", Country = "Germany", ServiceName = "nextbike Germany", UrlCityName = "berlin", Provider = Instance }
            };
            return result;
        }

        public class NextBikeAvailability
        {
            public int Available { get; set; }
        }

        public override IObservable<List<StationLocation>> DownloadStations(string cityName)
        {
            return DownloadUrl.GetAsync(StationListUrl)
                .Select<string, List<StationLocation>>(s =>
                {
                    var sl = LoadStationsFromXML(s, cityName);
                    return sl;
                });
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            var url = string.Format(StationInfoUrl, station.Number);
            return DownloadUrl.GetAsync<NextBikeAvailability>(url, station)
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Select(s => new StationAndAvailability(station, LoadAvailabilityFromNBA(s.Object)));
        }

        private StationAvailability LoadAvailabilityFromNBA(NextBikeAvailability nba)
        {
            return new StationAvailability()
            {
                Available = nba.Available
            };
        }
    }

    public class SambaService : BikeServiceProvider
    {
        public static SambaService Instance = new SambaService();

        private static string StationListUrl = "http://www.movesamba.com/{0}/mapaestacao.asp";

        // todo: add availability caching and read from cache until some time pases, eg. 5 min

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Pernambuco", Country = "Brasil", ServiceName = "BikePE", UrlCityName = "bikepe", Provider = Instance },
                new City(){ CityName = "Petrolina", Country = "Brasil", ServiceName = "PetroBike", UrlCityName = "petrobike", Provider = Instance },
                new City(){ CityName = "Porto Alegre", Country = "Brasil", ServiceName = "bike PoA", UrlCityName = "bikepoa", Provider = Instance },
                new City(){ CityName = "Porto Leve", Country = "Brasil", ServiceName = "PortoLeve", UrlCityName = "portoleve", Provider = Instance },
                new City(){ CityName = "Rio de Janeiro", Country = "Brasil", ServiceName = "Bike Rio", UrlCityName = "sambarjpt", Provider = Instance },
                new City(){ CityName = "São Paulo", Country = "Brasil", ServiceName = "Bike Sampa", UrlCityName = "bikesampa", Provider = Instance },
                new City(){ CityName = "Santos", Country = "Brasil", ServiceName = "Bike Santos", UrlCityName = "bikesantos", Provider = Instance },
                new City(){ CityName = "Sorocaba", Country = "Brasil", ServiceName = "Samba", UrlCityName = "sorocaba", Provider = Instance }
            };
            return result;
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            return DownloadStationsWithAvailability(station.City)
                .Select(sl => sl.Where(sa => sa.Station.Number == station.Number).FirstOrDefault());
        }

        public override IObservable<List<StationLocation>> DownloadStations(string cityName)
        {
            return DownloadStationsWithAvailability(cityName)
                .Select(sl => sl.Select(sa => sa.Station).ToList());
        }

        public IObservable<List<StationAndAvailability>> DownloadStationsWithAvailability(string cityName)
        {
            var url = string.Format(StationListUrl, cityName);
            return DownloadUrl.GetAsync(url)
                .Select<string, List<StationAndAvailability>>(s =>
                {
                    var sl = LoadStationsFromHTML(s, cityName);
                    return sl;
                });
        }

        private List<StationAndAvailability> LoadStationsFromHTML(string s, string cityName)
        {
            var result = new List<StationAndAvailability>();

            // exibirEstacaMapa("-23.930828", "-46.346232","img/icone-estacoes.gif","Saboó","30","A","EO","11","12","Praça Ruy de Lugo Viña, oposto ao Cemitério da Filosofia");
            // lat, lng, img, name, number, status online, status op, bikes, station count, address
            /*
             if((StatusOperacao == "EO") && (StatusOnline == "A")){
                strCaminho = "img/icone-estacoes.gif";
                if(VagasOcupadas == 0){
                strCaminho = "img/icone-estacoes-vazia.gif";
                }
                if(numBicicletas == VagasOcupadas){
                strCaminho = "img/icone-estacoes-cheia.gif";
                }
              }else if( ((StatusOperacao == "EI") && (StatusOnline == "A")) || ((StatusOperacao == "EI") && (StatusOnline == "I"))){
                strCaminho = "img/icone-estacoes-cinza.gif";
                }else{
                strCaminho = "img/icone-estacoes-desativado.gif";
                }
             */
            const string CDataStr = "exibirEstacaMapa(";
            
            int dataPos = s.IndexOf(CDataStr);
            if (dataPos > 0)
                s = s.Substring(dataPos, s.IndexOf("function exibirEstacaMapa(", dataPos) - dataPos);
            dataPos = s.IndexOf(CDataStr);
            string[] split = new string[] { "\",\"" };
            while (dataPos > -1)
            {
                dataPos += CDataStr.Length;
                var dataEndPos = s.IndexOf(");", dataPos);
                if (dataEndPos > -1 && s[dataEndPos] == '"')
                    dataEndPos--;
                var dataStr = s.Substring(dataPos + 1, dataEndPos - dataPos);
                var dataValues = Regex.Replace(dataStr, @"\t|\n|\r", "").Split(split, StringSplitOptions.None);
                // stations without the 'address' are test stations
                if (dataValues[9].Trim() != "")
                {
                    var station = new StationLocation();
                    station.Latitude = double.Parse(dataValues[0], CultureInfo.InvariantCulture);
                    station.Longitude = double.Parse(dataValues[1], CultureInfo.InvariantCulture);
                    station.Name = dataValues[3];
                    station.Number = int.Parse(dataValues[4]);
                    station.Address = dataValues[9];
                    station.City = cityName;
                    var availability = new StationAvailability();
                    availability.Available = int.Parse(dataValues[7]);
                    availability.Free = int.Parse(dataValues[8]);
                    availability.Free -= availability.Available;
                    availability.Connected = dataValues[7] == "A";
                    availability.Open = dataValues[6] == "EO";

                    result.Add(new StationAndAvailability(station, availability));
                }
                dataPos = s.IndexOf(CDataStr, dataPos);
            }

            return result;
        }
    }

    public class BicingService : BikeServiceProvider
    {
        public static BicingService Instance = new BicingService();

        private static string StationListUrl = "https://www.bicing.cat/ca/formmap/getJsonObject";

        public class BicingCommand
        {
            public string command { get; set; }
            public string method { get; set; }
            public string data { get; set; }
        }

        public class BicingAvailability
        {
            public int StationID { get; set; }
            public string StationName { get; set; }
            public double AddressGmapsLongitude { get; set; }
            public double AddressGmapsLatitude { get; set; }
            public int StationAvailableBikes { get; set; }
            public int StationFreeSlot { get; set; }
            public string AddressStreet1 { get; set; }
            public string AddressNumber { get; set; }
            public string StationStatusCode { get; set; }
        }

        private static List<StationAndAvailability> LoadStationsFromHTML(string stationsStr, string cityName)
        {
            if (string.IsNullOrWhiteSpace(stationsStr))
                return null;
            List<StationAndAvailability> stations = new List<StationAndAvailability>();

            const string CDataStr = @"""data"":""[{";
            const string CDataEndStr = @"}]"",""";
            var dataStart = stationsStr.IndexOf(CDataStr);
            if (dataStart > 0)
            {
                dataStart += CDataStr.Length - 2;
                var dataEnd = stationsStr.IndexOf(CDataEndStr, dataStart);
                if (dataEnd > 0)
                {
                    stationsStr = stationsStr.Substring(dataStart, dataEnd - dataStart + 2);
                }
            }
            
            stationsStr = Regex.Unescape(stationsStr);
            List<BicingAvailability> availabilityList = stationsStr.FromJson<List<BicingAvailability>>();
            foreach (var item in availabilityList)
            {
                if (string.IsNullOrEmpty(item.AddressStreet1))
                    continue;

                var station = new StationLocation();
                station.Name = item.StationName;
                station.Number = item.StationID;
                station.Latitude = item.AddressGmapsLatitude;
                station.Longitude = item.AddressGmapsLongitude;
                station.Open = item.StationStatusCode == "OPN";
                station.Address = item.AddressStreet1;
                if (!string.IsNullOrEmpty(item.AddressNumber))
                    station.Address += " " + item.AddressNumber;

                var availability = new StationAvailability();
                availability.Available = item.StationAvailableBikes;
                availability.Free = item.StationFreeSlot;
                availability.Total = availability.Available + availability.Free;
                availability.Open = station.Open;

                stations.Add(new StationAndAvailability(station, availability));
            }
            return stations;
        }

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Barcelona", Country = "Spain", ServiceName = "bicing", UrlCityName = "barcelona", Provider = Instance }
            };
            return result;
        }

        public override IObservable<List<StationLocation>> DownloadStations(string cityName)
        {
            return DownloadStationsWithAvailability(cityName)
                .Select(sl => sl.Select(sa => sa.Station).ToList());
        }

        public IObservable<List<StationAndAvailability>> DownloadStationsWithAvailability(string cityName)
        {
            return DownloadUrl.GetAsync(StationListUrl)
                .Select(cmdList =>
                {
                    var sl = LoadStationsFromHTML(cmdList, cityName);
                    return sl;
                });
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            return DownloadStationsWithAvailability(station.City)
                .Select(sl => sl.Where(sa => sa.Station.Number == station.Number).FirstOrDefault());
        }
    }
}
