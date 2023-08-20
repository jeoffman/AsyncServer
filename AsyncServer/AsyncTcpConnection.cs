using System.Net.Sockets;
using System.Threading.Tasks;

namespace AsyncServer
{
    /// <summary>This represents each connection to your AsyncTcpServer. Bytes arrive and you ProcessRead</summary>
    public abstract class AsyncTcpConnection
    {
        public TcpClient TcpClient { get; private set; }
        public Task Task { get; private set; }
        public AsyncTcpServer Server { get; private set; }

        public AsyncTcpConnection()
        {
        }

        public void FixClientAndServerAfterGenericConstructor(TcpClient client, AsyncTcpServer server)
        {
            this.TcpClient = client;
            this.Server = server;
        }

        public void PostFixTask(Task task)
        {
            this.Task = task;
        }

        public abstract Task ProcessRead(byte[] data);
    }
}
