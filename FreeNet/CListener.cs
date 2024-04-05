using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace FreeNet
{
    class CListener
    {
        private readonly SocketAsyncEventArgs _acceptArgs;
        private readonly Socket _listenSocket;
        private readonly AutoResetEvent _flowControlEvent;

        public delegate void NewClientHandler(Socket clientSocket, object token);

        private readonly NewClientHandler _callbackOnNewClient;

        public CListener(NewClientHandler callbackOnNewClient)
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _callbackOnNewClient = callbackOnNewClient;
            _flowControlEvent = new AutoResetEvent(true);
            _acceptArgs = new SocketAsyncEventArgs();
            _acceptArgs.Completed += OnAcceptCompleted;
        }

        public void Start(string host, int port, int backlog)
        {
            var address = host == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(host);
            IPEndPoint endpoint = new IPEndPoint(address, port);
            _listenSocket.Bind(endpoint);
            _listenSocket.Listen(backlog);
            StartAccept();
        }

        private void StartAccept()
        {
            _flowControlEvent.WaitOne();
            _acceptArgs.AcceptSocket = null;

            bool isPending = _listenSocket.AcceptAsync(_acceptArgs);
            if (!isPending)
            {
                ProcessAccept(_acceptArgs);
            }
        }

        private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (e.AcceptSocket == null)
                {
                    throw new Exception("AcceptSocket is null");
                }

                _callbackOnNewClient?.Invoke(e.AcceptSocket!, e.UserToken!);
            }
            else
            {
                Debug.WriteLine($"Failed to accept a connection. {e.SocketError}");
            }

            _flowControlEvent.Set();
            StartAccept();
        }
    }
}