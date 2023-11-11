using System.Net.Sockets;

namespace ExampleServer.SockServer.Contracts
{
    public class ClientConnectedEventArgs : EventArgs
    {
        public Socket ClientSocket { get; private set; }

        public ClientConnectedEventArgs(Socket clientSocket) { ClientSocket = clientSocket; }
    }
}
