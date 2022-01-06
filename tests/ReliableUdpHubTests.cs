using System.Collections.Generic;
using ReliableUdp.MessageFactory;
using ReliableUdp.Messages;
using ReliableUdp.Tests.Messages;
using ReliableUdp.Tests.Mocks;
using Xunit;

namespace ReliableUdp.Tests
{
    public class ReliableUdpHubTests
    {
        [Fact]
        public async void TestSendAndReceive()
        {
            var udpClient1 = new MockUdpClient(null);
            var hub1 = new ReliableUdpHub(udpClient1);
            hub1.RegisterMessageFactory<TestMessage>(10, new MessageFactory<TestMessage>());

            var udpClient2 = new MockUdpClient(null);
            var hub2 = new ReliableUdpHub(udpClient2);
            hub2.RegisterMessageFactory<TestMessage>(10, new MessageFactory<TestMessage>());

            // so clients forward datagrams to each other
            udpClient1.ConnectToClient(udpClient2);
            udpClient2.ConnectToClient(udpClient1);

            var msg1 = new TestMessage { ID = 0, Payload = new byte[] { 3, 4, 5, 6} };
            hub1.QueueMessage(msg1);
            await hub1.SendQueuedMessages();

            TestMessagesReceivedInOrder(await hub2.GetReceivedMessages(), msg1);
            // test if messages are only returned once
            Assert.Empty(await hub2.GetReceivedMessages());
            // no msg returned when no received/sent
            Assert.Empty(await hub1.GetReceivedMessages());

            var msg2 = new TestMessage { ID = 2, Payload = new byte[] { 4, 4, 5, 6} };
            var msg3 = new TestMessage { ID = 3, Payload = new byte[] { 5, 4, 5, 6} };
            var msg4 = new TestMessage { ID = 4, Payload = new byte[] { 6, 4, 5, 6} };
            hub2.QueueMessage(msg2);
            hub2.QueueMessage(msg3);
            hub2.QueueMessage(msg4);

            var msg5 = new TestMessage { ID = 5, Payload = new byte[] { 7, 4, 5, 6} };
            var msg6 = new TestMessage { ID = 6, Payload = new byte[] { 8, 4, 5, 6} };
            hub1.QueueMessage(msg5);
            hub1.QueueMessage(msg6);

            // still empty since only queing happened
            Assert.Empty(await hub1.GetReceivedMessages());
            Assert.Empty(await hub2.GetReceivedMessages());

            await hub2.SendQueuedMessages();
            await hub1.SendQueuedMessages();

            TestMessagesReceivedInOrder(await hub1.GetReceivedMessages(), msg2, msg3, msg4);
            TestMessagesReceivedInOrder(await hub2.GetReceivedMessages(), msg5, msg6);
        }

        [Fact]
        public async void TestReliableReceival()
        {
            var timestampProvider = new MockTimestampProvider();
            var udpClient1 = new MockUdpClient(null);
            var hub1 = new ReliableUdpHub(udpClient1, timestampProvider);
            hub1.RegisterMessageFactory<TestMessage>(10, new MessageFactory<TestMessage>());

            var udpClient2 = new MockUdpClient(null);
            var hub2 = new ReliableUdpHub(udpClient2);
            hub2.RegisterMessageFactory<TestMessage>(10, new MessageFactory<TestMessage>());

            // so clients forward datagrams to each other
            udpClient1.ConnectToClient(udpClient2);
            udpClient2.ConnectToClient(udpClient1);

            var msg1 = new TestMessage { ID = 0, Payload = new byte[] { 3, 4, 5, 6} };
            hub1.QueueMessage(msg1);

            udpClient1.DropNextOutgoingPacket();

            await hub1.SendQueuedMessages();
            // since outgoing message was dropped, none received here
            Assert.Empty(await hub2.GetReceivedMessages());

            timestampProvider.SetTimestamp(50);

            await hub1.SendQueuedMessages();
            // still empty since time has not passed enough for re-send
            Assert.Empty(await hub2.GetReceivedMessages());

            timestampProvider.SetTimestamp(100);

            udpClient2.DropNextIncomingPacket();

            await hub1.SendQueuedMessages();
            // since incoming message was dropped, none received here
            Assert.Empty(await hub2.GetReceivedMessages());

            await hub1.SendQueuedMessages();
            // again empty since time has not passed enough for re-send
            Assert.Empty(await hub2.GetReceivedMessages());

            timestampProvider.SetTimestamp(200);

            await hub1.SendQueuedMessages();
            // msg should be received now since enough time has passed for re-send
            TestMessagesReceivedInOrder(await hub2.GetReceivedMessages(), msg1);
        }

        private void TestMessagesReceivedInOrder(List<BaseMessage> recvMsgs, params TestMessage[] sentMsgs)
        {
            Assert.Equal(sentMsgs.Length, recvMsgs.Count);

            for (int i = 0; i < recvMsgs.Count; i++)
            {
                var msg = recvMsgs[i];
                Assert.IsType<TestMessage>(msg);
                var testMsg = msg as TestMessage;

                Assert.Equal(sentMsgs[i].ID, testMsg.ID);
                Assert.Equal(sentMsgs[i].Payload, testMsg.Payload);
            }
        }
    }
}