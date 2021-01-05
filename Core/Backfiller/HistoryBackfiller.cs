using Core.ConnectionManager;
using OSIsoft.AF;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Backfiller
{
    public class HistoryBackfiller : IHistoryBackfiller
    {
        private PIServer _SitePI;
        private bool _IsConnected;
        private ILogger _logger;
        //private IList<string> _nameList = new List<string> {"SIM_Tag1_HDA", "SIM_Tag2_HDA"};
        private IList<string> _nameList = CsvReader.CsvReader.readCsv();
        private IList<PIPoint> _pipointList;
        private AFTimeRange _backfillRange;

        public HistoryBackfiller(IPIConnectionManager piCM, ILogger logger)
        {
            (_IsConnected, _SitePI) = piCM.Connect();
            _logger = logger;
        }

        public async Task automateBackfill()
        {
            // Request and show PI Point List
            _pipointList = await PIPoint.FindPIPointsAsync(_SitePI, _nameList);

            // Request Backfill Time Range
            _backfillRange = requestBackfillTimeRange();

            // Get Recorded Values for points in the PI Point List
            var allTasks = new List<Task<AFValues>>();
            foreach (var point in _pipointList)
            {
                allTasks.Add(point.RecordedValuesAsync(_backfillRange, AFBoundaryType.Inside, null, false));
            }
            var results = await Task.WhenAll(allTasks);

            // Backfill the data from results into the relevant DA PI Points
            var allTasksDA = new List<Task<AFErrors<AFValue>>>();
            foreach (var result in results)
            {
                // Get the name of the DA PIPoint by removing the "_HDA" portion.
                var lastIndex = result.PIPoint.Name.Length - 1;
                string pointNameDA = result.PIPoint.Name.Remove(lastIndex - 3);

                var pipointDA = PIPoint.FindPIPoint(_SitePI, pointNameDA);
                allTasksDA.Add(pipointDA.ReplaceValuesAsync(_backfillRange, result));
            }
            var resultsDA = await Task.WhenAll(allTasksDA);
            
            // Aggregate the List of PIPoint names with the List of Backfill Results
            var aggregrateResults = _nameList.Zip(resultsDA, (a, b) => new
            {
                name = a,
                result = b
            });
                                                                      
            // Output the backfill results
            foreach (var ar in aggregrateResults)
            {
                // The PiPoint.ReplaceValuesAsync return a Task<AFErrors<AFValue>> which resolve to a null if replacement is successful and resolve to an AFErrors<AFValue> if replacement fail
                if (ar.result == null) _logger.Information("Historical Backfill successful for tag {0}", ar.name);
                else _logger.Error("Historical Backfill failed for tag {0}", ar.name);
            }
        }

        private AFTimeRange requestBackfillTimeRange()
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
    }
}
