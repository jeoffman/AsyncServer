using System.Security.Cryptography.X509Certificates;
using ExampleServer.SockServer.Contracts;

namespace ExampleServer.SockServer
{
    public interface IServerConnection
    {
        event EventHandler<EventArgs> Disconnected;
        event EventHandler<ReceivedEventArgs> Received;

        Task StartReceiveLoop(X509Certificate2 certificate);
        Task SendAsync(byte[] buffer);
    }
}
