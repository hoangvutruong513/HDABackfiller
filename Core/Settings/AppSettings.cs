using System.Configuration;

namespace Core.Settings
{
    public class AppSettings
    {
        public static string PICollectiveName => StringRetriever("PICollectiveName");
        public static string Path => StringRetriever("CSVLocation");
        private static string StringRetriever(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}
