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
        private IList<string> _csvData = new List<string>();

        public CsvReader(ILogger logger)
        {
            _logger = logger;
        }

        // show list of CSV files in the folder defined in AppSettings.Path
        private void showUserChoicesCsv()
        {
            _logger.Information("RETRIEVING FILES FROM {0}", _path);

            _fileList = Directory.GetFiles(_path,"*.csv");

            if (_fileList.Length > 0)
            {
                _logger.Information("LIST OF HDA TAGS CSV FILES AVAILABLE ...");
                for (int i = 0; i < _fileList.Length; i++)
                {
                    _logger.Information("     Choice {0}: {1}", i + 1, _fileList[i]);
                }
            }
            else
            {
                _logger.Error("THERE ARE NO CSV FILES IN THIS LOCATION");
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
                Console.Write("SELECT THE CSV FILE TO READ FROM (FROM 1 TO {0}): ", _fileList.Length);
                choice = Console.ReadLine();
            }

            _logger.Information("CSV file selected for backfill: {0}", _fileList[choiceInt-1]);

            return _fileList[choiceInt-1];
        }

        // Read and export the list of tags from the selected CSV File
        public IList<string> readFile()
        {
            try
            {
                // Retrieve files in folder, if empty, terminate reader and return empty list
                showUserChoicesCsv();
                if (_fileList.Length == 0) return _csvData;

                using var streamReader = File.OpenText(getUserChoiceCsv());
                using var csvReader = new CsvHelper.CsvReader(streamReader, CultureInfo.CurrentCulture);
                csvReader.Configuration.HasHeaderRecord = true;
                csvReader.Configuration.ShouldSkipRecord = row => row[0].Contains("HDA_TAGS");
                
                while (csvReader.Read())
                {
                    for (int i = 0; csvReader.TryGetField(i, out string value); i++)
                    {
                        _csvData.Add(value);
                    }
                }
                csvReader.Dispose();
                streamReader.Close();                
            } 
            catch (FileNotFoundException e)
            {
                _logger.Error("File does not exist in the program directory...");
                _logger.Error(e.Message);
            }
            catch (ArgumentNullException e)
            {
                _logger.Error("File path not found in App.config");
                _logger.Error(e.Message);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
            }
            return _csvData;
        }
    }
}
