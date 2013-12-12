
namespace Bicikelj.Model.Analytics
{
    public class NullAnalyticsService : IAnalyticsService
    {
        public void LogEvent(string name) { }
    }

    public class AnalyticsService : IAnalyticsService
    {
        public void LogEvent(string name)
        {
            FlurryWP8SDK.Api.LogEvent(name);
            BugSense.BugSenseHandler.Instance.SendEvent(name);
        }
    }
}
