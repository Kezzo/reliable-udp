using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReliableUdp.Tests.Mocks
{
    public class MockUdpClient : IUdpClient
    {
        public int Available 
        { 
            get 
            {
                var available = resultsToReturn.Count > 0 ? resultsToReturn.Peek().Length : 0;

                if(available > 0 && dropNextIncomingPacket)
                {
                    dropNextIncomingPacket = false;
                    available = 0;
                    resultsToReturn.Dequeue();
                }

                return available;
            }
        } 

        private readonly Queue<byte[]> resultsToReturn = new Queue<byte[]>();

        public List<byte[]> SentDatagrams = new List<byte[]>();

        private MockUdpClient connectedClient;
        private bool dropNextOutgoingPacket;
        private bool dropNextIncomingPacket;

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

        public void DropNextOutgoingPacket()
        {
            dropNextOutgoingPacket = true;
        }
        public void DropNextIncomingPacket()
        {
            dropNextIncomingPacket = true;
        }

        public Task<int> SendAsync(byte[] datagram)
        {
            if(dropNextOutgoingPacket)
            {
                dropNextOutgoingPacket = false;
                return Task.FromResult(datagram.Length);
            }

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