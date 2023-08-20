using SocketServer;
using System.Buffers;

namespace SocketServerTests
{
    public class IntegerFramerTests
    {
        [Fact]
        public void SimpleIntegerFramerTest()
        {
            // create a simple complete message of the correct length
            byte[] bytes = new byte[] { 0x00, 0x00, 0x00, 0x0b, 0x01, 0x02, 0x03, 0x04, 0x00, 0x00, 0x00, 0x03, 0x61, 0x62, 0x63 };
            ReadOnlySequence<byte> buffer = new ReadOnlySequence<byte>(bytes);

            IntegerFramer testee = new IntegerFramer(4);
            bool result = testee.TryParseMessage(ref buffer, out byte[] message);

            Assert.True(result);
            Assert.Equal(11, message.Length);
        }


        [Fact]
        public void ShortMessagesIntegerFramerTest()
        {
            // Initialize the buffer with some bytes
            byte[] bytes = new byte[] { 0x00, 0x00, 0x00, 0x0b, 0x01, 0x02, 0x03, 0x04, 0x00, 0x00, 0x00, 0x03, 0x61, 0x62, 0x63 };
            ReadOnlySequence<byte> buffer = new ReadOnlySequence<byte>(bytes);

            // Create an instance of the IntegerFramer class
            IntegerFramer framer = new IntegerFramer(4);

            // Parse messages from the buffer
            if (framer.TryParseMessage(ref buffer, out byte[] message))
            {
                Console.WriteLine($"Message: {message}");
            }

        }
    }
}
