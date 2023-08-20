using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace KestrelTcp.EchoServer
{
    public class EchoConnectionHandler : ConnectionHandler
    {
        public static int ConnectionCount = 0;
        public static int DisconnectCount = 0;

        public const int MaxMessageSize = 1024 * 1024 * 256;  // 256KB
        private readonly ILogger<EchoConnectionHandler> _logger;

        public EchoConnectionHandler(ILogger<EchoConnectionHandler> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            Interlocked.Increment(ref ConnectionCount);
            _logger.LogInformation(connection.ConnectionId + " connected");

            byte[] inboundBytes = new byte[MaxMessageSize];
            int index = 0;

            while (true)
            {
                ReadResult result = await connection.Transport.Input.ReadAsync();
                ReadOnlySequence<byte> buffer = result.Buffer;

                foreach (ReadOnlyMemory<byte> segment in buffer)
                {
                    Array.Copy(segment.ToArray(), 0, inboundBytes, index, segment.Length);
                    index += segment.Length;

                    await connection.Transport.Output.WriteAsync(segment);    //echo
                }

                if (result.IsCompleted)
                    break;

                connection.Transport.Input.AdvanceTo(buffer.End);
            }

            Interlocked.Increment(ref DisconnectCount);

            _logger.LogInformation(connection.ConnectionId + " disconnected");
        }
    }
}