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

namespace Bicikelj.Model
{
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
                case "milano":
                    return "http://www.bikemi.com/localizaciones/localizaciones.php";
                default:
                    return "";
            }
        }

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Barcelona", Country = "Spain", ServiceName = "bicing", UrlCityName = "barcelona", Provider = Instance },
                new City(){ CityName = "Zaragoza", Country = "Spain", ServiceName = "bizi Zaragoza", UrlCityName = "zaragoza", Provider = Instance },
                new City(){ CityName = "Milano", Country = "Italy", ServiceName = "bikeMi", UrlCityName = "milano", AlternateCityName = "Milan", Provider = Instance }
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

                    switch (cityName)
                    {
                        case "milano":
                            var idxStart = cmdList.IndexOf("exml.parseString('") + 18;
                            var idxEnd = cmdList.IndexOf("</kml>", idxStart) + 6;
                            cmdList = cmdList.Substring(idxStart, idxEnd - idxStart);
                            sl = LoadStationsFromKML(cmdList, cityName);
                            break;
                        default:
                            sl = LoadStationsFromHTML(cmdList, cityName);
                            break;
                    }
                    
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

        private List<StationAndAvailability> LoadStationsFromKML(string stationsStr, string cityName)
        {
            if (string.IsNullOrWhiteSpace(stationsStr))
                return null;
            var idxNS = stationsStr.IndexOf("xmlns");
            var idxEnd = stationsStr.IndexOf(">", idxNS);
            stationsStr = stationsStr.Remove(idxNS, idxEnd - idxNS);
            XDocument doc = XDocument.Load(new System.IO.StringReader(stationsStr));
            /*
             * <description><![CDATA[<div style="margin:10px"><div style="font:bold 11px verdana;color:#ED1B24;margin-bottom:10px">
             * 01 Duomo - lato Via Mengoni, 0</div><div style="text-align:right;float:left;font:bold 11px verdana">Biciclette<br />Stalli</div><div style="margin-left:5px;float:left;font:bold 11px verdana;color:green">
             * 19<br />
             * 4<br /></div></div>]]></description>
             */
            var newIndex = 1000;
            var numbersOnly = new Regex("\\d+", RegexOptions.Compiled);
            var stations = new List<StationAndAvailability>();
            foreach (var s in doc.Descendants("Placemark"))
            {
                var desc = s.Descendants("description").FirstOrDefault().Value;
                var descNode = XDocument.Parse(desc);
                var nodeList = descNode.DescendantNodes().ToList();
                desc = nodeList[2].ToString();
                var coords = s.Descendants("coordinates").FirstOrDefault().Value.Split(',');
                var idxName = 0;
                while (idxName < desc.Length && char.IsNumber(desc[idxName]))
                    idxName++;
                
                var station = new StationLocation();
                if (idxName == 0)
                    station.Number = newIndex++;
                else
                {
                    station.Number = int.Parse(desc.Substring(0, idxName));
                    while (idxName < desc.Length && !char.IsLetter(desc[idxName]))
                        idxName++;
                }
                idxEnd = desc.LastIndexOf(',') - 1;
                if (idxEnd <= 0)
                    idxEnd = desc.Length;
                station.Name = desc.Substring(idxName, idxEnd - idxName + 1);
                station.Name = station.Name.Replace("\\", "");
                station.Latitude = double.Parse(coords[1], CultureInfo.InvariantCulture);
                station.Longitude = double.Parse(coords[0], CultureInfo.InvariantCulture);
                station.Open = true;
                station.City = cityName;

                var availability = new StationAvailability();
                desc = nodeList[8].ToString();
                availability.Available = int.Parse(numbersOnly.Match(desc).Value);
                desc = nodeList[10].ToString();
                availability.Free = int.Parse(numbersOnly.Match(desc).Value);
                availability.Open = true;
                
                stations.Add(new StationAndAvailability(station, availability));
            }

            return stations;
        }
    }
}
