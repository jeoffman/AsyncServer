using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace BasicTcpServer
{
    public class ServerConnection : IServerConnection
    {
        public readonly Socket Socket;
        readonly ILogger<ServerConnection> _logger;

        public event EventHandler<EventArgs>? Disconnected;
        public event EventHandler<ReceivedEventArgs>? Received;

        public bool IsRunning { get; set; } = false;

        CancellationTokenSource? CancellationTokenSource;

        public ServerConnection(ILogger<ServerConnection> logger, Socket client)
        {
            _logger = logger;
            Socket = client;
        }

        public Task StartReceiveLoop()
        {
            CancellationTokenSource = new CancellationTokenSource();
            Task task = Task.Run(async () =>
            {
                _logger.LogDebug("Starting");

                try
                {
                    IsRunning = true;
                    ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[8192], 0, 8192);
                    while (!CancellationTokenSource.IsCancellationRequested)
                    {
                        int recv = await Socket
                            .ReceiveAsync(buffer, SocketFlags.None, CancellationTokenSource.Token)
                            .ConfigureAwait(false);
                        if (recv == 0)
                        {
                            _logger.LogDebug("Disconnected: received 0 bytes");
                            Disconnected?.Invoke(this, new EventArgs());
                            Socket.Shutdown(SocketShutdown.Both);
                            Socket.Dispose();
                            break;
                        }
                        Received?.Invoke(this, new ReceivedEventArgs(buffer.Slice(0, recv)));
                    }
                }
                catch (SocketException exc) when (exc.SocketErrorCode == SocketError.ConnectionReset)
                {
                    _logger.LogDebug("Disconnected: ConnectionReset");
                    Disconnected?.Invoke(this, new EventArgs());
                }
                catch (SocketException exc) when (exc.SocketErrorCode == SocketError.OperationAborted ||
                                                 exc.SocketErrorCode == SocketError.ConnectionAborted ||
                                                 exc.SocketErrorCode == SocketError.Interrupted ||
                                                 exc.SocketErrorCode == SocketError.InvalidArgument)
                {
                    _logger.LogDebug("Disconnected: Code={code}", ((SocketError)exc.SocketErrorCode));
                    Disconnected?.Invoke(this, new EventArgs());
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc,"Exception");
                    throw;
                }
                finally
                {
                    _logger.LogDebug("Stopping");
                    IsRunning = false;
                }
            });
            return task;
        }
    }

    public interface IServerConnection
    {
        event EventHandler<EventArgs>? Disconnected;
        event EventHandler<ReceivedEventArgs>? Received;

        Task StartReceiveLoop();
    }

    public class ReceivedEventArgs
    {
        public ArraySegment<byte> ArraySegment { get; }

        public ReceivedEventArgs(ArraySegment<byte> arraySegment)
        {
            ArraySegment = arraySegment;
        }
    }
}
