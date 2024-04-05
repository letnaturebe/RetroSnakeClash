using System.Net.Sockets;

namespace FreeNet
{
    class SocketAsyncEventArgsPool
    {
        private readonly object _lock = new();
        private readonly Stack<SocketAsyncEventArgs> _pool;

        public SocketAsyncEventArgsPool(int capacity)
        {
            _pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        public void Push(SocketAsyncEventArgs item)
        {
            lock (_lock)
            {
                _pool.Push(item);
            }
        }

        public SocketAsyncEventArgs Pop()
        {
            lock (_lock)
            {
                return _pool.Pop();
            }
        }
    }
}