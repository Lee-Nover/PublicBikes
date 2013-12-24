using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using ServiceStack.Text;
using System.Xml.Linq;
using System.Globalization;
using System.Net;
using System.Diagnostics;
using System.Reactive.Concurrency;

namespace Bicikelj.Model
{
    public class SmartBikeService : AzureServiceProxy
    {
        public SmartBikeService()
        {
            AzureServiceName = "smartbike";
        }

        public static SmartBikeService Instance = new SmartBikeService();

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Milano", Country = "Italy", ServiceName = "bikeMi", UrlCityName = "milano", AlternateCityName = "Milan", Provider = Instance },
                new City(){ CityName = "Antwerpen", Country = "Belgium", ServiceName = "velo", UrlCityName = "antwerpen", AlternateCityName = "antwerp", Provider = Instance }
            };
            return result;
        }

        private static string StationInfoUrl(string cityName)
        {
            switch (cityName)
            {
                case "antwerpen":
                    return "https://www.velo-antwerpen.be/CallWebService/StationBussinesStatus.php";
                case "mexicocity":
                    return "https://www.ecobici.df.gob.mx/CallWebService/StationBussinesStatus.php";
                default:
                    return "";
            }
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            switch (station.City)
            {
                case "antwerpen":
                case "mexicocity":
                    var csa = GetAvailabilityFromCache(station);
                    if (csa.Availability != null)
                        return Observable.Return<StationAndAvailability>(csa);
                    var urlData = string.Format("idStation={0}&s_id_idioma=en", station.Number);
                    return DownloadUrl.PostAsync(StationInfoUrl(station.City), urlData, station)
                        .ObserveOn(ThreadPoolScheduler.Instance)
                        .Select(s =>
                        {
                            var availability = LoadAvailabilityFromHTML(s.Object);
                            availability.Open = station.Open;
                            availability.Connected = true;
                            var sa = new StationAndAvailability(station, availability);
                            UpdateAvailabilityCacheItem(sa);
                            return sa;
                        });
                default:
                    return base.GetAvailability2(station);
            }
        }

        private StationAvailability LoadAvailabilityFromHTML(string s)
        {
            var bikePos = s.IndexOf("Bicycles", StringComparison.InvariantCultureIgnoreCase);
            if (bikePos > 0)
                s = s.Substring(bikePos);
            var numbersOnly = new Regex("\\d+", RegexOptions.Compiled);
            var matches = numbersOnly.Matches(s);
            var sa = new StationAvailability();
            sa.Available = int.Parse(matches[0].Value);
            sa.Free = int.Parse(matches[1].Value);
            sa.Total = sa.Free + sa.Available;
            return sa;
        }
    }

    public class ClearChannelService : BikeServiceProvider
    {
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

        public static ClearChannelService Instance = new ClearChannelService();

        private static string StationListUrl_ = "https://{0}/formmap/getJsonObject";

        private static string StationListUrl(string cityName)
        {
            switch (cityName)
            {
                case "barcelona":
                    return string.Format(StationListUrl_, "www.bicing.cat/ca");
                case "zaragoza":
                    return string.Format(StationListUrl_, "www.bizizaragoza.com/es");
                default:
                    return "";
            }
        }

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Barcelona", Country = "Spain", ServiceName = "bicing", UrlCityName = "barcelona", Provider = Instance },
                new City(){ CityName = "Zaragoza", Country = "Spain", ServiceName = "bizi Zaragoza", UrlCityName = "zaragoza", Provider = Instance },
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
                .Select(cmdList =>
                {
                    List<StationAndAvailability> sl;
                    sl = LoadStationsFromHTML(cmdList, cityName);
                    UpdateAvailabilityCache(sl);
                    return sl;
                });
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
                station.City = cityName;
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
    }
}
