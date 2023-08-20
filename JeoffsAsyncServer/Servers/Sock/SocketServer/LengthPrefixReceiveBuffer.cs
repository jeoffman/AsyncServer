namespace SocketServer
{
    public class LengthPrefixReceiveBuffer
    {
        public const int MaxMessageSize = 1024 * 1024;  // 1MB
        public const int DefaultReceiveBufferSize = 1024;
        public const int ReallocOvercompensation = 1024;

        byte[] _buffer = new byte[DefaultReceiveBufferSize];
        int _current;

        public void AppendAndResizeBytes(byte[] newData, int sizeOfNewData)
        {
            int totalNeededBufferSize = _current + sizeOfNewData;

            if (totalNeededBufferSize > MaxMessageSize)
                throw new InvalidOperationException($"{nameof(LengthPrefixReceiveBuffer)} max buffer of {MaxMessageSize} bytes exceeded, something has gone terribly wrong!");

            if (_buffer.Length < totalNeededBufferSize)
            {
                int amountToAdd = totalNeededBufferSize - _buffer.Length;
                int chunks = amountToAdd / ReallocOvercompensation;
                Array.Resize(ref _buffer, _buffer.Length + (chunks + 1) * ReallocOvercompensation);
            }

            Array.ConstrainedCopy(newData, 0, _buffer, _current, sizeOfNewData);
            _current += sizeOfNewData;
        }

        public void AppendAndResizeBytes(byte[] bytes)
        {
            AppendAndResizeBytes(bytes, bytes.Length);
        }

        public byte[]? TakeMessage()
        {
            byte[]? retval = null;
            if (_current > 2)
            {
                byte[] lenBytes = new byte[] { _buffer[0], _buffer[1] };
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lenBytes);
                ushort expectedMessageSize = BitConverter.ToUInt16(lenBytes, 0);
                if (expectedMessageSize <= _current)
                {
                    retval = new byte[expectedMessageSize]; //Future Jeoff: Span this!

                    Array.Copy(_buffer, retval, expectedMessageSize);

                    if (_current > expectedMessageSize)
                    {
                        Array.Copy(_buffer, expectedMessageSize, _buffer, 0, _current - expectedMessageSize);
                    }

                    _current -= expectedMessageSize;
                }
            }
            return retval;
        }
    }
}
