using Core.ConnectionManager;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Backfiller
{
    public class HistoryBackfiller : IHistoryBackfiller
    {
        private PIServer _SitePI;
        private bool _IsConnected;
        private ILogger _logger;
        private IList<string> nameList;

        public HistoryBackfiller(IPIConnectionManager piCM, ILogger logger)
        {
            (_IsConnected, _SitePI) = piCM.Connect();
            _logger = logger;
        }

        public void automateBackfill()
        {
            requestBackfillTimeRange();


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
            _logger.Information("Backfill Start Time: {0}", startTimeString);
            var endTime = DateTime.ParseExact(endTimeString, "dd-MMM-yyyy HH:mm:ss zz", cultureInfo);
            AFTime backfillEnd = new AFTime(endTime);

            // Construct an AF Time Range
            AFTimeRange backfillRange = new AFTimeRange(backfillStart, backfillEnd);
            return backfillRange;
        }
    }
}
