using System.Collections.Generic;
using System.IO;
using ReliableUdp.Packets;
using Xunit;

namespace ReliableUdp.Tests.Packets
{
    public class PacketHeaderTests
    {
        private byte[] GetTestPacketHeaderBytes(PacketHeader testHeader)
        {
            byte[] testBytes;

            using(MemoryStream ms = new MemoryStream())
            {
                using(BinaryWriter writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                {
                    writer.Write(testHeader.Sequence);
                    writer.Write(testHeader.LastAck);
                    writer.Write(testHeader.AckBits);
                }

                testBytes = ms.ToArray();
            }

            return testBytes;
        }

        [Fact]
        public void TestPacketHeaderCreationFromBytes()
        {
            var testHeader = new PacketHeader{
                Sequence = 23,
                AckBits = 23,
                LastAck = 225
            };

            var createdheader = new PacketHeader(GetTestPacketHeaderBytes(testHeader));
            Assert.True(testHeader.Equals(createdheader));
        }

        [Fact]
        public void TestAddingHeaderBytesToPayload()
        {
            var testHeader = new PacketHeader{
                Sequence = 23,
                AckBits = 23,
                LastAck = 225
            };

            var testPayload = new byte[] { 5, 6, 7, 8 };

            using(MemoryStream ms = new MemoryStream(testHeader.AddBytes(testPayload)))
            {
                using (BinaryReader reader = new BinaryReader(ms, System.Text.UTF8Encoding.UTF8, true))
                {
                    Assert.Equal(testHeader.Sequence, reader.ReadUInt16());
                    Assert.Equal(testHeader.LastAck, reader.ReadUInt16());
                    Assert.Equal(testHeader.AckBits, reader.ReadUInt32());
                    
                    for (int i = 0; i < testPayload.Length; i++)
                    {
                        Assert.Equal(testPayload[i], reader.ReadByte());
                    }

                    // check if stream end was reached
                    Assert.Equal(ms.Position, ms.Length);
                }
            }
        }

        [Fact]
        public void TestGetAcks()
        {
            var testHeader = new PacketHeader{
                AckBits = 0b_0001_0100_0100_0001_1000_0100_0110_1000,
                LastAck = 10
            };

            var expectedAcks = new List<ushort>{ 10, 6, 4, 3, 65535, 65530, 65529, 65523, 65519, 65517 };

            Assert.Equal(testHeader.GetAcks(), expectedAcks);
        }

        [Fact]
        public void TestGetAcksFromFirstHeader()
        {
            var testHeader = new PacketHeader{
                AckBits = 0b_0001_0100_0100_0001_1000_0100_0110_1001,
                LastAck = 0
            };

            var expectedAcks = new List<ushort>{ 0, 65535, 65532, 65530, 65529, 65525, 65520, 65519, 65513, 65509, 65507 };

            Assert.Equal(testHeader.GetAcks(), expectedAcks);
        }

        [Fact]
        public void TestGetAcksFromSecondHeader()
        {
            var testHeader = new PacketHeader{
                AckBits = 0b_0001_0100_0100_0001_1000_0100_0110_1001,
                LastAck = 1
            };

            var expectedAcks = new List<ushort>{ 1, 0, 65533, 65531, 65530, 65526, 65521, 65520, 65514, 65510, 65508 };

            Assert.Equal(testHeader.GetAcks(), expectedAcks);
        }
    }
}