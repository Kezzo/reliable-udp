using ReliableUDP.Packets;
using Xunit;

namespace ReliableUDP.Tests.Packets
{
    public class PacketSenderTests
    {
        [Fact]
        public async void TestSendPacket()
        {        
            var mockUdpClient = new Mocks.MockUdpClient(null);
            var packetSender = new PacketSender(mockUdpClient);

            byte payload = 20;
            ushort packetSequence = await packetSender.SendPacket(new PacketHeader{}, new byte[] { payload });

            Assert.NotEmpty(mockUdpClient.SentDatagrams);
            Assert.Contains(payload, mockUdpClient.SentDatagrams[0]);

            for (int i = 1; i < ushort.MaxValue + 2; i++)
            {
                packetSequence = await packetSender.SendPacket(new PacketHeader{}, new byte[] { payload });
                Assert.Equal(i % (ushort.MaxValue + 1), packetSequence);
            }
        }
    }
}