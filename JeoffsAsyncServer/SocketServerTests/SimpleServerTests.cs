using SocketServer;
using System.Net;
using System.Net.Sockets;

namespace SocketServerTests
{
    public class SimpleServerTests
    {
        [Fact]
        public async Task SimpleServerCanListenAndStopListening()
        {
            using SimpleServer testee = new SimpleServer();

            //ACT
            Task listenTask = testee.StartListening(0);
            Assert.True(testee.IsListening);
            testee.StopListening();
            await listenTask;
            Assert.False(testee.IsListening);
        }

        [Fact]
        public async Task SimpleServerCanListenAndStopListeningTwice()
        {
            using SimpleServer testee = new SimpleServer();

            //ACT & ASSERT 1
            Task listenTask1 = testee.StartListening(0);
            Assert.True(testee.IsListening);
            testee.StopListening();
            await listenTask1;
            Assert.False(testee.IsListening);

            //ACT & ASSERT 2
            Task listenTask2 = testee.StartListening(0);
            Assert.True(testee.IsListening);
            testee.StopListening();
            await listenTask2;
            Assert.False(testee.IsListening);
        }

        [Fact]
        public async Task SimpleServerCantListenTwice()
        {
            using SimpleServer testee = new SimpleServer();
            //ACT
            Task listenTask1 = testee.StartListening(0);
            //ACT & ASSERT
            InvalidOperationException exc = await Assert.ThrowsAsync<InvalidOperationException>(async () => await testee.StartListening(0));
            Assert.Contains("listening", exc.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task SimpleServerAcceptsClientsTest()
        {
            using SimpleServer testee = new SimpleServer();
            bool clientConnected = false;
            testee.ClientConnected += (sender, args) => { clientConnected = true; };
            Task listenTask = testee.StartListening(0);

            //ACT
            IPEndPoint? ephemeralEndPoint = testee.GetLocalEndPoint();
            Assert.NotNull(ephemeralEndPoint);
            Socket client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ephemeralEndPoint.Port));
            testee.StopListening();
            await listenTask;

            //ASSERT
            Assert.True(clientConnected);
        }
    }
}
