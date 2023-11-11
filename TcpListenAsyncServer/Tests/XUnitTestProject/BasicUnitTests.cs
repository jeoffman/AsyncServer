using AsyncServer;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTestProject
{
    public class BasicUnitTests
    {
        //don't uncomment this, XUnit is broken if you do
        //[Theory(Skip ="XUnit really is broken")]
        //[InlineData(')')]
        //[InlineData('(')]
        //public void IsXUnitTestRunnerBrokenTest(char character)
        //{
        //    Assert.True(character == character);
        //}

        [Fact]
        public async Task ItsAliveTest()
        {

            var server = new AsyncTcpServer<AsyncEchoTcpConnection>();
            Task serverTask = server.StartListening(IPAddress.Loopback, 0);

            TcpClient tcpClient = new TcpClient();

            await tcpClient.ConnectAsync(IPAddress.Loopback, server.GetPort());

            server.StopServer();

            await serverTask;
            Assert.True(true);
        }

        [Fact]
        public async Task ItEchosTest()
        {
            AsyncTcpServer server = new AsyncTcpServer<AsyncEchoTcpConnection>();
            Task serverTask = server.StartListening(IPAddress.Any, 0);

            TcpClient tcpClient = new TcpClient();

            await tcpClient.ConnectAsync(IPAddress.Loopback, server.GetPort());
            var stream = tcpClient.GetStream();
            string text = "Hello World";
            await stream.WriteAsync(Encoding.UTF8.GetBytes(text));
            byte[] buffer = new byte[1024];
            var received = await stream.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(received, text.Length);
            Assert.Equal(text, System.Text.Encoding.UTF8.GetString(buffer, 0, received));

            server.StopServer();

            await serverTask;
        }

        [Fact]
        public async Task ItBroadcastsTest()
        {
            AsyncTcpServer server = new AsyncTcpServer<AsyncEchoTcpConnection>();
            Task serverTask = server.StartListening(IPAddress.Any, 0);

            TcpClient tcpClient1 = new TcpClient();
            await tcpClient1.ConnectAsync(IPAddress.Loopback, server.GetPort());

            TcpClient tcpClient2 = new TcpClient();
            await tcpClient2.ConnectAsync(IPAddress.Loopback, server.GetPort());


            var stream1 = tcpClient1.GetStream();
            string text = "Hello World";
            await stream1.WriteAsync(Encoding.UTF8.GetBytes(text));
            byte[] buffer = new byte[1024];
            var received = await stream1.ReadAsync(buffer, 0, buffer.Length);

            Assert.Equal(received, text.Length);
            Assert.Equal(text, Encoding.UTF8.GetString(buffer, 0, received));

            var stream2 = tcpClient2.GetStream();
            received = await stream2.ReadAsync(buffer, 0, buffer.Length);
            Assert.Equal(received, text.Length);
            Assert.Equal(text, Encoding.UTF8.GetString(buffer, 0, received));

            server.StopServer();

            await serverTask;
        }
    }
}
