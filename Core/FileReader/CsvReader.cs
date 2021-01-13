using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using Core.Settings;

namespace Core.FileReader
{
    public class CsvReader
    {
        private static Dictionary<int, string> CSVLocationMap = new Dictionary<int, string>()
        {
            {1,  AppSettings.hdaTagsCSVLocation1},
            {2,  AppSettings.hdaTagsCSVLocation2},
            {3,  AppSettings.hdaTagsCSVLocation3},
            {4,  AppSettings.hdaTagsCSVLocation4},
            {5,  AppSettings.hdaTagsCSVLocation5},
            {6,  AppSettings.hdaTagsCSVLocation6},
        };

        private static string getUserChoiceCsv()
        {
            string choice = "";
            int choiceInt;
            Console.WriteLine("Below is the list of hda tag csv files available... ");
            foreach (KeyValuePair<int, string> kvp in CSVLocationMap)
            {
                if (kvp.Value != null)
                {
                    Console.WriteLine("Choice {0}: {1}", kvp.Key, kvp.Value);
                }                
            }
            while (!int.TryParse(choice, out choiceInt) || choiceInt < 1 || choiceInt > 6)
            {
                // keep asking for user input if input is invalid
                // for e.g. not an integer, integer not from 1 to 6
                Console.Write("Please choose the csv file to read (enter a valid number, from 1 to 6): ");
                choice = Console.ReadLine();
            }

            return CSVLocationMap[choiceInt];          
        }
        public static List<string> readCsv()
        {
            List<string> csvData = new List<string>();
            try
            {
                using var streamReader = File.OpenText(getUserChoiceCsv());
                using var csvReader = new CsvHelper.CsvReader(streamReader, CultureInfo.CurrentCulture);
                csvReader.Configuration.HasHeaderRecord = true;
                csvReader.Configuration.ShouldSkipRecord = row => row[0].Contains("HDA_TAGS");
                
                while (csvReader.Read())
                {
                    for (int i = 0; csvReader.TryGetField(i, out string value); i++)
                    {
                        csvData.Add(value);
                    }
                }
                csvReader.Dispose();
                streamReader.Close();                
            } 
            catch (FileNotFoundException e)
            {
                Console.WriteLine("File does not exist in the program directory...");
                Console.WriteLine(e.Message);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("File path not found in App.config");
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return csvData;
        }
    }
}
