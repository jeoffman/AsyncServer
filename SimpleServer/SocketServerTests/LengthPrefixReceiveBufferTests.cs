using ExampleServer;

namespace SocketServerTests
{
    public class DefaultReceiveBufferSizeTests
    {
        public static readonly byte[] NineByteMessage = new byte[] { 0x00, 0x09, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 };
        
        Random _random = new Random();

        [Fact]
        public void SimpleDefaultReceiveBufferSizeTest()
        {
            // create a simple complete message
            LengthPrefixReceiveBuffer testee = new LengthPrefixReceiveBuffer();
            testee.AppendAndResizeBytes(NineByteMessage);

            byte[]? result = testee.TakeMessage();

            Assert.NotNull(result);
            Assert.Equal(NineByteMessage.Length, result.Length);
            Assert.Equal(NineByteMessage, result);
        }

        [Fact]
        public void MessageTooLongStillWorksTest()
        {
            // simple message with some extra bytes at the end
            byte[] bytes = new byte[] { 0x00, 0x09, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };

            LengthPrefixReceiveBuffer testee = new LengthPrefixReceiveBuffer();
            testee.AppendAndResizeBytes(bytes);

            byte[]? result = testee.TakeMessage();

            Assert.NotNull(result);
            Assert.Equal(9, result.Length);
            Assert.Equal(new byte[] { 0x00, 0x09, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 }, result);
        }

        [Fact]
        public void TwoMessagesInOneSendTest()
        {
            // two simple complete messages in one byte[]
            byte[] bytes = new byte[] { 0x00, 0x05, 0x01, 0x02, 0x03, 0x00, 0x05, 0x04, 0x05, 0x06, };

            LengthPrefixReceiveBuffer testee = new LengthPrefixReceiveBuffer();
            testee.AppendAndResizeBytes(bytes);

            byte[]? result1 = testee.TakeMessage();
            Assert.NotNull(result1);
            Assert.Equal(5, result1.Length);
            Assert.Equal(new byte[] { 0x00, 0x05, 0x01, 0x02, 0x03 }, result1);

            byte[]? result2 = testee.TakeMessage();
            Assert.NotNull(result2);
            Assert.Equal(5, result2.Length);
            Assert.Equal(new byte[] { 0x00, 0x05, 0x04, 0x05, 0x06 }, result2);
        }

        [Fact]
        public void OneMessageTakesManySendsTest()
        {
            LengthPrefixReceiveBuffer testee = new LengthPrefixReceiveBuffer();

            testee.AppendAndResizeBytes(new byte [] {0x00 });
            byte[]? result = testee.TakeMessage();
            Assert.Null(result);

            testee.AppendAndResizeBytes(new byte[] { 0x05 });
            result = testee.TakeMessage();
            Assert.Null(result);

            testee.AppendAndResizeBytes(new byte[] { 0x01 });
            result = testee.TakeMessage();
            Assert.Null(result);

            testee.AppendAndResizeBytes(new byte[] { 0x02 });
            result = testee.TakeMessage();
            Assert.Null(result);

            testee.AppendAndResizeBytes(new byte[] { 0x03 });
            byte[]? result2 = testee.TakeMessage();

            Assert.NotNull(result2);
            Assert.Equal(5, result2.Length);
            Assert.Equal(new byte[] { 0x00, 0x05, 0x01, 0x02, 0x03 }, result2);
        }

        [Fact]
        public void BufferGrowsTest()
        {
            short size = (short)_random.Next(LengthPrefixReceiveBuffer.DefaultReceiveBufferSize + 1, 15000);    //anything bigger than 1K (DefaultReceiveBufferSize) and less that 1M
            byte[] header = BitConverter.GetBytes(size);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(header);

            byte[] messageChunk = new byte[size - 2];   //minus 2 because this is the message part without the header/length prefix
            _random.NextBytes(messageChunk);

            LengthPrefixReceiveBuffer testee = new LengthPrefixReceiveBuffer();
            testee.AppendAndResizeBytes(header);
            testee.AppendAndResizeBytes(messageChunk);

            byte[]? result = testee.TakeMessage();

            byte[] completeMessage = new byte[size];
            Array.Copy(header, completeMessage, header.Length);
            Array.Copy(messageChunk, 0, completeMessage, header.Length, messageChunk.Length);

            Assert.NotNull(result);
            Assert.Equal(size, result.Length);
            Assert.Equal(completeMessage, result);
        }
    }
}
