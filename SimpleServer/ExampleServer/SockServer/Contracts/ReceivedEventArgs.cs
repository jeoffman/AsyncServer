namespace ExampleServer.SockServer.Contracts
{
    public class ReceivedEventArgs : EventArgs
    {
        public byte[] Buffer { get; }

        public ReceivedEventArgs(byte[] bytes)
        {
            Buffer = bytes;
        }
    }
}
