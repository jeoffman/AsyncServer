using System.Net;
using System.Net.Sockets;

namespace ClientConsoleTestingConnectDisconnect
{
    public class ConnectDisconnecter
    {
        public static int ConnectionCount = 0;
        public static int DisconnectCount = 0;
        public static int AttemptsCount = 0;

        internal static IEnumerable<Thread> CreateSocketThreads(int threadCount, IPEndPoint server, CancellationToken token)
        {
            try
            {
                Console.WriteLine($"Testing 1 connection to {server}");
                //try 1 connection = is your server even running?
                using (TcpClient tcpClient = new TcpClient())
                {
                    tcpClient.Connect(server);
                }

                Console.WriteLine($"Starting {threadCount} threads");
                List<Thread> retval = new List<Thread>();
                for (int count = 0; count < threadCount; count++)
                {
                    var thread = new Thread(() =>
                    {
                        byte[] message = new byte[1000];
                        while (!token.IsCancellationRequested)
                        {
                            Interlocked.Increment(ref AttemptsCount);
                            try
                            {
                                using (TcpClient tcpClient = new TcpClient())
                                {
                                    tcpClient.Connect(server);
                                    Interlocked.Increment(ref ConnectionCount);
                                    NetworkStream stream = tcpClient.GetStream();
                                    stream.Write(message);
                                }
                                Interlocked.Increment(ref DisconnectCount);
                            }
                            catch (Exception exc)
                            {
                                Console.WriteLine(exc.ToString());
                            }
                        }
                    });
                    thread.IsBackground = true;
                    thread.Start();
                    retval.Add(thread);
                }
                return retval;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
                return new List<Thread>();
            }
        }
    }
}
