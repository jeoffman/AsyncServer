using System.Net;

namespace ClientConsoleTestingConnectDisconnect
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            int threadCount = 10;
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

            IEnumerable<Thread> threads = ConnectDisconnecter.CreateSocketThreads(threadCount, new IPEndPoint(IPAddress.Loopback, 5006), cancellationTokenSource.Token);


            Console.WriteLine("Sleep 30 seconds");
            Thread.Sleep(30000);


            //Console.WriteLine("Press ENTER/RETURN to exit" + Environment.NewLine);
            //Console.ReadLine();

            Console.WriteLine($"Closing {threads.Count()} Threads");
            cancellationTokenSource.Cancel();
            foreach (Thread thread in threads)
            {
                thread.Join(500);
            }

            Console.WriteLine($"AttemptsCount = {ConnectDisconnecter.AttemptsCount}, Connections = {ConnectDisconnecter.ConnectionCount}, Disconnects = {ConnectDisconnecter.DisconnectCount} (plus canary)");
            Console.ReadLine();
            Console.WriteLine($"BYE");
        }

    }
}
