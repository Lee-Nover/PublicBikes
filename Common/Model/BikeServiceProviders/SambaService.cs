using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace Bicikelj.Model
{
    public class SambaService : AzureServiceProxy
    {
        public SambaService()
        {
            AzureServiceName = "samba";
        }

        public static SambaService Instance = new SambaService();

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Recife", Country = "Brasil", ServiceName = "BikePE", UrlCityName = "bikepe", Provider = Instance },
                new City(){ CityName = "Petrolina", Country = "Brasil", ServiceName = "PetroBike", UrlCityName = "petrobike", Provider = Instance },
                new City(){ CityName = "Porto Alegre", Country = "Brasil", ServiceName = "bike PoA", UrlCityName = "bikepoa", Provider = Instance },
                //new City(){ CityName = "Porto Leve", Country = "Brasil", ServiceName = "PortoLeve", UrlCityName = "portoleve", Provider = Instance },
                new City(){ CityName = "Rio de Janeiro", Country = "Brasil", ServiceName = "Bike Rio", UrlCityName = "sambarjpt", AlternateCityName = "Rio", Provider = Instance },
                new City(){ CityName = "São Paulo", Country = "Brasil", ServiceName = "Bike Sampa", UrlCityName = "bikesampa", Provider = Instance },
                new City(){ CityName = "Santos", Country = "Brasil", ServiceName = "Bike Santos", UrlCityName = "bikesantos", Provider = Instance },
                new City(){ CityName = "Sorocaba", Country = "Brasil", ServiceName = "Samba", UrlCityName = "sorocaba", Provider = Instance }
            };
            return result;
        }
    }

    #region SambaServiceOld
    /*
    public class SambaServiceOld : BikeServiceProvider
    {
        public static SambaServiceOld Instance = new SambaServiceOld();

        private static string StationListUrl = "http://www.movesamba.com/{0}/mapaestacao.asp";

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Recife", Country = "Brasil", ServiceName = "BikePE", UrlCityName = "bikepe", Provider = Instance },
                new City(){ CityName = "Petrolina", Country = "Brasil", ServiceName = "PetroBike", UrlCityName = "petrobike", Provider = Instance },
                new City(){ CityName = "Porto Alegre", Country = "Brasil", ServiceName = "bike PoA", UrlCityName = "bikepoa", Provider = Instance },
                //new City(){ CityName = "Porto Leve", Country = "Brasil", ServiceName = "PortoLeve", UrlCityName = "portoleve", Provider = Instance },
                new City(){ CityName = "Rio de Janeiro", Country = "Brasil", ServiceName = "Bike Rio", UrlCityName = "sambarjpt", Provider = Instance },
                new City(){ CityName = "São Paulo", Country = "Brasil", ServiceName = "Bike Sampa", UrlCityName = "bikesampa", Provider = Instance },
                new City(){ CityName = "Santos", Country = "Brasil", ServiceName = "Bike Santos", UrlCityName = "bikesantos", Provider = Instance },
                new City(){ CityName = "Sorocaba", Country = "Brasil", ServiceName = "Samba", UrlCityName = "sorocaba", Provider = Instance }
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
            var url = string.Format(StationListUrl, cityName);
            return DownloadUrl.GetAsync(url)
                .Retry(1)
                .Select(s =>
                {
                    List<StationAndAvailability> sl;

                    switch (cityName)
                    {
                        case "sambarjpt":
                        case "bikesampa":
                        case "sorocaba":
                            sl = LoadStationsFromHTML_RIO(s, cityName);
                            break;
                        default:
                            sl = LoadStationsFromHTML(s, cityName);
                            break;
                    }
                    
                    UpdateAvailabilityCache(sl);
                    return sl;
                });
        }

        private static string[] ParseFunctionParams(string s)
        {
            // parses 'quoted groups', doesn't handle nested quotes ' " '
            var regex = new Regex(@"^(?:'(?<item>[^']*)'|(?<item>[^,]*))(?:,(?:'(?<item>[^']*)'|(?<item>[^,]*)))*$");
            var array = regex
              .Match(s)
              .Groups["item"]
              .Captures
              .Cast<Capture>()
              .Select(c => c.Value)
              .ToArray();
            return array;
        }

        private List<StationAndAvailability> LoadStationsFromHTML(string s, string cityName)
        {
            var result = new List<StationAndAvailability>();
            const string CDataStr = "exibirEstacaMapa(";
            
            int dataPos = s.IndexOf(CDataStr);
            if (dataPos > 0)
                s = s.Substring(dataPos, s.IndexOf("function exibirEstacaMapa(", dataPos) - dataPos);
            dataPos = s.IndexOf(CDataStr);
            string[] split = new string[] { "\",\"" };
            while (dataPos > -1)
            {
                dataPos += CDataStr.Length;
                var dataEndPos = s.IndexOf(");", dataPos) - 1;
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
                    station.Open = dataValues[6] == "EO";
                    var availability = new StationAvailability();
                    availability.Available = int.Parse(dataValues[7]);
                    availability.Free = int.Parse(dataValues[8]);
                    availability.Free -= availability.Available;
                    availability.Connected = dataValues[7] == "A";
                    availability.Open = station.Open;

                    result.Add(new StationAndAvailability(station, availability));
                }
                dataPos = s.IndexOf(CDataStr, dataPos);
            }

            return result;
        }

        private List<StationAndAvailability> LoadStationsFromHTML_RIO(string s, string cityName)
        {
            var result = new List<StationAndAvailability>();
            const string CCoordStr = "point = new GLatLng(";
            const string CDataStr = "criaPonto(point,";

            int dataPos = s.IndexOf(CCoordStr);
            if (dataPos > 0)
                s = s.Substring(dataPos, s.IndexOf("function criaPonto(", dataPos) - dataPos);
            dataPos = s.IndexOf(CCoordStr);
            string[] split = new string[] { "\",\"" };
            while (dataPos > -1)
            {
                dataPos += CCoordStr.Length;
                // read the coordinate
                var dataEndPos = s.IndexOf(");", dataPos);
                var dataStr = s.Substring(dataPos, dataEndPos - dataPos);
                var dataValues = dataStr.Split(',');

                var station = new StationLocation();
                station.Latitude = double.Parse(dataValues[0].Trim(), CultureInfo.InvariantCulture);
                station.Longitude = double.Parse(dataValues[1].Trim(), CultureInfo.InvariantCulture);

                dataPos = s.IndexOf(CDataStr, dataEndPos) + CDataStr.Length;
                dataEndPos = s.IndexOf(");", dataPos);
                dataStr = s.Substring(dataPos, dataEndPos - dataPos);
                dataValues = ParseFunctionParams(dataStr);

                int tmp = 0;
                if (int.TryParse(dataValues[0], out tmp))
                {
                    station.Number = tmp;
                    station.Name = dataValues[1];
                    station.Address = dataValues[2];
                    station.FullAddress = dataValues[3];
                    station.City = cityName;
                    station.Open = dataValues[7] == "EO";
                    var availability = new StationAvailability();

                    if (int.TryParse(dataValues[8], out tmp))
                        availability.Total = tmp;
                    if (int.TryParse(dataValues[9], out tmp))
                        availability.Available = tmp;
                    if (availability.Total < availability.Available)
                        availability.Total = availability.Available;
                    availability.Free = availability.Total - availability.Available;
                    availability.Connected = dataValues[6] == "A";
                    availability.Open = station.Open;

                    result.Add(new StationAndAvailability(station, availability));
                }
                dataPos = s.IndexOf(CCoordStr, dataPos);
            }

            return result;
        }
    }*/
    #endregion
}
