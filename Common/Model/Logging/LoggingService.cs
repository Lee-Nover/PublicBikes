using BugSense;
using BugSense.Core.Model;
using System;

namespace Bicikelj.Model.Logging
{
    public class NullLoggingService : ILoggingService
    {
        public void LogError(Exception e, string comment, string commentKey = null) { }
    }

    public class LoggingService : ILoggingService
    {
        public void LogError(Exception e, string comment, string commentKey = null)
        {
            BugSenseHandler.Instance.ClearCrashExtraData();
            if (e.Data != null && e.Data.Contains("DetailedStack"))
            {
                var extraData = new CrashExtraData() { Key = "DetailedStack", Value = (string)e.Data["DetailedStack"] };
                BugSenseHandler.Instance.AddCrashExtraData(extraData);
            }
            if (!string.IsNullOrEmpty(comment))
            {
                if (string.IsNullOrEmpty(commentKey))
                    commentKey = "Description";
                var extraData = new CrashExtraData() { Key = commentKey, Value = comment };
                BugSenseHandler.Instance.AddCrashExtraData(extraData);
            }
            var wex = e as WebExceptionEx;
            if (wex != null)
                BugSenseHandler.Instance.SendExceptionAsync(wex, "RequestedURL", wex.URL);
            else
                BugSenseHandler.Instance.SendExceptionAsync(e);
            FlurryWP8SDK.Api.LogError(comment, e);
        }
    }
}
