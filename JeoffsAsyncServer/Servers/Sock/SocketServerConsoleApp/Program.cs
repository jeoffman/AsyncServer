using SocketServer;
using Microsoft.Extensions.Logging;
using Moq;

namespace SocketServerConsoleApp
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            int port = 5006;
            using SimpleServer testee = new SimpleServer();
            ServerConnection? serverConnection = null;

            int connectionCount = 0;
            int disconnectCount = 0;
            testee.ClientConnected += async (sender, args) =>
            {
                Interlocked.Increment(ref connectionCount);
                serverConnection = new ServerConnection(new Mock<ILogger<ServerConnection>>().Object, args.ClientSocket);
                serverConnection.Disconnected += (sender, args) =>
                {
                    Interlocked.Increment(ref disconnectCount);
                };
                serverConnection.Received += async (sender, args) =>
                {
                    try
                    {
                        await serverConnection.SendAsync(args.ArraySegment);   //ECHO server
                    }
                    catch (Exception exc)
                    {
                        //Console.WriteLine(exc.ToString());
                    }
                };
                Task clientTask = serverConnection.StartReceiveLoop();
                await clientTask;
                serverConnection.Dispose();
            };
            Task listenTask = testee.StartListening(port);

            Console.WriteLine($"Now listening on port {port}");
            Console.WriteLine($"Press ENTER/RETURN to exit" + Environment.NewLine);
            Console.ReadLine();

            Console.WriteLine($"Connections = {connectionCount}, Disconnects = {disconnectCount}");

            testee.StopListening();
            await listenTask;

        }
    }
}