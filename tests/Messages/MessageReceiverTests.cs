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
        public void TestRegisterMessageFactory()
        {
            // arrive out of order
            var datagramsToTest = new List<byte[]>();
            var msg1 = new TestMessage{ ID = 1337, MessageTypeId = 10, MessageUid = 0, Payload = new byte[] { 3, 4, 7, 34, 200} };
            datagramsToTest.Add(GetTestDatagram(msg1));

            var mockUdpClient = new MockUdpClient(datagramsToTest);
            var receiver = new OrderedMessageReceiver(mockUdpClient);

            Assert.Throws<KeyNotFoundException>(() => receiver.ReceiveNextPacket());

            mockUdpClient.AddResultToReturn(GetTestDatagram(msg1));
            // add message factory with DIFFERENT message type id
            receiver.RegisterMessageFactory<MessageFactory<TestMessage>>(20, new MessageFactory<Messages.TestMessage>());
            Assert.Throws<KeyNotFoundException>(() => receiver.ReceiveNextPacket());

            mockUdpClient.AddResultToReturn(GetTestDatagram(msg1));
            // add message factory with CORRECT message type id
            receiver.RegisterMessageFactory<MessageFactory<TestMessage>>(10, new MessageFactory<Messages.TestMessage>());
            receiver.ReceiveNextPacket();

            TestReceivedCorrectlyInOrder(receiver.GetReceivedMessages(), msg1);
        }

        [Fact]
        public void TestReceiveReliableMessagesInOrder()
        {
            // arrive out of order
            var datagramsToTest = new List<byte[]>();

            var msg0 = new TestMessage{ ID = 0, IsReliable = true, MessageTypeId = 10, MessageUid = 0, Payload = new byte[] { 3, 4, 7, 34, 200} };
            var msg1 = new TestMessage{ ID = 1, IsReliable = true, MessageTypeId = 10, MessageUid = 1, Payload = new byte[] { 3, 5, 7, 34, 100} };
            var msg2 = new TestMessage{ ID = 2, IsReliable = true, MessageTypeId = 10, MessageUid = 2, Payload = new byte[] { } };
            var msg3 = new TestMessage{ ID = 3, IsReliable = true, MessageTypeId = 10, MessageUid = 3, Payload = new byte[] { 3, 7, 7, 34, 250} };
            var msg4 = new TestMessage{ ID = 4, IsReliable = true, MessageTypeId = 10, MessageUid = 4, Payload = new byte[] { 3, 8, 7, 34, 170} };

            datagramsToTest.Add(GetTestDatagram(msg4, msg3));
            datagramsToTest.Add(GetTestDatagram(msg1, msg2));
            datagramsToTest.Add(GetTestDatagram(msg0));

            var mockUdpClient = new MockUdpClient(datagramsToTest);
            var receiver = new OrderedMessageReceiver(mockUdpClient);
            receiver.RegisterMessageFactory<MessageFactory<TestMessage>>(10, new MessageFactory<Messages.TestMessage>());

            // receive next packets
            receiver.ReceiveNextPacket();
            receiver.ReceiveNextPacket();

            // should be empty here since very first message that should be returned first has not arrived yet.
            Assert.Empty(receiver.GetReceivedMessages());

            receiver.ReceiveNextPacket();

            TestReceivedCorrectlyInOrder(receiver.GetReceivedMessages(), msg0, msg1, msg2, msg3, msg4);
        }

        [Fact]
        public void TestReceiveUnreliableMessagesInBestEffortOrder()
        {
            // arrive out of order
            var datagramsToTest = new List<byte[]>();

            var msg0 = new TestMessage{ ID = 0, IsReliable = false, MessageTypeId = 10, MessageUid = 0, Payload = new byte[] { 3, 4, 7, 34, 200} };
            var msg1 = new TestMessage{ ID = 1, IsReliable = false, MessageTypeId = 10, MessageUid = 1, Payload = new byte[] { 3, 5, 7, 34, 100} };
            var msg2 = new TestMessage{ ID = 2, IsReliable = false, MessageTypeId = 10, MessageUid = 2, Payload = new byte[] { } };
            var msg3 = new TestMessage{ ID = 3, IsReliable = false, MessageTypeId = 10, MessageUid = 3, Payload = new byte[] { 3, 7, 7, 34, 250} };
            var msg4 = new TestMessage{ ID = 4, IsReliable = false, MessageTypeId = 10, MessageUid = 4, Payload = new byte[] { 3, 8, 7, 34, 170} };
            var msg5 = new TestMessage{ ID = 5, IsReliable = false, MessageTypeId = 10, MessageUid = 5, Payload = new byte[] { 3, 8, 7, 34, 120} };

            datagramsToTest.Add(GetTestDatagram(msg4, msg3));
            datagramsToTest.Add(GetTestDatagram(msg1, msg2));
            datagramsToTest.Add(GetTestDatagram(msg0, msg5));

            var mockUdpClient = new MockUdpClient(datagramsToTest);
            var receiver = new OrderedMessageReceiver(mockUdpClient);
            receiver.RegisterMessageFactory<MessageFactory<TestMessage>>(10, new MessageFactory<Messages.TestMessage>());

            // receive next packets
            receiver.ReceiveNextPacket();
            receiver.ReceiveNextPacket();
            TestReceivedCorrectlyInOrder(receiver.GetReceivedMessages(), msg1, msg2, msg3, msg4);

            receiver.ReceiveNextPacket();
            TestReceivedCorrectlyInOrder(receiver.GetReceivedMessages(), msg0, msg5);
        }

        [Fact]
        public void TestReceiveMixedMessagesInOrder()
        {
            // arrive out of order
            var datagramsToTest = new List<byte[]>();

            var reliableMsg0 = new TestMessage{ ID = 0, IsReliable = true, MessageTypeId = 10, MessageUid = 0, Payload = new byte[] { 3, 4, 7, 34, 200} };
            var reliableMsg1 = new TestMessage{ ID = 1, IsReliable = true, MessageTypeId = 10, MessageUid = 1, Payload = new byte[] { } };
            var reliableMsg2 = new TestMessage{ ID = 2, IsReliable = true, MessageTypeId = 10, MessageUid = 2, Payload = new byte[] { 3, 8, 7, 34, 170} };
            var unreliableMsg0 = new TestMessage{ ID = 0, IsReliable = false, MessageTypeId = 10, MessageUid = 0, Payload = new byte[] { 3, 5, 7, 34, 100} };
            var unreliableMsg1 = new TestMessage{ ID = 1, IsReliable = false, MessageTypeId = 10, MessageUid = 1, Payload = new byte[] { 3, 7, 7, 34, 250} };
            var unreliableMsg2 = new TestMessage{ ID = 2, IsReliable = false, MessageTypeId = 10, MessageUid = 2, Payload = new byte[] { 3, 8, 7, 34, 120} };

            datagramsToTest.Add(GetTestDatagram(reliableMsg2, unreliableMsg1));
            datagramsToTest.Add(GetTestDatagram(unreliableMsg0, reliableMsg1));
            datagramsToTest.Add(GetTestDatagram(reliableMsg0, unreliableMsg2));

            var mockUdpClient = new MockUdpClient(datagramsToTest);
            var receiver = new OrderedMessageReceiver(mockUdpClient);
            receiver.RegisterMessageFactory<MessageFactory<TestMessage>>(10, new MessageFactory<Messages.TestMessage>());

            // receive next packets
            receiver.ReceiveNextPacket();
            receiver.ReceiveNextPacket();
            TestReceivedCorrectlyInOrder(receiver.GetReceivedMessages(), unreliableMsg0, unreliableMsg1);

            receiver.ReceiveNextPacket();
            TestReceivedCorrectlyInOrder(receiver.GetReceivedMessages(), reliableMsg0, reliableMsg1, reliableMsg2, unreliableMsg2);
        }

        [Fact]
        public void TestReceiveMixedMessagesUnordered()
        {
            // arrive out of order
            var datagramsToTest = new List<byte[]>();

            var reliableMsg0 = new TestMessage{ ID = 0, IsReliable = true, MessageTypeId = 10, MessageUid = 0, Payload = new byte[] { 3, 4, 7, 34, 200} };
            var reliableMsg1 = new TestMessage{ ID = 1, IsReliable = true, MessageTypeId = 10, MessageUid = 1, Payload = new byte[] { } };
            var reliableMsg2 = new TestMessage{ ID = 2, IsReliable = true, MessageTypeId = 10, MessageUid = 2, Payload = new byte[] { 3, 8, 7, 34, 170} };
            var unreliableMsg0 = new TestMessage{ ID = 0, IsReliable = false, MessageTypeId = 10, MessageUid = 0, Payload = new byte[] { 3, 5, 7, 34, 100} };
            var unreliableMsg1 = new TestMessage{ ID = 1, IsReliable = false, MessageTypeId = 10, MessageUid = 1, Payload = new byte[] { 3, 7, 7, 34, 250} };
            var unreliableMsg2 = new TestMessage{ ID = 2, IsReliable = false, MessageTypeId = 10, MessageUid = 2, Payload = new byte[] { 3, 8, 7, 34, 120} };

            datagramsToTest.Add(GetTestDatagram(reliableMsg2, unreliableMsg1));
            datagramsToTest.Add(GetTestDatagram(unreliableMsg0, reliableMsg1));
            datagramsToTest.Add(GetTestDatagram(reliableMsg0, unreliableMsg2));

            var mockUdpClient = new MockUdpClient(datagramsToTest);
            var receiver = new UnorderedMessageReceiver(mockUdpClient);
            receiver.RegisterMessageFactory<MessageFactory<TestMessage>>(10, new MessageFactory<Messages.TestMessage>());

            // receive next packets
            receiver.ReceiveNextPacket();
            receiver.ReceiveNextPacket();
            TestReceivedCorrectlyInOrder(receiver.GetReceivedMessages(), reliableMsg2, unreliableMsg1, unreliableMsg0, reliableMsg1);

            receiver.ReceiveNextPacket();
            TestReceivedCorrectlyInOrder(receiver.GetReceivedMessages(), reliableMsg0, unreliableMsg2);
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