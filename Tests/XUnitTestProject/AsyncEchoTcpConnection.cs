using AsyncServer;
using System.Threading.Tasks;

namespace XUnitTestProject
{
    public class AsyncEchoTcpConnection : AsyncTcpConnection
    {
        public AsyncEchoTcpConnection()
        {
        }

        public override Task ProcessRead(byte[] data)
        {
            AsyncTcpServer<AsyncEchoTcpConnection> server = Server as AsyncTcpServer<AsyncEchoTcpConnection>;   //I'm feeling uncomfortable with this
            return server.Broadcast(data);
        }
    }
}
