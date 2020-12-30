using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Settings
{
    public class AppSettings
    {
        public static string PICollectiveName => StringRetriever("PICollectiveName");

        private static string StringRetriever(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}
