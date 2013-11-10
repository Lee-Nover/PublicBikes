
using System.Collections.Generic;
namespace Bicikelj.Model
{
    public static class AzureServiceCredentials
    {
        public static string Key = "your-app-key";
        public static Dictionary<string, string> AuthHeaders;
        
        static AzureServiceCredentials()
        {
            AuthHeaders = new Dictionary<string, string>();
            AuthHeaders.Add("x-zumo-application", Key);
        }
    }
}
