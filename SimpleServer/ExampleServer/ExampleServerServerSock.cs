using ExampleServer.SockServer;
using Microsoft.Extensions.Logging;

namespace ExampleServer
{
    public class ExampleServerServerSock : IDisposable
    {
        const int ErrorCode = 17000;

        readonly ILoggerFactory _loggerFactory;
        readonly ILogger<ExampleServerServerSock> _logger;

        SimpleServer _server;
        int connectCount;
        int disconnectCount;

        public ExampleServerServerSock(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            _logger = _loggerFactory.CreateLogger<ExampleServerServerSock>();
        }

        #region IDisposable
        private bool disposedValue;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _server?.Dispose();
                }

                // free unmanaged resources (unmanaged objects) and override finalizer
                // set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion IDisposable

        public bool IsListening
        {
            get
            {
                if (_server != null)
                    return _server.IsListening;
                return false;
            }
        }

        public Task OpenServerAsync(int port)
        {
            _server = new SimpleServer();
            _logger.LogInformation(ErrorCode + 1, "Binding server to port={Port}", port);

            _server.ClientConnected += async (sender, args) =>
            {
                Interlocked.Increment(ref connectCount);
                LengthPrefixReceiveBuffer lengthPrefixReceiveBuffer = new LengthPrefixReceiveBuffer();
                ServerConnection serverConnection = new ServerConnection(_loggerFactory.CreateLogger("ServerConnection"), args.ClientSocket, new CancellationTokenSource(), true);
                try
                {
                    serverConnection.Disconnected += (sender, args) =>
                    {
                        Interlocked.Increment(ref disconnectCount);
                    };
                    serverConnection.Received += async (sender, args) =>
                    {
                        lengthPrefixReceiveBuffer.AppendAndResizeBytes(args.Buffer);

                        byte[]? message;

                        bool keepProcessingMessages = true;
                        while (keepProcessingMessages)
                        {
                            message = lengthPrefixReceiveBuffer.TakeMessage();
                            if (message == null)
                                break;
                            await ProcessMessageAsync(sender as ServerConnection, message);
                        }
                    };

                    Task clientTask = serverConnection.StartReceiveLoop(null);
                    await clientTask;

                    if (serverConnection.AtLeastOneByteReceived)
                        _logger.LogInformation(ErrorCode + 3, "{ServerConnection} processing loop is exiting", nameof(ServerConnection));
                }
                catch (Exception exc)
                {
                    _logger.LogError(ErrorCode + 4, exc, "Exception processing client message, socket closed");
                }
                finally
                {
                    serverConnection?.Dispose();
                }
            };


            //NOTE: the Task returned is the "main processing task" - it will not return/end until you StopListening so be careful what you do with it!
            Task _serverTask = _server.StartListening(port);
            _serverTask.ContinueWith(t =>
            {
                _logger.LogCritical(ErrorCode + 5, t.Exception, "The server has faulted - this is extremely bad");
            }, TaskContinuationOptions.OnlyOnFaulted);

            return Task.CompletedTask;
        }

        public Task CloseServerAsync()
        {
            _server.StopListening();
            return Task.CompletedTask;
        }

        private async Task ProcessMessageAsync(ServerConnection serverConnection, byte[] message)
        {

            //TODO: interface with Orleans and all that stuff here

            await Task.Yield();
        }

    }
}
