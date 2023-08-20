using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;

namespace KestrelConsole2
{
    public static class Program
    {
        public static void Main(string[] args) => CreateWebHostBuilder(args).Build().Run();

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options =>
                {
                    options.ListenLocalhost(5006, builder => builder.UseConnectionHandler<EchoConnectionHandler>());
                })
                .UseStartup<Startup>();
    }
}
