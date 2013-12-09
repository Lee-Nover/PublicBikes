using System.Collections.Generic;

namespace Bicikelj.Model
{
    public static class BingMapsCredentials
    {
        public static string Key = "your app key";
    }

    public static class BugSenseCredentials
    {
        public static string Key = "your app key";
    }

    public static class AzureServiceCredentials
    {
        public static string Key = "your app key";
        public static Dictionary<string, string> AuthHeaders;

        static AzureServiceCredentials()
        {
            AuthHeaders = new Dictionary<string, string>();
            AuthHeaders.Add("x-zumo-application", Key);
        }
    }

    public static class FlurryCredentials
    {
        public static string Key = "your app key";
    }

    public static class AdDealsCredentials
    {
        public static string ID = "id";
        public static string Key = "your app key";
    }

    public static class AdDuplexCredentials
    {
        public static string ID = "your app key";
    }
}
