using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace Bicikelj.Model
{
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
                .Select<string, List<StationAndAvailability>>(s =>
                {
                    var sl = LoadStationsFromHTML(s, cityName);
                    UpdateAvailabilityCache(sl);
                    return sl;
                });
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

        private List<StationAndAvailability> LoadStationsFromHTML_RIO(string s, string cityName)
        {
            var result = new List<StationAndAvailability>();
            const string CDataStr = "point = new GLatLng(";

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
}
