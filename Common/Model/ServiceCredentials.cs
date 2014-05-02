using System.Collections.Generic;

namespace Bicikelj.Model
{
    public static class BingMapsCredentials
    {
        public static string Key = "Aplf-Zr29iXS78kX-EacQV1PtQHf5o2vhmRjPa-t_SU0tIlbWMzf7_tppgLFkYPM";
    }

    public static class MapServiceCredentials
    {
        public static string ApplicationID = "d1332e8c-936e-4dd2-9049-1e4036e3314a";
        public static string AuthenticationToken = "KUQdtn9r3UTe3k0DnBVt8A";
    }

    public static class BugSenseCredentials
    {
        public static string Key = "134aca06";
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
            AuthHeaders.Add("x-zumo-application", "wEIDJCYsKkLxegYpdcOrEQpGYzsYCD62");
        }
    }

    public static class FlurryCredentials
    {
        public static string Key = "HHNHFFZ8KYJR8K797M78";
    }

    public static class AdDealsCredentials
    {
        public static string ID = "102";
        public static string Key = "PJCGOBDZLHTH";
    }

    public static class AdDuplexCredentials
    {
        public static string ID = "70635";
    }
}
