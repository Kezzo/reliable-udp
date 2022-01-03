using System.Net.Sockets;
using System.Threading.Tasks;

namespace ReliableUDP
{
    public interface IUdpClient
    {
        Task<int> SendAsync(byte[] datagram, int bytes);
        Task<UdpReceiveResult> ReceiveAsync();
        int Available { get; }
    }
}