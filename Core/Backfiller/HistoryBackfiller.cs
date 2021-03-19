using Core.ConnectionManager;
using Core.FileReader;
using Core.ProgressReport;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Backfiller
{
    public class HistoryBackfiller : IHistoryBackfiller
    {
        private IPIConnectionManager _piCM;
        private PIServer _SitePI;
        private bool _IsConnected;
        private ILogger _logger;
        private AFTimeRange _backfillRange;
        private IReader _reader;
        private IList<string> _nameList;
        private int _totalCount;
        private IList<string> _errorList = new List<string>();

        // private variables for controlling Async Operations
        private SemaphoreSlim _throttler;
        private ProgressBar _progressBar;
        private int _progressCounter = 0;

        public HistoryBackfiller(IPIConnectionManager piCM, ILogger logger, IReader reader)
        {
            _piCM = piCM;
            _logger = logger;
            _reader = reader;
            _throttler = new SemaphoreSlim(3, 5);
        }

        public async Task automateBackfill()
        {
            // Retrieve connected PIServer from PIConnectionManager
            (_IsConnected, _SitePI) = _piCM.Connect();

            // Retrieve list of HDA PI Points from CSV and find those PI Points on the PI Data Server
            _nameList = _reader.readFile();
            _totalCount = _nameList.Count;
            // If _nameList is empty, terminate without doing anything
            if (_totalCount == 0)
            {
                _errorList.Add("List of PI Points is empty. Terminating Service");
                return;
            }

            // Request Backfill Time Range
            _backfillRange = _RequestBackfillTimeRange();

            // Create list of tasks for multi-threading the workload through the list of PI Points
            var tasks = new List<Task>();
            _progressBar = new ProgressBar();
            foreach (var HDAPIPointName in _nameList)
            {
                tasks.Add(_RetrieveAndBackfillAsync(HDAPIPointName));
                //await Task.Delay(2000);
            }
            await Task.WhenAll(tasks);

            // Wait for progress bar to reach 100% first before disposing
            await Task.Delay(10000);
            _progressBar.Dispose();
            
            return;
        }

        public void logErrors()
        {
            _logger.Information("SUCCESSFULLY BACKFILLED {0}/{1} of HDA PI POINTS FROM SELECTED CSV", (_totalCount - _errorList.Count), _totalCount);
            _logger.Information("LIST OF ERRORS ENCOUNTERED DURING BACKFILL");
            foreach (var error in _errorList)
            {
                _logger.Error("     {0}", error);
            }
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

        private async Task _RetrieveAndBackfillAsync(string HDAPIPointName)
        {
            // await for the _throttler to give a worker to the task. The worker is released in ReportAndRelease method
            await _throttler.WaitAsync();
            string message;

            // Try find HDA PI Point, if it cant be found, Report Error, Progress and Release throttler.
            PIPoint HDAPIPoint;
            if (!PIPoint.TryFindPIPoint(_SitePI, HDAPIPointName, out HDAPIPoint))
            {
                message = string.Format("HDA PI Point {0} not found", HDAPIPointName);
                ReportAndRelease(message);
                return;
            }

            // Try find DA PI Point, if it cant be found, Report Error, Progress and Release throttler.
            string DAPIPointName = _GetDAPIPointName(HDAPIPointName);
            PIPoint DAPIPoint;
            if (!PIPoint.TryFindPIPoint(_SitePI, DAPIPointName, out DAPIPoint))
            {
                message = string.Format("DA PI Point {0} not found", DAPIPointName);
                ReportAndRelease(message);
                return;
            }

            // Retrieve Recorded Values within backfill time range
            var retrieveDataTask = HDAPIPoint.RecordedValuesAsync(_backfillRange, AFBoundaryType.Inside, null, false);

            // Backfill retrieved data from HDA PI Point into the DA PI Point
            var backfillResult = await DAPIPoint.ReplaceValuesAsync(_backfillRange, await retrieveDataTask, AFBufferOption.Buffer);
            
            // if backfillResult != null, backfill failed; else if backfillResult = null, it succeeds
            if (backfillResult != null)
            {
                message = string.Format("Backfill from {0} to {1} failed", HDAPIPointName, DAPIPointName);
                ReportAndRelease(message);
                return;
            }
            else ReportAndRelease();
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

        private void ReportAndRelease(string message = null)
        {
            if(!string.IsNullOrEmpty(message))
            {
                _errorList.Add(message);
            }

            Interlocked.Increment(ref _progressCounter);
            _progressBar.Report((double)_progressCounter/_totalCount);
            _throttler.Release();
        }
    }
}
