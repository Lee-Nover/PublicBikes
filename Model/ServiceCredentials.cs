using System.Collections.Generic;

namespace Bicikelj.Model
{
    public static class BingMapsCredentials
    {
        public static string Key = "your api key";
    }

    public static class BugSenseCredentials
    {
        public static string Key = "your api key";
    }

    public static class AzureServiceCredentials
    {
        public static string Key = "your api key";
        public static Dictionary<string, string> AuthHeaders;

        static AzureServiceCredentials()
        {
            AuthHeaders = new Dictionary<string, string>();
            AuthHeaders.Add("x-zumo-application", Key);
        }
    }

    public static class FlurryCredentials
    {
        public static string Key = "your api key";
    }
}
