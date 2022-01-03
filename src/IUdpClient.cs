using System.Threading.Tasks;

namespace ReliableUDP
{
    public interface IUdpClient
    {
        Task<int> SendAsync(byte[] datagram);
        Task<byte[]> ReceiveAsync();
        int Available { get; }
    }
}