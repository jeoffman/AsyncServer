using Microsoft.Extensions.Logging;
using System.Net.Sockets;

namespace SocketServer
{
    public class ServerConnection : IServerConnection, IDisposable
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

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Socket?.Dispose();
            }
        }
        #endregion IDisposable


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
                catch (ObjectDisposedException exc)// when (exc.ObjectName == "System.Net.Sockets.Socket")
                {
                    _logger.LogDebug("Disconnected: ObjectDisposed");
                    Disconnected?.Invoke(this, new EventArgs());
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
                catch (ArgumentException exc)
                {
                    _logger.LogError(exc, "Exception");
                    //throw;
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, "Exception");
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

        public void Close()
        {
            Socket.Close();
        }

        public Task SendAsync(ArraySegment<byte> arraySegment)
        {
            try
            {
                return Socket.SendAsync(arraySegment, SocketFlags.None);
            }
            catch (ArgumentException)
            {
                //client socket is dead
                return Task.CompletedTask;
            }
        }
    }

    public interface IServerConnection
    {
        event EventHandler<EventArgs>? Disconnected;
        event EventHandler<ReceivedEventArgs>? Received;

        Task StartReceiveLoop();
        Task SendAsync(ArraySegment<byte> arraySegment);
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
