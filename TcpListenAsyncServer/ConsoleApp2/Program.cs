using AsyncServer;
using System;
using System.Net;
using System.Threading.Tasks;
using XUnitTestProject;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            AsyncTcpServer<AsyncEchoTcpConnection> server = new AsyncTcpServer<AsyncEchoTcpConnection>();
            Task serverTask = server.StartListening(IPAddress.Any, 12345);


            Console.WriteLine("Hit ENTER to quit");
            Console.ReadLine();

            server.StopServer();
            serverTask.Wait();
        }
    }
}
