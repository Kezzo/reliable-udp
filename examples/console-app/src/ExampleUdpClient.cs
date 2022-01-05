using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using ReliableUdp;

namespace ConsoleExampleApp
{
    public class ExampleUdpClient : IUdpClient
    {
        private UdpClient udpClient;

        public int Available { get { return udpClient.Available; } }

        private string targetHost;
        private int targetPort;

        public ExampleUdpClient(int bindPort, string targetHost, int targetPort)
        {
            udpClient = new UdpClient(bindPort);
            this.targetHost = targetHost;
            this.targetPort = targetPort;
            Console.WriteLine($"LocalUdpClient listening to: { udpClient.Client.LocalEndPoint }");
        }

        ~ExampleUdpClient()
        {
            udpClient.Close();
        }

        public async Task<byte[]> ReceiveAsync()
        {
            var result = await udpClient.ReceiveAsync();
            return result.Buffer;
        }

        public Task<int> SendAsync(byte[] datagram)
        {
            return udpClient.SendAsync(datagram, datagram.Length, targetHost, targetPort);
        }
    }
}