using Autofac;
using Core.Backfiller;
using Core.ConnectionManager;
using Core.Service;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Threading.Tasks;

namespace Core
{
    class Program
    {
        private static IContainer _container;

        static Program()
        {
            // Configure and Create a logger using Serilog
            var logger = new LoggerConfiguration().MinimumLevel.Debug()
                                                  .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                                                  .WriteTo.File("Logs\\History.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: null)
                                                  .CreateLogger();

            // Registers instances into container
            var builder = new ContainerBuilder();
            builder.RegisterInstance(logger).As<ILogger>().SingleInstance();
            builder.RegisterType<PIConnectionManager>().As<IPIConnectionManager>().SingleInstance();
            builder.RegisterType<HistoryBackfiller>().As<IHistoryBackfiller>().SingleInstance();
            builder.RegisterType<HDAService>().As<IHDAService>().SingleInstance();
            _container = builder.Build();
        }

        static async Task Main(string[] args)
        {
            var service = _container.Resolve<IHDAService>();
            await service.Start();
            service.Stop();
            Console.ReadLine();
        }
    }
}
