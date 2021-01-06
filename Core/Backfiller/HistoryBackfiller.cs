using Core.ConnectionManager;
using Core.FileReader;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Core.Backfiller
{
    public class HistoryBackfiller : IHistoryBackfiller
    {
        private PIServer _SitePI;
        private bool _IsConnected;
        private ILogger _logger;
        private IList<PIPoint> _pipointList;
        private AFTimeRange _backfillRange;

        public HistoryBackfiller(IPIConnectionManager piCM, ILogger logger)
        {
            (_IsConnected, _SitePI) = piCM.Connect();
            _logger = logger;
        }

        public async Task automateBackfill()
        {
            // Retrieve list of HDA PI Points from CSV and find those PI Points on the PI Data Server
            IList<string> _nameList = CsvReader.readCsv();
            _pipointList = await PIPoint.FindPIPointsAsync(_SitePI, _nameList);

            // Request Backfill Time Range
            _backfillRange = _RequestBackfillTimeRange();

            // Create list of tasks for multi-threading the workload through the list of PI Points
            var tasks = new List<Task>();
            foreach (var point in _pipointList)
            {
                tasks.Add(_RetrieveAndBackfillAsync(point));
            }

            await Task.WhenAll(tasks);

            return;
        }

        private AFTimeRange _RequestBackfillTimeRange()
        {
            // Ask for User's input start time 
            var cultureInfo = new CultureInfo("en-US");
            Console.WriteLine("Input start time for backfill: ");
            string startTimeString = Console.ReadLine() + " +08";
            _logger.Information("Backfill Start Time: {0}", startTimeString);
            var startTime = DateTime.ParseExact(startTimeString, "dd-MMM-yyyy HH:mm:ss zz", cultureInfo);
            AFTime backfillStart = new AFTime(startTime);

            // Ask for User's input end time
            Console.WriteLine("Input end time for backfill: ");
            string endTimeString = Console.ReadLine() + " +08";
            _logger.Information("Backfill End Time: {0}", endTimeString);
            var endTime = DateTime.ParseExact(endTimeString, "dd-MMM-yyyy HH:mm:ss zz", cultureInfo);
            AFTime backfillEnd = new AFTime(endTime);

            // Construct an AF Time Range
            AFTimeRange backfillRange = new AFTimeRange(backfillStart, backfillEnd);
            return backfillRange;
        }

        private async Task _RetrieveAndBackfillAsync(PIPoint HDAPIPoint)
        {
            // Retrieve Recorded Values within backfill time range
            var retrieveDataTask = HDAPIPoint.RecordedValuesAsync(_backfillRange, AFBoundaryType.Inside, null, false);

            // Find corresponding DA PI Point
            string DAPIPointName = _GetDAPIPointName(HDAPIPoint.Name);
            var pipointDA = PIPoint.FindPIPoint(_SitePI, DAPIPointName);
            
            // Backfill retrieved data from HDA PI Point into the DA PI Point
            var backfillResult = await pipointDA.ReplaceValuesAsync(_backfillRange, await retrieveDataTask, AFBufferOption.Buffer);

            // Log Backfill Result for the PI Point
            _logger.Information("PI Point: {0}; Success: {1}", DAPIPointName, backfillResult == null ? true : false);

            return;
        }

        private string _GetDAPIPointName(string HDAPIPointName)
        {
            // Get the name of the DA PIPoint by removing the "_HDA" portion.
            var lastIndex = HDAPIPointName.Length - 1;
            return HDAPIPointName.Remove(lastIndex - 3);
        }
    }
}
