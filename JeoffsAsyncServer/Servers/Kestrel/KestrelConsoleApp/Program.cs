using KestrelTcp.EchoServer;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace ConsoleAppKestrel
{
    class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args)
                .Build()
                .Run();

            Console.WriteLine($"Connections = {EchoConnectionHandler.ConnectionCount}, Disconnections = {EchoConnectionHandler.DisconnectCount}");
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(services =>
                {   // This shows how a custom framework could plug in an experience without using Kestrel APIs directly
                    services.ConfigureMessageParser(new IPEndPoint(IPAddress.Loopback, 5007));
                })
                .UseKestrel(options =>
                {   // Setup a ConnectionHandler through UseKestrel
                    options.ListenAnyIP(5006, builder =>
                    {
                        builder.UseConnectionHandler<EchoConnectionHandler>();
                        builder.KestrelServerOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(1);
                        builder.NoDelay = true;
                        //builder.Use(del);
                    });
                })
                .UseStartup<Startup>();
    }

    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // There is no HTTP request pipeline, so who is going to use this? Ah, its a kestrel thing
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}

            //app.Run(async (context) =>
            //{
            //    await context.Response.WriteAsync("Hello World!");
            //});
        }
    }

}
