using System.Net;
using System.Net.Sockets;

namespace SocketServer
{
    public interface ISimpleServer : IDisposable
    {
        event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>What address/port are you listening on? (useful if you choose port 0)</summary>
        IPEndPoint? GetLocalEndPoint();
        /// <summary>WARN: The returned task will not end until you call StopListening</summary>
        public Task StartListening(int port);
        /// <summary>WARN: The returned task will not end until you call StopListening</summary>
        public Task StartListening(EndPoint endPoint);
        /// <summary>Close the server, but connections remain</summary>
        void StopListening();
    }

    public class SimpleServer : ISimpleServer
    {
        public const int MaxBacklog = 1024 * 4; //4K backlog WAG

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

            if(!Socket.IsBound)
                Socket.Bind(endPoint);

            IsListening = true;
            CancellationTokenSource = new CancellationTokenSource();
            Task task = Task.Run(async () =>
            {
                try
                {
                    //Queue<Socket> clients = new Queue<Socket>();
                    //for (int count = 0; count < MaxBacklog; count++)
                    //    clients.Enqueue(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
                    Socket.Listen(MaxBacklog);
                    while (!CancellationTokenSource.IsCancellationRequested)
                    {
                        try
                        {
                            Socket newClient = await Socket
                                //.AcceptAsync(clients.Dequeue(), CancellationTokenSource.Token)
                                .AcceptAsync(CancellationTokenSource.Token)
                                .ConfigureAwait(false);
                            ClientConnected?.Invoke(this, new ClientConnectedEventArgs(newClient));
                        }
                        catch (OperationCanceledException)
                        {
                            //_logger.LogInformation("OperationCanceledException in server, happens during unit tests");
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
