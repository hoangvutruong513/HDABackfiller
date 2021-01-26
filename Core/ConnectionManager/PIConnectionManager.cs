using Core.Settings;
using OSIsoft.AF.PI;
using Serilog;
using System;

namespace Core.ConnectionManager
{
    public class PIConnectionManager : IPIConnectionManager
    {
        private PIServer _SitePI;
        private ILogger _logger;
        private string _PICollectiveName = AppSettings.PICollectiveName;

        public PIConnectionManager(ILogger logger)
        {
            _logger = logger;
        }

        public (bool, PIServer) Connect()
        {
            if (_SitePI.ConnectionInfo.IsConnected) return (true, _SitePI);
            else
            {
                try
                {
                    _logger.Information("Connecting to PI {0}", _PICollectiveName);
                    _SitePI = new PIServers()[_PICollectiveName];
                    _SitePI.Connect();

                    // Connection Info
                    _logger.Information("Successfully connected to {0}", _PICollectiveName);
                    _logger.Information("Connected to {0} at port {1} as user {2}", _SitePI.ConnectionInfo.Host, _SitePI.ConnectionInfo.Port, _SitePI.CurrentUserName);
                    _logger.Information("PI Identities mapped to above user: ");
                    foreach (var identity in _SitePI.CurrentUserIdentities)
                    {
                        _logger.Information("     {0}", identity.Name);
                    }
                    // _logger.Information("AllowWriteValues: {0}", _SitePI.Collective.AllowWriteValues);
                }
                catch (Exception e)
                {
                    _logger.Error("Unable to connect to PI Data Collective. Error: {0}", e.Message);
                }

                return (_SitePI.ConnectionInfo.IsConnected, _SitePI);
            }
        }

        public bool Disconnect()
        {
            if (!_SitePI.ConnectionInfo.IsConnected) return true;
            else
            {
                _logger.Information("Disconnecting from PI");
                try
                {
                    _SitePI.Disconnect();
                    _logger.Information("Successfully disconnected from PI Data Collective");
                }
                catch (Exception e)
                {
                    _logger.Error("Unable to disconnect from PI Data Collective. Error {0}", e.Message);
                }
                return (!_SitePI.ConnectionInfo.IsConnected);
            }
        }
    }
}
