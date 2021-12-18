using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ReliableUDP.Tests.Mocks;

public class MockUDPClient : IUdpClient
{
    public int Available { get { return resultsToReturn.Count > 0 ? resultsToReturn.Peek().Length : 0; } } 

    private readonly Queue<byte[]> resultsToReturn = new Queue<byte[]>();

    public List<byte[]> SentDatagrams = new List<byte[]>();

    public MockUDPClient(List<byte[]>? resultsToReturn)
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

    public void AddResultToReturn(byte[] result)
    {
        resultsToReturn.Enqueue(result);
    }

    public Task<UdpReceiveResult> ReceiveAsync()
    {
        if(resultsToReturn.Count == 0)
        {
            // throw here to surface code using the udp client without checking first if data is available
            throw new InvalidOperationException();
        }

        return Task.FromResult(new UdpReceiveResult(resultsToReturn.Dequeue(), new System.Net.IPEndPoint(0, 0)));
    }

    public Task<int> SendAsync(byte[] datagram, int bytes)
    {
        SentDatagrams.Add(datagram);
        return Task.FromResult(bytes);
    }
}