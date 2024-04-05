using System.Net.Sockets;

namespace FreeNet
{
    class BufferManager
    {
        private readonly int _numBytes;
        private readonly byte[] _buffer;
        private readonly Stack<int> _mFreeIndexPool;
        private int _mCurrentIndex;
        private readonly int _mBufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            _numBytes = totalBytes;
            _mCurrentIndex = 0;
            _mBufferSize = bufferSize;
            _mFreeIndexPool = new Stack<int>();
            _buffer = new byte[_numBytes];
        }

        public void SetBuffer(SocketAsyncEventArgs args)
        {
            if (_mFreeIndexPool.Count > 0)
            {
                args.SetBuffer(_buffer, _mFreeIndexPool.Pop(), _mBufferSize);
            }
            else
            {
                if (_numBytes - _mBufferSize < _mCurrentIndex)
                {
                    throw new Exception("BufferManager: SetBuffer: No more space in buffer");
                }

                args.SetBuffer(_buffer, _mCurrentIndex, _mBufferSize);
                _mCurrentIndex += _mBufferSize;
            }
        }
    }
}