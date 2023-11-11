using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExampleServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging(builder => builder.AddConsole());

                    services.AddHostedService<Worker>();

                    services.AddTransient<ExampleServerServerSock>();
                });
        }
    }

    public class Worker : BackgroundService
    {
        readonly ILogger<Worker> _logger;
        readonly IHost _host;
        readonly ExampleServerServerSock _server;

        public Worker(ILogger<Worker> logger, IHost host, ExampleServerServerSock server)
        {
            _logger = logger;
            _host = host;
            _server = server;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Hello world!");

            await _server.OpenServerAsync(5003);


            Console.WriteLine("ENTER to quit...");
            Console.ReadLine();

            await _server.CloseServerAsync();


            await _host.StopAsync();
        }
    }
}