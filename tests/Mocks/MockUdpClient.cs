using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReliableUdp.Tests.Mocks
{
    public class MockUdpClient : IUdpClient
    {
        public int Available { get { return resultsToReturn.Count > 0 ? resultsToReturn.Peek().Length : 0; } } 

        private readonly Queue<byte[]> resultsToReturn = new Queue<byte[]>();

        public List<byte[]> SentDatagrams = new List<byte[]>();

        private MockUdpClient connectedClient;

        public MockUdpClient(List<byte[]> resultsToReturn)
        {
            if(resultsToReturn == null)
            {
                return;
            }

            for (int i = 0; i < resultsToReturn.Count; i++)
            {
                this.resultsToReturn.Enqueue(resultsToReturn[i]);
            }
        }

        public void ConnectToClient(MockUdpClient udpClient)
        {
            connectedClient = udpClient;
        }

        public void AddResultToReturn(byte[] result)
        {
            resultsToReturn.Enqueue(result);
        }

        public Task<byte[]> ReceiveAsync()
        {
            if(resultsToReturn.Count == 0)
            {
                // throw here to surface code using the udp client without checking first if data is available
                throw new InvalidOperationException();
            }

            return Task.FromResult(resultsToReturn.Dequeue());
        }

        public Task<int> SendAsync(byte[] datagram)
        {
            var datagramCopy = new byte[datagram.Length];
            Array.Copy(datagram, datagramCopy, datagram.Length);

            SentDatagrams.Add(datagramCopy);

            if(connectedClient != null)
            {
                connectedClient.AddResultToReturn(datagram);
            }

            return Task.FromResult(datagram.Length);
        }
    }
}