using ExampleServer.SockServer.Contracts;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace ExampleServer.SockServer
{
    public class ServerConnection : IServerConnection, IDisposable
    {
        const int ErrorCode = 17000;

        readonly Socket _socket;
        readonly ILogger _logger;
        readonly bool _allowAnyClientCertificate;
        readonly EndPoint _endpointDebugCopy;
        readonly CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<EventArgs> Disconnected;
        public event EventHandler<ReceivedEventArgs> Received;

        public bool IsRunning { get; set; }
        public bool AtLeastOneByteReceived { get; set; }

        Stream _stream;

        public ServerConnection(ILogger logger, Socket client, CancellationTokenSource cancellationTokenSource, bool allowAnyClientCertificate = true)
        {
            _logger = logger;
            _socket = client;
            _cancellationTokenSource = cancellationTokenSource;
            _allowAnyClientCertificate = allowAnyClientCertificate;

            _endpointDebugCopy = _socket?.RemoteEndPoint;   //so we can log info about _socket even after its gone
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _socket?.Dispose();
            }
        }
        #endregion IDisposable


        public Task StartReceiveLoop(X509Certificate2? certificate)
        {
            SetupNetworkStream(certificate);

            Task task = Task.Run(async () =>
            {
                IsRunning = true;

                _logger.LogDebug(ErrorCode + 1, "Starting connection receive loop for {RemoteEndPoint}", _endpointDebugCopy);

                try
                {
                    await ProcessStreamWhileLoopAsync()
                        .ConfigureAwait(false);
                }
                //NOTE: all the weird little ways that a socket can close out on our server:
                catch (ObjectDisposedException exc)
                {
                    _logger.LogInformation(ErrorCode + 2, exc, "Disconnected: ObjectDisposed for {RemoteEndPoint}", _endpointDebugCopy);
                    Disconnected?.Invoke(this, new EventArgs());
                }
                catch (SocketException exc)
                {
                    if (exc.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        _logger.LogInformation(ErrorCode + 8, "Disconnected: ConnectionReset");
                        Disconnected?.Invoke(this, new EventArgs());
                    }
                    else if (exc.SocketErrorCode == SocketError.OperationAborted ||
                            exc.SocketErrorCode == SocketError.ConnectionAborted ||
                            exc.SocketErrorCode == SocketError.Interrupted ||
                            exc.SocketErrorCode == SocketError.InvalidArgument)
                    {
                        _logger.LogInformation(ErrorCode + 9, "Socket Disconnected: Socket Code={SockCode}, RemoteEndPoint={RemoteEndPoint}", exc.SocketErrorCode, _endpointDebugCopy);
                        Disconnected?.Invoke(this, new EventArgs());
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (ArgumentException exc)
                {
                    _logger.LogError(ErrorCode + 3, exc, "Exception for {RemoteEndPoint}", _endpointDebugCopy);
                    throw;
                }
                catch (IOException exc)
                {
                    if (exc.InnerException is SocketException exc2 &&
                            (exc2.SocketErrorCode == SocketError.OperationAborted ||
                                exc2.SocketErrorCode == SocketError.ConnectionAborted ||
                                exc2.SocketErrorCode == SocketError.Interrupted ||
                                exc2.SocketErrorCode == SocketError.InvalidArgument ||
                                exc2.SocketErrorCode == SocketError.ConnectionReset))
                    {
                        _logger.LogInformation(ErrorCode + 6, "Stream Disconnected: Stream Code={StreamCode}, Socket Code={SockCode}, RemoteEndPoint={RemoteEndPoint}", exc.HResult, exc2.SocketErrorCode, _endpointDebugCopy);
                        Disconnected?.Invoke(this, new EventArgs());
                    }
                    else if (exc.HResult == -2146232800)   //0x80131620
                    {
                        _logger.LogInformation(ErrorCode + 7, "Stream failure on Socket disconnected: Socket Code={SockCode}, RemoteEndPoint={RemoteEndPoint}", exc.HResult, _endpointDebugCopy);
                        Disconnected?.Invoke(this, new EventArgs());
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception exc)
                {
                    _logger.LogError(ErrorCode + 4, exc, "Exception for {RemoteEndPoint}", _endpointDebugCopy);
                    throw;
                }
                finally
                {
                    if (AtLeastOneByteReceived)
                        _logger.LogInformation(ErrorCode + 5, "Stopping connection receive loop for {RemoteEndPoint}", _endpointDebugCopy);
                    IsRunning = false;
                }
            });
            return task;
        }

        /// <summary>WARN: this is a "while" loop - it won't return until the connection dies</summary>
        private async Task ProcessStreamWhileLoopAsync()
        {
            byte[] byteBuffer = new byte[8192];
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                int bytesRead = await _stream.ReadAsync(byteBuffer, _cancellationTokenSource.Token).ConfigureAwait(false);
                if (bytesRead > 0)
                {
                    AtLeastOneByteReceived = true;

                    _logger.LogTrace(ErrorCode + 10, "Read {Count} bytes from socket {RemoteEndPoint}", bytesRead, _endpointDebugCopy);

                    byte[] truncated = new byte[bytesRead];
                    Buffer.BlockCopy(byteBuffer, 0, truncated, 0, bytesRead);
                    Received?.Invoke(this, new ReceivedEventArgs(truncated));   //future Jeoff: span this!
                }
                else
                {
                    if (AtLeastOneByteReceived)
                        _logger.LogInformation(ErrorCode + 11, "Disconnected: received 0 bytes {RemoteEndPoint}", _endpointDebugCopy);
                    Disconnected?.Invoke(this, new EventArgs());
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Dispose();
                    break;
                }
            }
        }

        /// <summary>use certificate for TLS or null for no encryption</summary>
        private void SetupNetworkStream(X509Certificate2? certificate)
        {
            _stream = new NetworkStream(_socket);

            if (certificate != null)
            {
                SslStream sslStream = new SslStream(_stream, false);
                sslStream.AuthenticateAsServer(new SslServerAuthenticationOptions
                {
                    ServerCertificate = certificate,
                    ClientCertificateRequired = false,
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => _allowAnyClientCertificate,  // do we check client certs?
                });
                _stream = sslStream;
            }
        }

        public void Close()
        {
            _socket.Close();
        }

        public Task SendAsync(byte[] buffer)
        {
            try
            {
                return _stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (ArgumentException)
            {
                //client socket is dead
                return Task.CompletedTask;
            }
        }
    }
}
