using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ReliableUDP.Packets;
using ReliableUDP.SequenceBuffer;

namespace ReliableUDP.Messages
{
    public class MessageSender
    {
        private readonly PacketSender packetSender;
        private readonly SequenceBuffer<BaseMessage> messagesToSend;
        private readonly SequenceBuffer<List<ushort>> packetToMessageLookup;

        private ushort oldestUnackedMessageId = 0;
        private ushort nextMessageIdToSend = 0;

        // Keep packet at this max or below to avoid ip packet fragmentation
        // and allow each packet to be transmitted in one ethernet frame
        private const int MAX_PACKET_PAYLOAD_SIZE = 508;

        public MessageSender(IUdpClient udpClient)
        {
            packetSender = new PacketSender(udpClient);
            messagesToSend = new SequenceBuffer<BaseMessage>();
            packetToMessageLookup = new SequenceBuffer<List<ushort>>();
        }
        public void AckMessages(List<ushort> acks)
        {
            if(acks == null)
            {
                return;
            }

            for (int i = 0; i < acks.Count; i++)
            {
                MarkSendBufferMessagesAcked(acks[i]);
                UpdateOldestAckedMessageId();
            }
        }

        public void QueueMessage(BaseMessage message)
        {
            if(message == null)
            {
                return;
            }

            message.MessageId = nextMessageIdToSend;
            messagesToSend.AddEntry(nextMessageIdToSend, message);

            nextMessageIdToSend++;
        }

        public async Task SendQueuedMessages(long timestampNow, PacketHeader headerToUse)
        {
            while(true)
            {
                // send packets until message queue has no message available for sending
                var nextPayload = ConstructNextPayload(timestampNow, out List<ushort> messageIds);

                if(nextPayload == null || nextPayload.Length == 0)
                {
                    return;
                }

                var packetSequence = await packetSender.SendPacket(
                    headerToUse, nextPayload);
                packetToMessageLookup.AddEntry(packetSequence, messageIds);
            }
        }

        private void MarkSendBufferMessagesAcked(ushort ackedPacketSequence)
        {
            var messageIds = packetToMessageLookup.GetEntry(ackedPacketSequence);

            if(messageIds != null)
            {
                for (int i = 0; i < messageIds.Count; i++)
                {
                    var message = messagesToSend.GetEntry(messageIds[i]);
                    if(message != null)
                    {
                        message.IsAcked = true;
                    }
                }

                packetToMessageLookup.RemoveEntry(ackedPacketSequence);
            }
        }

        private void UpdateOldestAckedMessageId()
        {
            while(true)
            {
                var message = messagesToSend.GetEntry(oldestUnackedMessageId);
                
                if(message == null || !message.IsAcked)
                {
                    return;
                }

                oldestUnackedMessageId++;
            }
        }

        private byte[] ConstructNextPayload(long timestampNow, out List<ushort> messageIds)
        {
            messageIds = new List<ushort>();

            //TODO: Re-use these
            var ms = new MemoryStream(MAX_PACKET_PAYLOAD_SIZE);
            var writer = new BinaryWriter(ms);

            var nextMessageIdToCheck = oldestUnackedMessageId;
            bool isPacketFull = false;

            while(!isPacketFull)
            {
                var message = messagesToSend.GetEntry(nextMessageIdToCheck);
                nextMessageIdToCheck++;

                if(message == null)
                {
                    // reached end of send message buffer
                    break;
                }
                
                // TODO: Use RTT here
                if(message.IsAcked || (message.LastSentTimestamp != null && (timestampNow - message.LastSentTimestamp) < 100))
                {
                    // skip message
                    continue;
                }

                var preWritePosition = ms.Position;
                var preWriteLength = ms.Length;

                // TODO: Only add message if it fits into buffer
                message.Serialize(writer);

                // allow sending huge payloads in separate packets
                if(messageIds.Count == 0 && ms.Position > MAX_PACKET_PAYLOAD_SIZE)
                {
                    // stop packing messages after this message
                    isPacketFull = true;
                }
                // does buffer fit into payload?
                else if(ms.Position > MAX_PACKET_PAYLOAD_SIZE)
                {
                    // Reset position back to last flushed position
                    ms.Position = preWritePosition;
                    ms.SetLength(preWriteLength);
                    continue;
                }

                // overwrite send buffer entry with new sent time
                message.LastSentTimestamp = timestampNow;
                messagesToSend.AddEntry(message.MessageId, message);
                messageIds.Add(message.MessageId);
            }

            ms.Dispose();
            writer.Dispose();
            
            return ms.ToArray();
        }
    }
}