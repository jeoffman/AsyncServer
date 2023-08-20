using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Net;

namespace KestrelTcp.MessageServer
{
    public static class MessageHandlerExtensions
    {
        public static IServiceCollection ConfigureMessageParser(this IServiceCollection services, IPEndPoint endPoint)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<KestrelServerOptions>, MessageOptionsSetup>());

            services.Configure<MessageHandlerOptions>(o =>
            {
                o.EndPoint = endPoint;
            });

            services.TryAddSingleton<IMessageParser, MessageParser>();
            return services;
        }
    }
}