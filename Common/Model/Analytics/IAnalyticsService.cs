using FlurryWP8SDK.Models;
using System.Collections.Generic;
using System.Device.Location;

namespace Bicikelj.Model.Analytics
{
    public interface IAnalyticsService
    {
        void LogEvent(string name);
        void LogEvent(string name, string[] nameValuePairs);
        void LogTimedEvent(string name);
        void LogTimedEvent(string name, string[] nameValuePairs);
        void EndTimedEvent(string name);
        void EndTimedEvent(string name, string[] nameValuePairs);
        void SetLocation(GeoCoordinate coordinate);
    }
}
