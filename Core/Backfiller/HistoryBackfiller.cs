using Core.ConnectionManager;
using OSIsoft.AF.PI;
using Serilog;
using System;
using System.Collections.Generic;
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

        public HistoryBackfiller(IPIConnectionManager piCM, ILogger logger)
        {
            (_IsConnected, _SitePI) = piCM.Connect();
            _logger = logger;
        }
    }
}
