using System;
using System.Collections.Generic;
using System.IO;
using ReliableUDP.Messages;
using ReliableUDP.Packets;
using ReliableUDP.Tests.Mocks;
using Xunit;

namespace ReliableUDP.Tests.Messages
{
    public class MessageSenderTests
    {
        [Fact]
        public void TestMessageQueueingFieldSetup()
        {
            var udpClient = new MockUdpClient(null);
            var messageSender = new MessageSender(udpClient);
            
            var msg1 = new TestMessage{ 
                ID = 0, 
                IsAcked = true, 
                LastSentTimestamp = 45903485, 
                MessageTypeId = 50646, 
                MessageUid = 49345 
            };

            Assert.Throws<InvalidOperationException>(() => messageSender.QueueMessage(msg1));

            messageSender.RegisterMessageTypeId(typeof(TestMessage), 10);
            messageSender.QueueMessage(msg1);

            // check if falsely set fields have been reset correctly
            Assert.False(msg1.IsAcked);
            Assert.Null(msg1.LastSentTimestamp);
            Assert.Equal(10, msg1.MessageTypeId);
            Assert.Equal(0, msg1.MessageUid);
        }
        
        [Fact]
        public async void TestQueueingAndSendingMessages()
        {
            var udpClient = new MockUdpClient(null);
            var messageSender = new MessageSender(udpClient);
            messageSender.RegisterMessageTypeId(typeof(TestMessage), 10);

            var msg1 = new TestMessage{ ID = 0 };
            var msg2 = new TestMessage{ ID = 1 };
            var msg3 = new TestMessage{ ID = 2 };

            messageSender.QueueMessage(msg1);
            messageSender.QueueMessage(msg2);
            messageSender.QueueMessage(msg3);

            // no message should be sent yet
            Assert.Empty(udpClient.SentDatagrams);

            await messageSender.SendQueuedMessages(0, new PacketHeader());

            // all message packed into one packet
            Assert.Single(udpClient.SentDatagrams);

            TestMessagesIncludedInPacket(udpClient.SentDatagrams[0], msg1, msg2, msg3);
        }

        [Fact]
        public async void TestPayloadConstructionMessageSize()
        {
            var udpClient = new MockUdpClient(null);
            var messageSender = new MessageSender(udpClient);
            messageSender.RegisterMessageTypeId(typeof(TestMessage), 10);

            var msg1 = new TestMessage{ ID = 0, Payload = new byte[237] };

            // too big message to fit into regular max payload size, will be sent separately
            var msg2 = new TestMessage{ ID = 1, Payload = new byte[1000] };

            // too big to fit into same packet as msg1, will be sent in next packet
            var msg3 = new TestMessage{ ID = 2, Payload = new byte[300] };
            var msg4 = new TestMessage{ ID = 3, Payload = new byte[247] };

            messageSender.QueueMessage(msg1);
            messageSender.QueueMessage(msg2);
            messageSender.QueueMessage(msg3);
            messageSender.QueueMessage(msg4);

            // no message should be sent yet
            Assert.Empty(udpClient.SentDatagrams);

            await messageSender.SendQueuedMessages(0, new PacketHeader());

            // message packed into separate packets
            Assert.Equal(3, udpClient.SentDatagrams.Count);

            TestMessagesIncludedInPacket(udpClient.SentDatagrams[0], msg1, msg4);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[1], msg2);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[2], msg3);
        }

        [Fact]
        public async void TestPayloadConstructionTimeAndAckBased()
        {
            var udpClient = new MockUdpClient(null);
            var messageSender = new MessageSender(udpClient);
            messageSender.RegisterMessageTypeId(typeof(TestMessage), 10);

            var msg1 = new TestMessage{ ID = 0 };
            var msg2 = new TestMessage{ ID = 1 };

            messageSender.QueueMessage(msg1);
            messageSender.QueueMessage(msg2);
            Assert.Empty(udpClient.SentDatagrams);
            await messageSender.SendQueuedMessages(0, new PacketHeader());
            Assert.Single(udpClient.SentDatagrams);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[0], msg1, msg2);

            var msg3 = new TestMessage{ ID = 2 };
            messageSender.QueueMessage(msg3);
            await messageSender.SendQueuedMessages(50, new PacketHeader());
            Assert.Equal(2, udpClient.SentDatagrams.Count);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[1], msg3);

            var msg4 = new TestMessage{ ID = 3 };
            messageSender.QueueMessage(msg4);
            await messageSender.SendQueuedMessages(75, new PacketHeader());
            Assert.Equal(3, udpClient.SentDatagrams.Count);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[2], msg4);

            await messageSender.SendQueuedMessages(100, new PacketHeader());
            Assert.Equal(4, udpClient.SentDatagrams.Count);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[3], msg1, msg2);

            await messageSender.SendQueuedMessages(149, new PacketHeader());
            Assert.Equal(4, udpClient.SentDatagrams.Count);

            await messageSender.SendQueuedMessages(150, new PacketHeader());
            Assert.Equal(5, udpClient.SentDatagrams.Count);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[4], msg3);

            messageSender.AckMessages(new List<ushort>{ 2 });
            await messageSender.SendQueuedMessages(225, new PacketHeader());
            Assert.Equal(6, udpClient.SentDatagrams.Count);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[5], msg1, msg2);

            messageSender.AckMessages(new List<ushort>{ 0 });
            await messageSender.SendQueuedMessages(300, new PacketHeader());
            Assert.Equal(7, udpClient.SentDatagrams.Count);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[6], msg3);

            messageSender.AckMessages(new List<ushort>{ 1 });
            await messageSender.SendQueuedMessages(400, new PacketHeader());
            Assert.Equal(7, udpClient.SentDatagrams.Count);
        }

        private void TestMessagesIncludedInPacket(byte[] datagram, params TestMessage[] msgs)
        {
            var packet = new Packet(datagram);
            var ms = new MemoryStream(packet.Payload.Array, packet.Payload.Offset, packet.Payload.Count);
            var reader = new BinaryReader(ms);
            
            foreach (var msg in msgs)
            {
                // read away message type id first
                reader.ReadUInt16();
                
                var sentMsg = new TestMessage();
                sentMsg.Deserialize(reader);
                Assert.Equal(msg.ID, sentMsg.ID);

                if(sentMsg.Payload != null && msg.Payload != null)
                {
                    Assert.Equal(msg.Payload.Length, sentMsg.Payload.Length);
                }
            }
        }
    }
    }