using System.Buffers;

namespace KestrelTcp.MessageServer
{
    public interface IMessageParser
    {
        bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out Message message);
    }
}