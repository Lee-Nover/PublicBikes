using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bicikelj.Model
{
    public class ClearChannel2Service : BikeServiceProvider
    {
        public static ClearChannel2Service Instance = new ClearChannel2Service();

        private static string StationListUrl(string cityName)
        {
            switch (cityName)
            {
                case "antwerpen":
                    return "https://www.velo-antwerpen.be/localizaciones/station_map.php";
                case "mexicocity":
                    return "https://www.ecobici.df.gob.mx/localizaciones/localizaciones_body.php";
                default:
                    return "";
            }
            
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

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Antwerpen", Country = "Belgium", ServiceName = "velo", UrlCityName = "antwerpen", Provider = Instance },
                new City(){ CityName = "Mexico City", Country = "Mexico", ServiceName = "ecobici", UrlCityName = "mexicocity", Provider = Instance }
            };
            return result;
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            var urlData = string.Format("idStation={0}&s_id_idioma=en", station.Number);
            return DownloadUrl.PostAsync(StationInfoUrl(station.City), urlData, station)
                .ObserveOn(ThreadPoolScheduler.Instance)
                .Select(s => {
                    var availability = LoadAvailabilityFromHTML(s.Object);
                    availability.Open = station.Open;
                    return new StationAndAvailability(station, availability);
                });
        }

        public override IObservable<List<StationAndAvailability>> DownloadStationsWithAvailability(string cityName)
        {
            return DownloadUrl.GetAsync(StationListUrl(cityName))
                .Select(s =>
                {
                    var sl = LoadStationsFromHTML(s, cityName);
                    return sl;
                });
        }

        private List<StationAndAvailability> LoadStationsFromHTML(string s, string cityName)
        {
            var result = new List<StationAndAvailability>();

            const string CCoordStr = "point = new GLatLng(";
            const string CDataStr = "data:\"";

            int dataPos = 0;
            int coordPos = s.IndexOf(CCoordStr);
            if (coordPos > 0)
            {
                var endScriptPos = s.IndexOf("</script>", coordPos);
                if (endScriptPos < coordPos)
                    endScriptPos = s.Length;
                s = s.Substring(coordPos, endScriptPos - coordPos);
            }
            coordPos = s.IndexOf(CCoordStr);
            while (coordPos > -1)
            {
                coordPos += CCoordStr.Length;
                // new GLatLng(51.199306000000000000,4.390135000000000000);
                var coordEndPos = s.IndexOf(");", coordPos);
                var coordStr = s.Substring(coordPos, coordEndPos - coordPos);
                var coords = coordStr.Split(',');
                // antwerpen:    data:"idStation=12&addressnew=MDEyIC0gQnJ1c3NlbA==&s_id_idioma=en",
                // mexico city:  data:"idStation="+89+"&addressnew=ODkgUkVQVUJMSUNBIERFIEdVQVRFTUFMQSAtIE1PTlRFIERFIFBJRURBRA=="+"&s_id_idioma="+"es"
                dataPos = s.IndexOf(CDataStr, coordEndPos) + CDataStr.Length;
                var dataEndPos = s.IndexOf("\",", dataPos);
                var dataStr = s.Substring(dataPos, dataEndPos - dataPos);
                var dataValues = dataStr.Replace("\"", string.Empty).Replace("+", string.Empty).Split('&');

                var station = new StationLocation();
                station.Latitude = double.Parse(coords[0], CultureInfo.InvariantCulture);
                station.Longitude = double.Parse(coords[1], CultureInfo.InvariantCulture);
                var value = dataValues[0].Split('=')[1];
                station.Number = int.Parse(value);
                value = dataValues[1].Remove(0, dataValues[1].IndexOf('=') + 1);
                var bytes = Convert.FromBase64String(value);
                station.Address = UTF8Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                station.City = cityName;
                station.Address = HttpUtility.HtmlDecode(station.Address);
                station.Name = station.Address;
                result.Add(new StationAndAvailability(station, null));

                coordPos = s.IndexOf(CCoordStr, dataPos);
            }

            return result;
        }

        private StationAvailability LoadAvailabilityFromHTML(string s)
        {
            string availStr = "0";
            string slotsStr = "0";
            var bikePos = s.IndexOf("Bicycles", StringComparison.InvariantCultureIgnoreCase);
            // todo: deleting one char at a time is really stupid and slow
            // use a regex with split(); var numbersOnly = new Regex("\\d+", RegexOptions.Compiled);
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
                bikePos = s.IndexOf("Slots", StringComparison.InvariantCultureIgnoreCase);
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
}
