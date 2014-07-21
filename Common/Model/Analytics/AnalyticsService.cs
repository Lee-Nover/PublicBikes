using FlurryWP8SDK.Models;
using System.Collections.Generic;
using System.Device.Location;

namespace Bicikelj.Model.Analytics
{
    public class NullAnalyticsService : IAnalyticsService
    {
        public void LogEvent(string name) { }
        public void LogEvent(string name, string[] nameValuePairs) { }
        public void LogTimedEvent(string name) { }
        public void LogTimedEvent(string name, string[] nameValuePairs) { }
        public void EndTimedEvent(string name) { }
        public void EndTimedEvent(string name, string[] nameValuePairs) { }
        public void SetLocation(GeoCoordinate coordinate) { }
    }

    public static class ArrayToParamsExt
    {
        public static List<FlurryWP8SDK.Models.Parameter> ToParams(this string[] nameValuePairs)
        {
            var result = new List<FlurryWP8SDK.Models.Parameter>(nameValuePairs.Length / 2);

            for (int idxItem = 0; idxItem < nameValuePairs.Length - 1; idxItem += 2)
                result.Add(new FlurryWP8SDK.Models.Parameter(nameValuePairs[idxItem], nameValuePairs[idxItem + 1]));
            
            return result;
        }
    }

    public class AnalyticsService : IAnalyticsService
    {
        public void LogEvent(string name)
        {
            FlurryWP8SDK.Api.LogEvent(name);
            BugSense.BugSenseHandler.Instance.SendEventAsync(name);
        }

        public void LogEvent(string name, string[] nameValuePairs)
        {
            
            FlurryWP8SDK.Api.LogEvent(name, nameValuePairs.ToParams());
            BugSense.BugSenseHandler.Instance.SendEventAsync(name);
        }

        public void LogTimedEvent(string name)
        {
            FlurryWP8SDK.Api.LogEvent(name, true);
            BugSense.BugSenseHandler.Instance.SendEventAsync(name);
        }

        public void LogTimedEvent(string name, string[] nameValuePairs)
        {
            FlurryWP8SDK.Api.LogEvent(name, nameValuePairs.ToParams(), true);
            BugSense.BugSenseHandler.Instance.SendEventAsync(name);
        }

        public void EndTimedEvent(string name)
        {
            FlurryWP8SDK.Api.EndTimedEvent(name);
        }

        public void EndTimedEvent(string name, string[] nameValuePairs)
        {
            FlurryWP8SDK.Api.EndTimedEvent(name, nameValuePairs.ToParams());
        }

        public void SetLocation(GeoCoordinate coordinate)
        {
            FlurryWP8SDK.Api.SetLocation(coordinate.Latitude, coordinate.Longitude, 100);
        }
    }
}
