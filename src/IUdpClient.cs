namespace ReliableUdp
{
    public interface IUdpClient
    {
        int Send(byte[] datagram);
        byte[] Receive();
        int Available { get; }
    }
}