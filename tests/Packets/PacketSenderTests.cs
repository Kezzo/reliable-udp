using System.Linq;
using ReliableUDP.Packets;
using Xunit;

namespace ReliableUDP.Tests.Packets;

public class PacketSenderTests
{
    [Fact]
    public void TestIsPacketAcked()
    {        
        var mockUdpClient = new Mocks.MockUDPClient(null);
        var packetSender = new PacketSender(mockUdpClient);

        packetSender.SendPacket(new PacketHeader{}, new byte[] { 20 });

        Assert.False(packetSender.IsPacketAcked(0));

        packetSender.OnPacketAcked(0);
        Assert.True(packetSender.IsPacketAcked(0));
    }

    [Fact]
    public void TestReceiveNextPacket()
    {        
        var mockUdpClient = new Mocks.MockUDPClient(null);
        var packetSender = new PacketSender(mockUdpClient);

        byte payload = 20;
        packetSender.SendPacket(new PacketHeader{}, new byte[] { payload });

        Assert.NotEmpty(mockUdpClient.SentDatagrams);
        Assert.Contains(payload, mockUdpClient.SentDatagrams[0]);
    }
}