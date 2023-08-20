using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace KestrelTcp.MessageServer
{
    public class MessageOptionsSetup : IConfigureOptions<KestrelServerOptions>
    {
        private readonly MessageHandlerOptions _options;

        public MessageOptionsSetup(IOptions<MessageHandlerOptions> options)
        {
            _options = options.Value;
        }

        public void Configure(KestrelServerOptions options)
        {
            options.Listen(_options.EndPoint, builder =>
            {
                builder.UseConnectionHandler<MessageConnectionHandler>();
            });
        }

        private class MessageConnectionHandler : ConnectionHandler
        {
            private readonly IMessageParser _parser;

            public MessageConnectionHandler(IMessageParser parser)
            {
                _parser = parser;
            }

            public override async Task OnConnectedAsync(ConnectionContext connection)
            {

                PipeReader input = connection.Transport.Input;

                // Code to parse messages
                while (true)
                {
                    ReadResult result = await input.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    if (_parser.TryParseMessage(ref buffer, out Message message))
                    {
                        await ProcessMessageAsync(message);
                    }

                    input.AdvanceTo(buffer.Start, buffer.End);
                }
            }

            private Task ProcessMessageAsync(Message message)
            {
                return Task.CompletedTask;
                throw new NotImplementedException();
            }
        }
    }
}