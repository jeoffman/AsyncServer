using System.Net;
using ExampleServer.SockServer.Contracts;

namespace ExampleServer.SockServer
{
    public interface ISimpleServer : IDisposable
    {
        event EventHandler<ClientConnectedEventArgs> ClientConnected;

        /// <summary>What address/port are you listening on? (useful if you choose port 0)</summary>
        IPEndPoint GetLocalEndPoint();
        /// <summary>WARN: The returned task will not end until you call StopListening</summary>
        public Task StartListening(int port);
        /// <summary>WARN: The returned task will not end until you call StopListening</summary>
        public Task StartListening(EndPoint endPoint);
        /// <summary>Close the server, but connections remain</summary>
        void StopListening();
    }
}
