using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AsyncServer
{
    public class AsyncTcpServer
    {
        public bool KeepRunning { get; set; }
        TcpListener Listener { get; set; }

        int ArbitraryPacketLength = 1024 * 12;  //12K buffer is several typical MTUs - maybe you
                                                //  need more bytes for your TCP messages?

        public async Task StartListening(IPAddress localAddress, int port)
        {
            KeepRunning = true;
            Listener = new TcpListener(localAddress, port);
            Listener.Start();

            try
            {
                while (KeepRunning)
                {
                    TcpClient client = await Listener.AcceptTcpClientAsync();
                    OnClientConnected(client);
                }
            }
            catch (ObjectDisposedException exc)
            {
                if (KeepRunning)   //otherwise I am ASSuming that someone yanked the Listener.Server out from under me (via "Close" and .Stop, below)
                    throw;
            }
            catch (Exception exc)
            {
                throw;
            }
            finally
            {
                if (Listener.Server.IsBound)
                    Listener.Stop();
            }
        }

        public virtual void OnClientConnected(TcpClient client)
        {
        }

        public void StopServer()
        {
            KeepRunning = false;
            Listener.Stop();

            OnStopServer();
        }

        public virtual void OnStopServer()
        {
        }

        public virtual void OnClientClosed(AsyncTcpConnection connection)
        {
        }

        /// <summary>Something has connected to our server = spin up a new async task loop for 
        /// that client</summary>
        public async Task Accept(AsyncTcpConnection connection)
        {
            try
            {
                await Task.Yield(); //returns immediately to our server

                using (NetworkStream stream = connection.TcpClient.GetStream())
                {
                    byte[] data = new byte[ArbitraryPacketLength];
                    bool keepProcessingClient = true;

                    while (KeepRunning && keepProcessingClient)
                    {
                        int bytesRead = 0;
                        bytesRead = await stream.ReadAsync(data, bytesRead, data.Length - bytesRead);
                        if (bytesRead == 0)
                        {
                            keepProcessingClient = false;
                        }
                        else
                        {   //process the bytes sent by the client
                            await connection.ProcessRead(data.Take(bytesRead).ToArray());
                        }
                    }
                }
            }
            catch (IOException ex)
            {   //I think we are closing the server and the "Close" method just closed our socket
                Console.WriteLine(ex.Message);
            }

            OnClientClosed(connection);
        }

        public int GetPort()
        {
            if (Listener == null) throw new InvalidOperationException("Server is not listening yet, StartListening first");
            return ((IPEndPoint)Listener.LocalEndpoint).Port;
        }

        public async Task Send(TcpClient client, string text)
        {
            byte[] data = Encoding.ASCII.GetBytes(text);
            await Send(client, data);
        }

        public async Task Send(TcpClient client, byte[] data)
        {
            //NOTE: the "Read" is still using this GetStream, so don't go disposing it all willy-nilly now
            NetworkStream stream = client.GetStream();
            await stream.WriteAsync(data, 0, data.Length);
        }
    }

    public class AsyncTcpServer<TClient> : AsyncTcpServer where TClient : AsyncTcpConnection, new()
    {
        object _clientListLock = new object();
        List<AsyncTcpConnection> _connections = new List<AsyncTcpConnection>();

        public static TClient CreateClient(TcpClient client, AsyncTcpServer server)
        {   //what I really want is a generic constructor with parameters...
            TClient retval = new TClient();
            retval.FixClientAndServerAfterGenericConstructor(client, server);
            return retval;
        }

        public override void OnClientConnected(TcpClient client)
        {
            // I want a generic constructor that takes parameters (TcpClient client, AsyncTcpServer server)
            //var connection = new TClient(client, this);
            var connection = CreateClient(client, this);    //instead I guess I do it the dangerous way

            lock (_clientListLock)
                _connections.Add(connection);

            var clientTask = Accept(connection);
            connection.PostFixTask(clientTask); //chicken-before-the-egg problem = need a client task (preferred to do it in the constructor) but we need the returned task from "Accept" first
        }

        public override void OnStopServer()
        {
            lock (_clientListLock)
            {
                foreach (var connection in _connections)
                {
                    try
                    {
                        connection.TcpClient.Close();
                    }
                    catch (Exception exc)
                    {
                        throw;
                    }
                }
            }
        }
        public override void OnClientClosed(AsyncTcpConnection connection)
        {
            lock (_clientListLock)
                _connections.Remove(connection);
        }

        public Task Broadcast(byte[] data)
        {
            List<Task> whenAllWaits = new List<Task>();

            lock (_clientListLock)
            {
                foreach (var connections in _connections)
                {
                    try
                    {
                        whenAllWaits.Add(Send(connections.TcpClient, data));
                    }
                    catch (Exception exc)
                    {
                        throw;
                    }
                }
            }
            Task.WaitAll(whenAllWaits.ToArray());
            return Task.CompletedTask;
        }
    }
}
