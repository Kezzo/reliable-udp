using System.Threading.Tasks;

namespace ReliableUdp
{
    public interface IUdpClient
    {
        Task<int> SendAsync(byte[] datagram);
        Task<byte[]> ReceiveAsync();
        int Available { get; }
    }
}