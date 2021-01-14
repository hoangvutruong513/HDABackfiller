using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using Core.Settings;
using Serilog;

namespace Core.FileReader
{
    public class CsvReader : IReader
    {
        private ILogger _logger;
        private string _path = AppSettings.Path;
        private string[] _fileList;

        public CsvReader(ILogger logger)
        {
            _logger = logger;
        }

        // show list of CSV files in the folder defined in AppSettings.Path
        private void showUserChoicesCsv()
        {
            _logger.Information("Retrieving file from {0}", _path);

            _fileList = Directory.GetFiles(_path,"*.csv");

            _logger.Information("List of HDA Tags CSV files available ...");
            for (int i = 0; i < _fileList.Length; i++)
            {
                _logger.Information("     Choice {0}: {1}", i+1, _fileList[i]);
            }
        }

        // get the name of the file selected by the user
        private string getUserChoiceCsv()
        {
            string choice = "";
            int choiceInt;
            
            while (!int.TryParse(choice, out choiceInt) || choiceInt < 1 || choiceInt > _fileList.Length)
            {
                // keep asking for user input if input is invalid
                // for e.g. not an integer, integer not from 1 to 6
                Console.Write("Please select the csv file to read from (enter a valid number, from 1 to {0}): ", _fileList.Length);
                choice = Console.ReadLine();
            }

            _logger.Information("HDA tag CSV file selected for backfill is: {0}", _fileList[choiceInt-1]);

            return _fileList[choiceInt-1];
        }

        // Read and export the list of tags from the selected CSV File
        public IList<string> readFile()
        {
            showUserChoicesCsv();

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
