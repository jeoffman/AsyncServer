using System.Net;
using System.Net.Sockets;
using ExampleServer.SockServer.Contracts;

namespace ExampleServer.SockServer
{
    public class SimpleServer : ISimpleServer
    {
        public const int MaxBacklog = 1024 * 4; //4K backlog WAG

        public Socket Socket { get; }
        public bool IsListening { get; set; }

        CancellationTokenSource CancellationTokenSource;

        public event EventHandler<ClientConnectedEventArgs> ClientConnected;

        public SimpleServer()
        {
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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

        public IPEndPoint GetLocalEndPoint()
        {
            if (!Socket.IsBound)
                return null;
            return Socket.LocalEndPoint as IPEndPoint;
        }

        public Task StartListening(int port)
        {
            return StartListening(new IPEndPoint(IPAddress.Any, port));
        }

        public Task StartListening(EndPoint endPoint)
        {
            if (IsListening) throw new InvalidOperationException("Already listening");

            if (!Socket.IsBound)
                Socket.Bind(endPoint);

            IsListening = true;
            CancellationTokenSource = new CancellationTokenSource();
            Task task = Task.Run(async () =>
            {
                try
                {
                    Socket.Listen(MaxBacklog);
                    while (!CancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            Socket newClient = await Socket
                                .AcceptAsync(CancellationTokenSource.Token)
                                .ConfigureAwait(false);
                            ClientConnected?.Invoke(this, new ClientConnectedEventArgs(newClient));
                        }
                        catch (OperationCanceledException)
                        {
                            //OperationCanceledException in server, happens during unit tests
                        }
                    }
                }
                finally
                {
                    IsListening = false;
                }
            });
            return task;
        }

        public void StopListening()
        {
            CancellationTokenSource?.Cancel();
        }
    }
}
