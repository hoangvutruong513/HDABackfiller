using Core.ConnectionManager;
using Core.FileReader;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Backfiller
{
    public class HistoryBackfiller : IHistoryBackfiller
    {
        private PIServer _SitePI;
        private bool _IsConnected;
        private ILogger _logger;
        private AFTimeRange _backfillRange;
        private SemaphoreSlim _throttler;

        public HistoryBackfiller(IPIConnectionManager piCM, ILogger logger)
        {
            (_IsConnected, _SitePI) = piCM.Connect();
            _logger = logger;
            _throttler = new SemaphoreSlim(3, 5);
        }

        public async Task automateBackfill()
        {
            // Retrieve list of HDA PI Points from CSV and find those PI Points on the PI Data Server
            IList<string> _nameList = CsvReader.readCsv(_logger);
            var _pipointListTask = PIPoint.FindPIPointsAsync(_SitePI, _nameList);

            // Request Backfill Time Range
            _backfillRange = _RequestBackfillTimeRange();

            // Create list of tasks for multi-threading the workload through the list of PI Points
            var tasks = new List<Task>();
            foreach (var point in await _pipointListTask)
            {
                tasks.Add(_RetrieveAndBackfillAsync(point));
            }

            await Task.WhenAll(tasks);

            return;
        }

        private AFTimeRange _RequestBackfillTimeRange()
        {
            AFTime backfillStart, backfillEnd;

            // Ask for User's input start time 
            
            Console.WriteLine("PLEASE ONLY INPUT DATE AND TIME IN dd-mmm-yyyy HH:mm:ss (24hr) FORMAT");
            Console.WriteLine("Input start time for backfill: ");
            backfillStart = enforceInputFormat();

            Console.WriteLine("Input end time for backfill: ");
            backfillEnd = enforceInputFormat();

            // Construct an AF Time Range
            AFTimeRange backfillRange = new AFTimeRange(backfillStart, backfillEnd);
            _logger.Information("Backfill Time Range: {0}", backfillRange);
            return backfillRange;
        }

        private async Task _RetrieveAndBackfillAsync(PIPoint HDAPIPoint)
        {
            await _throttler.WaitAsync();

            // Retrieve Recorded Values within backfill time range
            var retrieveDataTask = HDAPIPoint.RecordedValuesAsync(_backfillRange, AFBoundaryType.Inside, null, false);

            // Find corresponding DA PI Point
            string DAPIPointName = _GetDAPIPointName(HDAPIPoint.Name);
            var pipointDA = PIPoint.FindPIPoint(_SitePI, DAPIPointName);
            
            // Backfill retrieved data from HDA PI Point into the DA PI Point
            var backfillResult = await pipointDA.ReplaceValuesAsync(_backfillRange, await retrieveDataTask, AFBufferOption.Buffer);

            // Log Backfill Result for the PI Point
            _logger.Information("PI Point: {0}; Success: {1}", DAPIPointName, backfillResult == null ? true : false);

            _throttler.Release();

            return;
        }

        private string _GetDAPIPointName(string HDAPIPointName)
        {
            // Get the name of the DA PIPoint by removing the "_HDA" portion.
            var lastIndex = HDAPIPointName.Length - 1;
            return HDAPIPointName.Remove(lastIndex - 3);
        }

        private AFTime enforceInputFormat()
        {
            var cultureInfo = new CultureInfo("en-US");
            string timeString;
            DateTime timeObj = DateTime.Now;

            bool correctInput = false;
            while (correctInput == false)
            {
                timeString = (Console.ReadLine().Trim() + " +08");
                try
                {
                    timeObj = DateTime.ParseExact(timeString, "dd-MMM-yyyy HH:mm:ss zz", cultureInfo);
                    _logger.Information("Input accepted: {0}", timeString);
                    correctInput = true;
                }
                catch(Exception e)
                {
                    Console.WriteLine("Wrong time format. Please enter again:");
                }
            }
            return new AFTime(timeObj);
        }
    }
}
