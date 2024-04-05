using System.Diagnostics;
using System.Net.Sockets;

namespace FreeNet
{
    public class CUserToken
    {
        public Socket? Socket { get; set; }
        public SocketAsyncEventArgs? ReceiveEventArgs { get; private set; }
        public SocketAsyncEventArgs? SendEventArgs { get; private set; }
        private readonly CMessageResolver _messageResolver = new();
        private IPeer? _peer;

        private readonly object _sendingQueueLock = new();
        private readonly Queue<CPacket> _sendingQueue = new();

        public void SetPeer(IPeer peer)
        {
            _peer = peer;
        }

        public void SetEventArgs(SocketAsyncEventArgs receiveEventArgs, SocketAsyncEventArgs sendEventArgs)
        {
            ReceiveEventArgs = receiveEventArgs;
            SendEventArgs = sendEventArgs;
        }

        public void OnReceive(byte[] buffer, int offset, int receivedByteCount)
        {
            _messageResolver.OnReceive(buffer, offset, receivedByteCount, OnMessage);
        }

        private void OnMessage(byte[] buffer)
        {
            _peer?.OnMessage(buffer);
        }

        public void OnRemoved()
        {
            lock (_sendingQueueLock)
            {
                _sendingQueue.Clear();
            }

            Debug.Assert(Socket != null, "Socket != null");
            Socket.Close();
            _peer?.OnRemoved();
        }

        public void Send(CPacket msg)
        {
            CPacket clone = new CPacket();
            msg.copy_to(clone);

            lock (_sendingQueueLock)
            {
                if (_sendingQueue.Count <= 0)
                {
                    _sendingQueue.Enqueue(clone);
                    StartSend();
                    return;
                }

                _sendingQueue.Enqueue(clone);
            }
        }

        private void StartSend()
        {
            Debug.Assert(SendEventArgs != null, "SendEventArgs != null");
            Debug.Assert(Socket != null, "Socket != null");

            lock (_sendingQueueLock)
            {
                CPacket msg = _sendingQueue.Peek();
                msg.record_size();
                SendEventArgs.SetBuffer(SendEventArgs.Offset, msg.position);
                Array.Copy(
                    msg.buffer,
                    0,
                    SendEventArgs.Buffer!,
                    SendEventArgs.Offset,
                    msg.position);

                if (Socket.Connected == false)
                {
                    while (_sendingQueue.Count > 0)
                    {
                        _sendingQueue.Dequeue();
                    }
                    return;
                }

                try
                {
                    bool pending = Socket.SendAsync(SendEventArgs);
                    if (!pending)
                    {
                        SendCallback(SendEventArgs);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }
        }

        public void SendCallback(SocketAsyncEventArgs e)
        {
            if (e.BytesTransferred <= 0 || e.SocketError != SocketError.Success)
            {
                return;
            }

            lock (_sendingQueueLock)
            {
                _sendingQueue.Dequeue();
                if (_sendingQueue.Count > 0)
                {
                    StartSend();
                }
            }
        }
    }
}