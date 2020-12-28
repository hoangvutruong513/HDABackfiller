using Autofac;
using OSIsoft.AF.Asset;
using OSIsoft.AF.Data;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    class Program
    {
        private static ContainerBuilder _builder = new ContainerBuilder();
        private static IContainer _container;
        private static DateTime startTime, endTime;
        private static PIServer myPIServer;
        private static List<string> csvList = new List<string>{ "SIM_Tag1_DA", "SIM_Tag2_DA" };
        static Program()
        {
            // TODO: Register DI components here
            //_container = _builder.Build();
        }
        static async Task Main(string[] args)
        {
            Console.WriteLine("Connecting to SGTAKEDAPI Collective");
            myPIServer = new PIServers()["SGTAKEDAPI"];
            myPIServer.Connect();

            // Connection Info
            Console.WriteLine("Connection status {0}", myPIServer.ConnectionInfo.IsConnected);
            Console.WriteLine("Connected to {0} at port {1} as user {2}", myPIServer.ConnectionInfo.Host, myPIServer.ConnectionInfo.Port, myPIServer.CurrentUserName);
            Console.WriteLine("PI Identities mapped to above user: ");
            foreach (var identity in myPIServer.CurrentUserIdentities)
            {
                Console.WriteLine("     {0}", identity.Name);
            }
            Console.WriteLine("AllowWriteValues: {0}", myPIServer.Collective.AllowWriteValues);
            Console.WriteLine();

            // Ask for User's input start time 
            var cultureInfo = new CultureInfo("en-US");
            Console.WriteLine("Input start time for backfill: ");
            string startTimeString = Console.ReadLine() + " +08";
            startTime = DateTime.ParseExact(startTimeString, "dd-MMM-yyyy HH:mm:ss zz", cultureInfo);
            AFTime backfillStart = new AFTime(startTime);

            // Ask for User's input end time
            Console.WriteLine("Input end time for backfill: ");
            string endTimeString = Console.ReadLine() + " +08";
            endTime = DateTime.ParseExact(endTimeString, "dd-MMM-yyyy HH:mm:ss zz", cultureInfo);
            AFTime backfillEnd = new AFTime(endTime);

            // Construct an AF Time Range
            AFTimeRange backfillRange = new AFTimeRange(backfillStart, backfillEnd);

            // Get a task handle
            var daPointListTask = PIPoint.FindPIPointsAsync(myPIServer, csvList);

            while (!daPointListTask.IsCompleted)
            {
                Console.Write(".");
            }

            var daPointList = await daPointListTask;
            Console.WriteLine();

            foreach (var point in daPointList)
            {
                Console.WriteLine("Name {0}", point.Name);
            }

            //PIPoint daPoint = PIPoint.FindPIPoint(myPIServer, "SIM_Tag1_DA");
            //Console.WriteLine("Recorded Values between {0}", backfillRange);
            //AFValues recordedValues = daPoint.RecordedValues(backfillRange, AFBoundaryType.Inside, null, true);
            //foreach(AFValue value in recordedValues)
            //{
            //    Console.WriteLine("Timestamp: {0}, Value: {1}", value.Timestamp, value.Value);
            //}

            myPIServer.Disconnect();
            Console.ReadLine();
        }
    }
}
