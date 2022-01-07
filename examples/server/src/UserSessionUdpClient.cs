using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ReliableUdp;

namespace ServerExample
{
    public class UserSessionUdpClient : IUdpClient
    {
        private Queue<byte[]> receivedDatagrams;

        public int Available { get { return receivedDatagrams.Count; } }
        private readonly IPEndPoint userEndpoint;
        private readonly Func<IPEndPoint, byte[], Task<int>> sendDatagramFunc;

        public UserSessionUdpClient(IPEndPoint userEndpoint, Func<IPEndPoint, byte[], Task<int>> sendDatagramFunc)
        {
            this.userEndpoint = userEndpoint;
            this.sendDatagramFunc = sendDatagramFunc;
            receivedDatagrams = new Queue<byte[]>();
        }

        public void AddReceivedDatagram(byte[] datagram)
        {
            receivedDatagrams.Enqueue(datagram);
        }

        public Task<byte[]> ReceiveAsync()
        {
            return Task.FromResult(receivedDatagrams.Dequeue());
        }

        public Task<int> SendAsync(byte[] datagram)
        {
            return this.sendDatagramFunc(userEndpoint, datagram);
        }
    }
}