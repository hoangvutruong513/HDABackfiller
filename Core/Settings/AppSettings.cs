using System.Configuration;

namespace Core.Settings
{
    public class AppSettings
    {
        public static string PICollectiveName => StringRetriever("PICollectiveName");
        public static string hdaTagsCSVLocation1 => StringRetriever("hdaTagsCsvLocation1");
        public static string hdaTagsCSVLocation2 => StringRetriever("hdaTagsCsvLocation2");
        public static string hdaTagsCSVLocation3 => StringRetriever("hdaTagsCsvLocation3");
        public static string hdaTagsCSVLocation4 => StringRetriever("hdaTagsCsvLocation4");
        public static string hdaTagsCSVLocation5 => StringRetriever("hdaTagsCsvLocation5");
        public static string hdaTagsCSVLocation6 => StringRetriever("hdaTagsCsvLocation6");

        private static string StringRetriever(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}
