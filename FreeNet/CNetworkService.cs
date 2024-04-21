using System.Diagnostics;
using System.Net.Sockets;

namespace FreeNet
{
    public class CNetworkService
    {
        private const int MAX_CONNECTION_COUNT = 512;
        private const int BUFFER_SIZE = 1024;
        private const int PRE_ALLOC_COUNT = 2; // read, write

        private readonly CListener _clientListener;
        private readonly SocketAsyncEventArgsPool _receiveEventArgsPool;
        private readonly SocketAsyncEventArgsPool _sendEventArgsPool;
        private readonly BufferManager _bufferManager;
        public delegate void SessionHandler(CUserToken token);
        private SessionHandler _sessionCreatedCallback { get; set; }

        public CNetworkService(SessionHandler sessionCreatedCallback)
        {
            _clientListener = new CListener(OnNewClient);
            _sessionCreatedCallback = sessionCreatedCallback;
            _bufferManager = new BufferManager(MAX_CONNECTION_COUNT * BUFFER_SIZE * PRE_ALLOC_COUNT, BUFFER_SIZE);
            _receiveEventArgsPool = new SocketAsyncEventArgsPool(MAX_CONNECTION_COUNT);
            _sendEventArgsPool = new SocketAsyncEventArgsPool(MAX_CONNECTION_COUNT);

            for (int i = 0; i < MAX_CONNECTION_COUNT; i++)
            {
                SocketAsyncEventArgs socketAsyncEventArgs;
                {
                    socketAsyncEventArgs = new SocketAsyncEventArgs();
                    socketAsyncEventArgs.Completed += ReceiveCompleted;
                    socketAsyncEventArgs.UserToken = null;
                    _bufferManager.SetBuffer(socketAsyncEventArgs);
                    _receiveEventArgsPool.Push(socketAsyncEventArgs);
                }

                {
                    socketAsyncEventArgs = new SocketAsyncEventArgs();
                    socketAsyncEventArgs.Completed += SendCompleted;
                    socketAsyncEventArgs.UserToken = null;
                    _bufferManager.SetBuffer(socketAsyncEventArgs);
                    _sendEventArgsPool.Push(socketAsyncEventArgs);
                }
            }
        }

        public void Listen(string host, int port, int backlog)
        {
            _clientListener.Start(host, port, backlog);
        }

        public void OnConnectCompleted(Socket socket, CUserToken token)
        {
            SocketAsyncEventArgs receiveEventArg = new SocketAsyncEventArgs();
            receiveEventArg.Completed += ReceiveCompleted;
            receiveEventArg.UserToken = token;
            receiveEventArg.SetBuffer(new byte[1024], 0, 1024);

            SocketAsyncEventArgs sendEventArg = new SocketAsyncEventArgs();
            sendEventArg.Completed += SendCompleted;
            sendEventArg.UserToken = token;
            sendEventArg.SetBuffer(new byte[1024], 0, 1024);

            BeginReceive(socket, receiveEventArg, sendEventArg);
        }

        private void OnNewClient(Socket clientSocket, object token)
        {
            Debug.Assert(_sessionCreatedCallback != null, nameof(_sessionCreatedCallback) + " != null");
            Console.WriteLine("Client is connected.");
            SocketAsyncEventArgs receiveArgs = _receiveEventArgsPool.Pop();
            SocketAsyncEventArgs sendArgs = _sendEventArgsPool.Pop();

            Debug.Assert(receiveArgs != null, nameof(receiveArgs) + " != null");
            Debug.Assert(sendArgs != null, nameof(sendArgs) + " != null");

            var userToken = new CUserToken();
            userToken.SetEventArgs(receiveArgs, sendArgs);
            receiveArgs.UserToken = userToken;
            sendArgs.UserToken = userToken;
            _sessionCreatedCallback(userToken);
            BeginReceive(clientSocket, receiveArgs, sendArgs);
        }

        private void BeginReceive(Socket socket, SocketAsyncEventArgs receiveArgs, SocketAsyncEventArgs sendArgs)
        {
            CUserToken token = (CUserToken)receiveArgs.UserToken!;
            token.SetEventArgs(receiveArgs, sendArgs);
            token.Socket = socket;

            try
            {
                bool pending = socket.ReceiveAsync(receiveArgs);
                if (!pending)
                {
                    ProcessReceive(receiveArgs);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"error code {e.ErrorCode}, message {e.Message}");
            }
        }

        private void ReceiveCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                ProcessReceive(e);
                return;
            }

            throw new ArgumentException("The last operation completed on the socket was not a receive.");
        }

        private void SendCompleted(object? sender, SocketAsyncEventArgs e)
        {
            CUserToken token = (CUserToken)e.UserToken!;
            token.SendCallback(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            Console.WriteLine("ProcessReceive");

            CUserToken token = (CUserToken)e.UserToken!;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                Debug.Assert(token != null, nameof(token) + " != null");
                token.OnReceive(e.Buffer!, e.Offset, e.BytesTransferred);

                bool pending = token.Socket!.ReceiveAsync(e);
                if (!pending)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                Console.WriteLine($"error {e.SocketError},  transferred {e.BytesTransferred}");
                CloseClientSocket(token);
            }
        }

        public void CloseClientSocket(CUserToken? token)
        {
            if (token == null)  // token is already removed.
            {
                Console.WriteLine("Token is null.");
                return;
            }

            if (token.Socket == null)
            {
                Console.WriteLine("Socket is null");
                return;
            }

            _receiveEventArgsPool.Push(token.ReceiveEventArgs!);
            _sendEventArgsPool.Push(token.SendEventArgs!);
            token.OnRemoved();
        }
    }
}