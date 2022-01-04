using System.Collections.Generic;
using System.IO;
using ReliableUdp.MessageFactory;
using ReliableUdp.Messages;
using ReliableUdp.Packets;
using ReliableUdp.Tests.Mocks;
using Xunit;

namespace ReliableUdp.Tests.Messages
{
    public class MessageReceiverTests
    {
        [Fact]
        public async void TestRegisterMessageFactory()
        {
            // arrive out of order
            var datagramsToTest = new List<byte[]>();
            var msg1 = new TestMessage{ ID = 1337, MessageTypeId = 10, MessageUid = 0, Payload = new byte[] { 3, 4, 7, 34, 200} };
            datagramsToTest.Add(GetTestDatagram(msg1));

            var mockUdpClient = new MockUdpClient(datagramsToTest);
            var receiver = new MessageReceiver(mockUdpClient);

            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await receiver.ReceiveNextPacket());

            mockUdpClient.AddResultToReturn(GetTestDatagram(msg1));
            // add message factory with DIFFERENT message type id
            receiver.RegisterMessageFactory<MessageFactory<TestMessage>>(20, new MessageFactory<Messages.TestMessage>());
            await Assert.ThrowsAsync<KeyNotFoundException>(async () => await receiver.ReceiveNextPacket());

            mockUdpClient.AddResultToReturn(GetTestDatagram(msg1));
            // add message factory with CORRECT message type id
            receiver.RegisterMessageFactory<MessageFactory<TestMessage>>(10, new MessageFactory<Messages.TestMessage>());
            await receiver.ReceiveNextPacket();

            TestReceivedCorrectlyInOrder(receiver.GetReceivedMessages(), msg1);
        }

        [Fact]
        public async void TestReceiveMessagesInOrder()
        {
            // arrive out of order
            var datagramsToTest = new List<byte[]>();

            var msg0 = new TestMessage{ ID = 0, MessageTypeId = 10, MessageUid = 0, Payload = new byte[] { 3, 4, 7, 34, 200} };
            var msg1 = new TestMessage{ ID = 1, MessageTypeId = 10, MessageUid = 1, Payload = new byte[] { 3, 5, 7, 34, 100} };
            var msg2 = new TestMessage{ ID = 2, MessageTypeId = 10, MessageUid = 2, Payload = new byte[] { } };
            var msg3 = new TestMessage{ ID = 3, MessageTypeId = 10, MessageUid = 3, Payload = new byte[] { 3, 7, 7, 34, 250} };
            var msg4 = new TestMessage{ ID = 4, MessageTypeId = 10, MessageUid = 4, Payload = new byte[] { 3, 8, 7, 34, 170} };

            datagramsToTest.Add(GetTestDatagram(msg4, msg3));
            datagramsToTest.Add(GetTestDatagram(msg1, msg2));
            datagramsToTest.Add(GetTestDatagram(msg0));

            var mockUdpClient = new MockUdpClient(datagramsToTest);
            var receiver = new MessageReceiver(mockUdpClient);
            receiver.RegisterMessageFactory<MessageFactory<TestMessage>>(10, new MessageFactory<Messages.TestMessage>());

            // receive next packets
            await receiver.ReceiveNextPacket();
            await receiver.ReceiveNextPacket();

            // should be empty here since very first message that should be returned first has not arrived yet.
            Assert.Empty(receiver.GetReceivedMessages());

            await receiver.ReceiveNextPacket();

            TestReceivedCorrectlyInOrder(receiver.GetReceivedMessages(), msg0, msg1, msg2, msg3, msg4);
        }

        private void TestReceivedCorrectlyInOrder(List<BaseMessage> receivedMsgs, params TestMessage[] msgsSent)
        {
            Assert.Equal(msgsSent.Length, receivedMsgs.Count);

            for (int i = 0; i < receivedMsgs.Count; i++)
            {
                BaseMessage recvMsg = receivedMsgs[i];
                Assert.NotNull(recvMsg);
                Assert.IsType<TestMessage>(recvMsg);

                var testMessage = recvMsg as TestMessage;
                Assert.Equal(msgsSent[i].ID, testMessage.ID);
                Assert.Equal(msgsSent[i].Payload, testMessage.Payload);
            }
        }

        private byte[] GetTestDatagram(params TestMessage[] messages)
        {
            var packetHeader = new PacketHeader {
                Sequence = 234,
                LastAck = 345,
                AckBits = 456
            };

            var ms = new MemoryStream();
            var writer = new BinaryWriter(ms);

            foreach (var msg in messages)
            {
                msg.Serialize(writer);
            }

            return packetHeader.AddBytes(ms.ToArray());
        }
    }
}