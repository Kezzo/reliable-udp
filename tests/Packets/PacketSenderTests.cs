using System;
using ReliableUDP.Packets;
using Xunit;

namespace ReliableUDP.Tests.Packets;

public class PacketSenderTests
{
    [Fact]
    public async void TestSendPacket()
    {        
        var mockUdpClient = new Mocks.MockUdpClient(null);
        var packetSender = new PacketSender(mockUdpClient);

        byte payload = 20;
        UInt16 packetSequence = await packetSender.SendPacket(new PacketHeader{}, new byte[] { payload });

        Assert.NotEmpty(mockUdpClient.SentDatagrams);
        Assert.Contains(payload, mockUdpClient.SentDatagrams[0]);

        for (int i = 1; i < UInt16.MaxValue + 2; i++)
        {
            packetSequence = await packetSender.SendPacket(new PacketHeader{}, new byte[] { payload });
            Assert.Equal(i % (UInt16.MaxValue + 1), packetSequence);
        }
    }
}