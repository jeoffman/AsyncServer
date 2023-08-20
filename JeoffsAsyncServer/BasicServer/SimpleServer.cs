using System.Net;
using System.Net.Sockets;

namespace BasicTcpServer
{
    public interface ISimpleServer : IDisposable
    {
        event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>What address/port will you listen on?</summary>
        /// <summary>What address/port are you listening on? (useful if you choose port 0)</summary>
        IPEndPoint? GetLocalEndPoint();
        /// <summary>WARN: The returned task will not end until you StopListening</summary>
        public Task StartListening(int port);
        public Task StartListening(EndPoint endPoint);
        /// <summary>Shutdown</summary>
        void StopListening();
    }

    public class SimpleServer : ISimpleServer
    {
        public Socket Socket { get; }
        public bool IsListening { get; set; }

        CancellationTokenSource? CancellationTokenSource;

        public event EventHandler<ClientConnectedEventArgs>? ClientConnected;

        public SimpleServer()
        {
            Socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public IPEndPoint? GetLocalEndPoint()
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
            if (!Socket.IsBound) throw new InvalidOperationException("Not bound");

            if(!Socket.IsBound)
                Socket.Bind(endPoint);

            IsListening = true;
            CancellationTokenSource = new CancellationTokenSource();
            Task task = Task.Run(async () =>
            {
                try
                {
                    Socket.Listen(1024 * 4);    //4K backlog WAG
                    while (!CancellationTokenSource.IsCancellationRequested)
                    {
                        Socket newClient = await Socket
                            .AcceptAsync(CancellationTokenSource.Token)
                            .ConfigureAwait(false);
                        ClientConnected?.Invoke(this, new ClientConnectedEventArgs(newClient));
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
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
            }
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
                Socket?.Close();
                Socket?.Dispose();
            }
        }
        #endregion IDisposable
    }

    public class ClientConnectedEventArgs : EventArgs
    {
        public Socket ClientSocket { get; private set; }

        public ClientConnectedEventArgs(Socket clientSocket) { ClientSocket = clientSocket; }
    }
}
