using System;
using System.Buffers;

namespace SocketServer
{
    public class IntegerFramer
    {
        private readonly int _headerSize;
        private int _messageSize;
        private bool _parsingMessage;

        public IntegerFramer(int headerSize)
        {
            _headerSize = headerSize;
            _parsingMessage = false;
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out byte[] message)
        {
            message = new byte[0];

            if (!_parsingMessage)
            {
                if (buffer.Length < _headerSize)
                {
                    return false;
                }

                byte[] header = buffer.Slice(0, _headerSize).ToArray();
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(header);
                _messageSize = BitConverter.ToInt32(header, 0);

                buffer = buffer.Slice(_headerSize);
                _parsingMessage = true;
            }

            if (_parsingMessage && buffer.Length < _messageSize)
            {
                return false;
            }

            ReadOnlySequence<byte> messageBytes = buffer.Slice(0, _messageSize);
            message = messageBytes.ToArray();

            buffer = buffer.Slice(_messageSize);
            _parsingMessage = false;

            return true;
        }
    }
}
