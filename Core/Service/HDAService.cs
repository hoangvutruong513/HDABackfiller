using Core.Backfiller;
using Core.ConnectionManager;
using Core.Settings;
using OSIsoft.AF.PI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class HDAService : IHDAService
    {
        private ILogger _logger;
        private IPIConnectionManager _piCM;
        private IHistoryBackfiller _backfiller;
        private PIServer _SitePI;
        private bool _IsConnected;

        public HDAService(IPIConnectionManager piCM, ILogger logger, IHistoryBackfiller backfiller)
        {
            _piCM = piCM;
            _logger = logger;
            _backfiller = backfiller;
        }

        public void Start()
        {
            _logger.Information("History Backfill Service started successfully");
            (_IsConnected, _SitePI) = _piCM.Connect();
            
            // If cannot connecto to PI Data Collective, return to terminate console app
            if (!_IsConnected) return;
            else
            {

            }
        }

        public void Stop()
        {
            _piCM.Disconnect();
            _logger.Information("History Backfill Service completed");
        }
    }
}
