using System.Collections.Generic;

namespace Bicikelj.Model
{
    public static class BingMapsCredentials
    {
        public static string Key = "your app key";
    }

    public static class MapServiceCredentials
    {
        public static string ApplicationID = "your app key";
        public static string AuthenticationToken = "your app key";
    }

    public static class BugSenseCredentials
    {
        public static string Key = "your app key";
    }

    public static class AzureServiceCredentials
    {
        public static string Key
        {
            get { return AuthHeaders["x-zumo-application"]; }
            set { AuthHeaders["x-zumo-application"] = value; }
        }
        public static Dictionary<string, string> AuthHeaders;

        static AzureServiceCredentials()
        {
            AuthHeaders = new Dictionary<string, string>();
            AuthHeaders.Add("x-zumo-application", "your app key");
        }
    }

    public static class FlurryCredentials
    {
        public static string Key = "your app key";
    }

    public static class AdDealsCredentials
    {
        public static string ID = "xxx";
        public static string Key = "your app key";
    }

    public static class AdDuplexCredentials
    {
        public static string ID = "your app key";
    }
}
