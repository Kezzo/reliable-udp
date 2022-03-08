using System;
using System.Collections.Generic;
using System.IO;
using ReliableUdp.Messages;
using ReliableUdp.Packets;
using ReliableUdp.Tests.Mocks;
using Xunit;

namespace ReliableUdp.Tests.Messages
{
    public class MessageSenderTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestMessageQueueingFieldSetup(bool sendReliable)
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

            Assert.Throws<InvalidOperationException>(() => messageSender.QueueMessage(msg1, true));

            messageSender.RegisterMessageTypeId(typeof(TestMessage), 10);
            messageSender.QueueMessage(msg1, sendReliable);

            // check if falsely set fields have been reset correctly
            Assert.False(msg1.IsAcked);
            Assert.Null(msg1.LastSentTimestamp);
            Assert.Equal(10, msg1.MessageTypeId);
            Assert.Equal(0, msg1.MessageUid);
        }
        
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestQueueingAndSendingMessages(bool sendReliable)
        {
            var udpClient = new MockUdpClient(null);
            var messageSender = new MessageSender(udpClient);
            messageSender.RegisterMessageTypeId(typeof(TestMessage), 10);

            var msg1 = new TestMessage{ ID = 0 };
            var msg2 = new TestMessage{ ID = 1 };
            var msg3 = new TestMessage{ ID = 2 };

            messageSender.QueueMessage(msg1, sendReliable);
            messageSender.QueueMessage(msg2, sendReliable);
            messageSender.QueueMessage(msg3, sendReliable);

            // no message should be sent yet
            Assert.Empty(udpClient.SentDatagrams);

            messageSender.SendQueuedMessages(0, new PacketHeader());

            // all messages packed into one packet
            Assert.Single(udpClient.SentDatagrams);

            TestMessagesIncludedInPacket(udpClient.SentDatagrams[0], msg1, msg2, msg3);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestPayloadConstructionMessageSize(bool sendReliable)
        {
            var udpClient = new MockUdpClient(null);
            var messageSender = new MessageSender(udpClient);
            messageSender.RegisterMessageTypeId(typeof(TestMessage), 10);

            var msg1 = new TestMessage{ ID = 0, Payload = new byte[236] };

            // too big message to fit into regular max payload size, will be sent separately
            var msg2 = new TestMessage{ ID = 1, Payload = new byte[1000] };

            // too big to fit into same packet as msg1, will be sent in next packet
            var msg3 = new TestMessage{ ID = 2, Payload = new byte[300] };
            var msg4 = new TestMessage{ ID = 3, Payload = new byte[246] };

            messageSender.QueueMessage(msg1, sendReliable);
            messageSender.QueueMessage(msg2, sendReliable);
            messageSender.QueueMessage(msg3, sendReliable);
            messageSender.QueueMessage(msg4, sendReliable);

            // no message should be sent yet
            Assert.Empty(udpClient.SentDatagrams);

            messageSender.SendQueuedMessages(0, new PacketHeader());

            // message packed into separate packets
            Assert.Equal(3, udpClient.SentDatagrams.Count);

            TestMessagesIncludedInPacket(udpClient.SentDatagrams[0], msg1, msg4);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[1], msg2);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[2], msg3);
        }

        [Fact]
        public void TestPayloadConstructionTimeAndAckBased()
        {
            var udpClient = new MockUdpClient(null);
            var messageSender = new MessageSender(udpClient);
            messageSender.RegisterMessageTypeId(typeof(TestMessage), 10);

            var msg1 = new TestMessage{ ID = 0 };
            var msg2 = new TestMessage{ ID = 1 };

            messageSender.QueueMessage(msg1, true);
            messageSender.QueueMessage(msg2, true);
            Assert.Empty(udpClient.SentDatagrams);
            messageSender.SendQueuedMessages(0, new PacketHeader());
            Assert.Single(udpClient.SentDatagrams);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[0], msg1, msg2);

            var msg3 = new TestMessage{ ID = 2 };
            var msg4 = new TestMessage{ ID = 2 };
            messageSender.QueueMessage(msg3, true);
            messageSender.QueueMessage(msg4, false);
            messageSender.SendQueuedMessages(50, new PacketHeader());
            Assert.Equal(2, udpClient.SentDatagrams.Count);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[1], msg3, msg4);

            var msg5 = new TestMessage{ ID = 3 };
            messageSender.QueueMessage(msg5, true);
            messageSender.SendQueuedMessages(75, new PacketHeader());
            Assert.Equal(3, udpClient.SentDatagrams.Count);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[2], msg5);

            messageSender.SendQueuedMessages(100, new PacketHeader());
            Assert.Equal(4, udpClient.SentDatagrams.Count);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[3], msg1, msg2);

            messageSender.SendQueuedMessages(149, new PacketHeader());
            Assert.Equal(4, udpClient.SentDatagrams.Count);

            messageSender.SendQueuedMessages(150, new PacketHeader());
            Assert.Equal(5, udpClient.SentDatagrams.Count);
            // here msg4 should not be included because it was unreliable and therefore only sent once
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[4], msg3);

            messageSender.AckMessages(new List<ushort>{ 2 });
            messageSender.SendQueuedMessages(225, new PacketHeader());
            Assert.Equal(6, udpClient.SentDatagrams.Count);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[5], msg1, msg2);

            messageSender.AckMessages(new List<ushort>{ 0 });
            messageSender.SendQueuedMessages(300, new PacketHeader());
            Assert.Equal(7, udpClient.SentDatagrams.Count);
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[6], msg3);

            messageSender.AckMessages(new List<ushort>{ 1 });
            messageSender.SendQueuedMessages(400, new PacketHeader());
            Assert.Equal(7, udpClient.SentDatagrams.Count);
        }

        [Fact]
        public void TestMessageSendingAfterAcking()
        {
            var udpClient = new MockUdpClient(null);
            var messageSender = new MessageSender(udpClient);
            messageSender.RegisterMessageTypeId(typeof(TestMessage), 10);

            var msg1 = new TestMessage{ ID = 0 };
            var msg2 = new TestMessage{ ID = 1 };

            messageSender.QueueMessage(msg1, true);
            messageSender.QueueMessage(msg2, true);
            Assert.Empty(udpClient.SentDatagrams);

            messageSender.SendQueuedMessages(0, new PacketHeader());
            Assert.Single(udpClient.SentDatagrams);
            
            TestMessagesIncludedInPacket(udpClient.SentDatagrams[0], msg1, msg2);

            messageSender.AckMessages(new List<ushort>{ 0 });

            var msg3 = new TestMessage{ ID = 2 };
            messageSender.QueueMessage(msg3, true);

            messageSender.SendQueuedMessages(100, new PacketHeader());
            Assert.Equal(2, udpClient.SentDatagrams.Count);

            TestMessagesIncludedInPacket(udpClient.SentDatagrams[1], msg3);
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

            // test that there are no extra messages
            Assert.Equal(ms.Length, ms.Position);
        }
    }
    }