using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;


namespace Bicikelj.Model
{
    public class BCycleService : BikeServiceProvider
    {
        public static BCycleService Instance = new BCycleService();

        private static string StationListUrl = "http://{0}.bcycle.com/home.aspx";

        // todo: add availability caching and read from cache until some time pases, eg. 5 min

        protected override IList<City> GetCities()
        {
            var result = new List<City>() {
                new City(){ CityName = "Battle Creek", Country = "USA", ServiceName = "B-cycle", UrlCityName = "battlecreek", Provider = Instance },
                new City(){ CityName = "Boulder", Country = "USA", ServiceName = "B-cycle", UrlCityName = "boulder", Provider = Instance },
                new City(){ CityName = "Broward", Country = "USA", ServiceName = "B-cycle", UrlCityName = "broward", Provider = Instance },
                new City(){ CityName = "Charlotte", Country = "USA", ServiceName = "B-cycle", UrlCityName = "charlotte", Provider = Instance },
                new City(){ CityName = "Denver", Country = "USA", ServiceName = "B-cycle", UrlCityName = "denver", Provider = Instance },
                new City(){ CityName = "Des Moines", Country = "USA", ServiceName = "B-cycle", UrlCityName = "desmoines", Provider = Instance },
                new City(){ CityName = "Fort Worth", Country = "USA", ServiceName = "B-cycle", UrlCityName = "fortworth", Provider = Instance },
                new City(){ CityName = "Greenville", Country = "USA", ServiceName = "B-cycle", UrlCityName = "greenville", Provider = Instance },
                new City(){ CityName = "Hawaii", Country = "USA", ServiceName = "B-cycle", UrlCityName = "hawaii", Provider = Instance },
                new City(){ CityName = "Houston", Country = "USA", ServiceName = "B-cycle", UrlCityName = "houston", Provider = Instance },
                new City(){ CityName = "Kansas City", Country = "USA", ServiceName = "B-cycle", UrlCityName = "kansascity", Provider = Instance },
                new City(){ CityName = "Madison", Country = "USA", ServiceName = "B-cycle", UrlCityName = "madison", Provider = Instance },
                new City(){ CityName = "Milwaukee", Country = "USA", ServiceName = "B-cycle", UrlCityName = "milwaukee", Provider = Instance },
                new City(){ CityName = "Nashville", Country = "USA", ServiceName = "B-cycle", UrlCityName = "nashville", Provider = Instance },
                new City(){ CityName = "Omaha", Country = "USA", ServiceName = "B-cycle", UrlCityName = "omaha", Provider = Instance },
                new City(){ CityName = "San Antonio", Country = "USA", ServiceName = "B-cycle", UrlCityName = "sanantonio", Provider = Instance },
                new City(){ CityName = "Spartanburg", Country = "USA", ServiceName = "B-cycle", UrlCityName = "spartanburg", Provider = Instance }
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
                .Select(s =>
                {
                    List<StationAndAvailability> sl;
                    sl = LoadStationsFromHTML(s, cityName);
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
            /*
            List<object> result = new List<object>();

            int lastCopiedIdx = 0;
            bool insideQuote = false;
            char? currentQuoteChar = null;
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (c == '\'' || c == '"')
                {
                    if (currentQuoteChar.HasValue && currentQuoteChar.Value == c)
                    {
                        insideQuote = false;
                        result.Add(s.Substring(lastCopiedIdx + 1, i - lastCopiedIdx - 1));
                        lastCopiedIdx = i;
                    }
                    else if (!currentQuoteChar.HasValue)
                        currentQuoteChar = c;
                }
            }
            
            return result;*/
        }

        private List<StationAndAvailability> LoadStationsFromHTML(string s, string cityName)
        {
            /*
             
            var icon = '../Portals/10/images/maps/marker-outofservice.png';
            var back = 'infowin-unavail';
            var point = new google.maps.LatLng(40.01630, -105.28230);
            kioskpoints.push(point);
            var marker = new createMarker(point, "<div class='location'><strong>10th & Walnut</strong><br />10th St. & Walnut St.<br />Boulder, CO 80302</div><div class='avail'>Bikes available: <strong>5</strong><br />Docks available: <strong>6</strong></div><div></div>", icon, back);
            markers.push(marker);

            */

            var result = new List<StationAndAvailability>();

            const string CCoordStr = "point = new google.maps.LatLng(";
            const string CDataStr = "createMarker(point, \"";
            const string CDataEndStr = "\", icon, back);";
            int index = 1;
            int dataPos = 0;
            int coordPos = s.IndexOf("function LoadKiosks()");
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
                var coordEndPos = s.IndexOf(");", coordPos);
                var coordStr = s.Substring(coordPos, coordEndPos - coordPos);
                var coords = coordStr.Split(',');
                
                dataPos = s.IndexOf(CDataStr, coordEndPos) + CDataStr.Length;
                var dataEndPos = s.IndexOf(CDataEndStr, dataPos);
                var dataStr = s.Substring(dataPos, dataEndPos - dataPos);
                dataStr = dataStr.Replace("&", "&amp;");
                dataStr = "<div>" + dataStr + "</div>";
                var xml = XDocument.Parse(dataStr);

                var station = new StationLocation();
                station.Number = index++;
                station.Latitude = double.Parse(coords[0].Trim(), CultureInfo.InvariantCulture);
                station.Longitude = double.Parse(coords[1].Trim(), CultureInfo.InvariantCulture);
                
                //xml
                // read the name and address and availability from the xml
                // name = //div[@class='location']/strong/text()
                // address = //div[@class='location']/text()
                var loc = xml.Descendants("div").Where(el => (string)el.Attribute("class") == "location").FirstOrDefault();
                station.Name = loc.Descendants("strong").FirstOrDefault().Value;
                station.Address = loc.Value.Remove(0, station.Name.Length);
                var availability = new StationAvailability();
                availability.Connected = true;
                availability.Open = true;
                // bikes = //div[@class='avail']/strong[1]/text()
                // docks = //div[@class='avail']/strong[2]/text()
                var avail = xml.Descendants("div").Where(el => (string)el.Attribute("class") == "avail").Descendants("strong").ToArray();
                availability.Available = int.Parse(avail[0].Value);
                availability.Free = int.Parse(avail[1].Value);
                availability.Total = availability.Available + availability.Free;
                result.Add(new StationAndAvailability(station, availability));

                coordPos = s.IndexOf(CCoordStr, dataPos);
            }

            return result;
        }
    }
}
