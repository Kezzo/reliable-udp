using ReliableUDP.Packets;
using Xunit;

namespace ReliableUDP.Tests.Packets;

public class PacketTests
{
    [Fact]
    public void TestPacketCreation()
    {
        var payload = new byte[] { 1, 23, 234, 32 };
        var header = new PacketHeader {
            Sequence = 324,
            LastAck = 23,
            AckBits = 12334
        };

        var buffer = header.AddBytes(payload);
        var packet = new Packet(buffer);

        Assert.True(packet.Header.Equals(header));
        Assert.Equal(payload, packet.Payload);
    }
}