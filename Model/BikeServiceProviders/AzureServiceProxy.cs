using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Bicikelj.Model
{
    public class AzureServiceProxy : BikeServiceProvider
    {
        protected string AzureServiceName;

        protected string GetStationDetailsUri(string city, string stationId)
        {
            return string.Format("https://publicbikes.azure-mobile.net/api/stations/{0}/{1}/{2}", AzureServiceName, city, stationId);
        }

        protected string GetStationListUri(string city)
        {
            return string.Format("https://publicbikes.azure-mobile.net/api/stations/{0}/{1}", AzureServiceName, city);
        }

        public override IObservable<StationAndAvailability> GetAvailability2(StationLocation station)
        {
            var availability = GetAvailabilityFromCache(station);
            if (availability.Availability != null)
                return Observable.Return<StationAndAvailability>(availability);
            else
                return DownloadUrl.GetAsync<AzureService.StationInfo>(GetStationDetailsUri(station.City, station.Number.ToString()), AzureServiceCredentials.AuthHeaders)
                    .ObserveOn(ThreadPoolScheduler.Instance)
                    .Select(s =>
                    {
                        var sa = new StationAndAvailability(station, s.GetAvailability());
                        UpdateAvailabilityCacheItem(sa);
                        return sa;
                    });
        }

        public override IObservable<List<StationAndAvailability>> DownloadStationsWithAvailability(string cityName)
        {
            return DownloadUrl.GetAsync<List<AzureService.StationInfo>>(GetStationListUri(cityName), AzureServiceCredentials.AuthHeaders)
                .Select(s =>
                {
                    var sl = LoadStationsFromSI(s, cityName);
                    UpdateAvailabilityCache(sl);
                    return sl;
                });
        }

        private static List<StationAndAvailability> LoadStationsFromSI(List<AzureService.StationInfo> s, string cityName)
        {
            var result = s.Select(si =>
            {
                var sa = new StationAndAvailability()
                {
                    Station = si.GetStation(),
                    Availability = si.GetAvailability()
                };
                if (string.IsNullOrEmpty(sa.Station.City))
                    sa.Station.City = cityName;
                return sa;
            }).ToList();
            return result;
        }
    }
}
