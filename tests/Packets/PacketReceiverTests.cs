using System;
using System.Collections.Generic;
using ReliableUdp.Packets;
using ReliableUdp.SequenceBuffer;
using Xunit;

namespace ReliableUdp.Tests.Packets
{
    public class PacketReceiverTests
    {
        [Fact]
        public void TestReceiveNextPacket()
        {
            var mockUdpClient = new Mocks.MockUdpClient(null);
            var testReceiver = new PacketReceiver(mockUdpClient);

            Assert.Null(testReceiver.ReceiveNextPacket());

            var testHeader = new PacketHeader{
                Sequence = 23,
                AckBits = 23,
                LastAck = 225
            };
            var payload = new byte[] { 5, 6, 7, 8 };

            mockUdpClient.AddResultToReturn(testHeader.AddBytes(payload));

            var packetToTest = testReceiver.ReceiveNextPacket();
            Assert.NotNull(packetToTest);

            if(packetToTest != null)
            {
                Assert.True(testHeader.Equals(packetToTest.Header));
                Assert.Equal(payload, packetToTest.Payload);
            }

            Assert.Null(testReceiver.ReceiveNextPacket());
        }

        [Fact]
        public void TestCreateHeaderAfterSingleMessage()
        {
            var payload = new byte[] { 5, 6, 7, 8 };
            var mockUdpClient = new Mocks.MockUdpClient(new List<byte[]>{
                new PacketHeader{
                    Sequence = 0,
                    LastAck = 0,
                    AckBits = 0,
                }.AddBytes(payload),
                new PacketHeader{
                    Sequence = 1,
                    LastAck = 0,
                    AckBits = 0,
                }.AddBytes(payload),
            });

            var testReceiver = new PacketReceiver(mockUdpClient);

            testReceiver.ReceiveNextPacket();
            testReceiver.ReceiveNextPacket();

            var headerToTest = testReceiver.CreateNextHeader();
            Assert.Equal(1, headerToTest.LastAck);

            UInt32 expectedBits = 0b_0000_0000_0000_0000_0000_0000_0000_0001;

            Console.WriteLine($"expectedBits: {Convert.ToString(expectedBits, toBase: 2)}");
            Console.WriteLine($"headerToTest.AckBits:  {Convert.ToString(headerToTest.AckBits, toBase: 2)}");
            Assert.Equal(expectedBits, headerToTest.AckBits);
        }

        [Fact]
        public void TestCreateNextHeader()
        {
            var payload = new byte[] { 5, 6, 7, 8 };
            var mockUdpClient = new Mocks.MockUdpClient(new List<byte[]>{
                new PacketHeader{
                    Sequence = 20,
                    AckBits = 23,
                    LastAck = 225
                }.AddBytes(payload),
                new PacketHeader{
                    Sequence = 25,
                    AckBits = 23,
                    LastAck = 225
                }.AddBytes(payload),
                new PacketHeader{
                    Sequence = 65535,
                    AckBits = 23,
                    LastAck = 225
                }.AddBytes(payload),
                new PacketHeader{
                    Sequence = 23,
                    AckBits = 23,
                    LastAck = 225
                }.AddBytes(payload),
                new PacketHeader{
                    Sequence = 65530,
                    AckBits = 23,
                    LastAck = 225
                }.AddBytes(payload),
            });

            var testReceiver = new PacketReceiver(mockUdpClient);

            testReceiver.ReceiveNextPacket();
            testReceiver.ReceiveNextPacket();
            testReceiver.ReceiveNextPacket();
            testReceiver.ReceiveNextPacket();
            testReceiver.ReceiveNextPacket();

            var headerToTest = testReceiver.CreateNextHeader();
            Assert.Equal(25, headerToTest.LastAck);

            UInt32 expectedBits = 0b_0100_0010_0000_0000_0000_0000_0001_0010;

            //Console.WriteLine($"expectedBits: {Convert.ToString(expectedBits, toBase: 2)}");
            //Console.WriteLine($"headerToTest.AckBits:  {Convert.ToString(headerToTest.AckBits, toBase: 2)}");
            Assert.Equal(expectedBits, headerToTest.AckBits);
        }

        [Fact]
        public void TestAckBitsConstruction()
        {
            var sequences = new List<ushort> { 10, 12, 15, 16, 18, 25, 32 };
            SequenceBuffer<Tuple<bool>> buffer = new SequenceBuffer<Tuple<bool>>();
            sequences.ForEach(s => buffer.AddEntry(s, new Tuple<bool>(true)));

            var header = PacketHeader.Create(buffer);
            Assert.Equal(32, header.LastAck);
            UInt32 expectedBits = 0b_0000_0000_0010_1001_1010_0000_0100_0000;

            Assert.Equal(expectedBits, header.AckBits);

            // acks come order the other way
            sequences.Reverse();
            Assert.Equal(sequences, header.GetAcks());
        }
    }
}