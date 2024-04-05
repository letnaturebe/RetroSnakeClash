namespace FreeNet
{
    static class Defines
    {
        public const short HEADER_SIZE = 2;
    }

    class CMessageResolver
    {
        public delegate void CompletedMessageCallback(byte[] buffer);

        private int _messageSize;
        readonly byte[] _messageBuffer = new byte[1024];
        private int _currentPosition;
        private int _positionToRead;
        private int _remainBytes;

        private bool ReadUntil(byte[] buffer, ref int srcPosition, int offset, int receivedByteCount)
        {
            if (_currentPosition >= offset + receivedByteCount)
            {
                return false;
            }

            int copySize = _positionToRead - _currentPosition;
            if (_remainBytes < copySize)
            {
                copySize = _remainBytes;
            }

            Array.Copy(buffer, srcPosition, this._messageBuffer, this._currentPosition, copySize);

            srcPosition += copySize;
            _currentPosition += copySize;
            _remainBytes -= copySize;
            if (_currentPosition < _positionToRead)
            {
                return false;
            }

            return true;
        }

        public void OnReceive(
            byte[] buffer, int offset, int receivedByteCount, CompletedMessageCallback callback)
        {
            _remainBytes = receivedByteCount;
            int srcPosition = offset;

            while (_remainBytes > 0)
            {
                bool completed;
                if (_currentPosition < Defines.HEADER_SIZE)
                {
                    _positionToRead = Defines.HEADER_SIZE;

                    completed = ReadUntil(buffer, ref srcPosition, offset, receivedByteCount);
                    if (!completed)
                    {
                        return;
                    }

                    _messageSize = GetBodySize();
                    _positionToRead = _messageSize + Defines.HEADER_SIZE;
                }

                completed = ReadUntil(buffer, ref srcPosition, offset, receivedByteCount);

                if (completed)
                {
                    callback(_messageBuffer);
                    ClearBuffer();
                }
            }
        }

        private int GetBodySize()
        {
            return BitConverter.ToInt16(_messageBuffer, 0);
        }

        private void ClearBuffer()
        {
            Array.Clear(_messageBuffer, 0, this._messageBuffer.Length);
            _currentPosition = 0;
            _messageSize = 0;
        }
    }
}