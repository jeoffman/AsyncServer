using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KestrelConsole2
{
    public class Startup : IDisposable
    {
        #region IDisposable
        public void Dispose()
        {
            //_server.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion IDisposable

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // services.Add(new ServiceDescriptor(typeof(RespServer), _server));
        }


        // IWebHostBuilder but we aren't using web pages so I don't think you want to set anything up in here
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
        }
    }
}
