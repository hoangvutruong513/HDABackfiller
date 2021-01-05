using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using Core.Settings;

namespace Core.FileReader
{
    public class CsvReader
    {
        private static string csvLocation = AppSettings.hdaTagsCSVLocation;
        public static List<string> readCsv()
        {
            using var streamReader = File.OpenText(csvLocation);
            using var csvReader = new CsvHelper.CsvReader(streamReader, CultureInfo.CurrentCulture);
            csvReader.Configuration.HasHeaderRecord = true;
            csvReader.Configuration.ShouldSkipRecord = row => row[0].Contains("HDA_TAGS");
            List<string> csvData = new List<string>();

            while (csvReader.Read())
            {
                for (int i = 0; csvReader.TryGetField(i, out string value); i++)
                {
                    csvData.Add(value);
                }
            }
            return csvData;
        }
    }
}
