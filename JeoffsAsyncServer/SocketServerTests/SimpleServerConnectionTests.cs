using SocketServer;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Sockets;

namespace SocketServerTests
{
    public class SimpleServerConnectionTests
    {
        [Fact]
        public async Task ServerConnectionTest()
        {
            using SimpleServer testee = new SimpleServer();
            Task? clientTask = null;
            byte[]? recv = null;
            testee.ClientConnected += (sender, args) =>
            {
                ServerConnection serverConnection = new ServerConnection(new Mock<ILogger<ServerConnection>>().Object, args.ClientSocket);
                serverConnection.Received += (sender, args) =>
                {
                    recv = args.ArraySegment.ToArray(); //THIS is the thing we capture here and Assert below!
                };
                clientTask = serverConnection.StartReceiveLoop();
            };
            Task listenTask = testee.StartListening(0);

            //ACT
            IPEndPoint? ephemeralEndPoint = testee.GetLocalEndPoint();
            Assert.NotNull(ephemeralEndPoint);
            Socket client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ephemeralEndPoint.Port));
            await client.SendAsync(new ReadOnlyMemory<byte>(DefaultReceiveBufferSizeTests.NineByteMessage), SocketFlags.None);

            testee.StopListening();
            await listenTask;
            client.Close();
            Assert.NotNull(clientTask);
            await clientTask;

            //ASSERT
            Assert.NotNull(recv);
            Assert.Equal(DefaultReceiveBufferSizeTests.NineByteMessage, recv);
        }

        [Fact]
        public async Task ServerConnectionClientDisconnectTest()
        {
            using SimpleServer testee = new SimpleServer();
            Task? clientTask = null;
            bool disconnect = false;
            testee.ClientConnected += (sender, args) =>
            {
                ServerConnection serverConnection = new ServerConnection(new Mock<ILogger<ServerConnection>>().Object, args.ClientSocket);
                serverConnection.Disconnected += (sender, args) =>
                {
                    disconnect = true;
                };
                clientTask = serverConnection.StartReceiveLoop();
            };
            Task listenTask = testee.StartListening(0);

            IPEndPoint? ephemeralEndPoint = testee.GetLocalEndPoint();
            Assert.NotNull(ephemeralEndPoint);
            Socket client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ephemeralEndPoint.Port));

            //ACT
            client.Close(); // the CLIENT closes their connection

            testee.StopListening();
            await listenTask;
            Assert.NotNull(clientTask);
            await clientTask;

            //ASSERT
            Assert.True(disconnect);
        }

        [Fact]
        public async Task ServerConnectionServerDisconnectTest()
        {
            using SimpleServer testee = new SimpleServer();
            Task? clientTask = null;
            bool disconnect = false;
            ServerConnection? serverConnection = null;
            testee.ClientConnected += (sender, args) =>
            {
                serverConnection = new ServerConnection(new Mock<ILogger<ServerConnection>>().Object, args.ClientSocket);
                serverConnection.Disconnected += (sender, args) =>
                {
                    disconnect = true;
                };
                clientTask = serverConnection.StartReceiveLoop();
            };
            Task listenTask = testee.StartListening(0);

            IPEndPoint? ephemeralEndPoint = testee.GetLocalEndPoint();
            Assert.NotNull(ephemeralEndPoint);
            Socket client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ephemeralEndPoint.Port))
                .ConfigureAwait(false);

            //WARNING: doing the "StopListening" FIRST seems to give the ServerConnection await enough time to establish the connection before we close it below
            testee.StopListening();
            await listenTask;

            //ACT
            serverConnection?.Close(); // the SERVER closes the connection

            Assert.NotNull(clientTask);
            await clientTask;

            //ASSERT
            Assert.True(disconnect);
        }
    }
}
