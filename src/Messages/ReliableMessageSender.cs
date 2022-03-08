using System.Collections.Generic;
using ReliableUdp.SequenceBuffer;

namespace ReliableUdp.Messages
{
    public class ReliableMessageSender
    {
        private readonly SequenceBuffer<BaseMessage> messagesToSend;
        private readonly SequenceBuffer<List<ushort>> packetToMessageLookup;

        private ushort nextMessageUidToSend = 0;
        private ushort oldestUnackedMessageId = 0;

        public ReliableMessageSender()
        {
            messagesToSend = new SequenceBuffer<BaseMessage>();
            packetToMessageLookup = new SequenceBuffer<List<ushort>>();
        }

        public void AckMessages(List<ushort> packetAcks)
        {
            if(packetAcks == null)
            {
                return;
            }

            for (int i = 0; i < packetAcks.Count; i++)
            {
                MarkSendBufferMessagesAcked(packetAcks[i]);
                UpdateOldestAckedMessageId();
            }
        }

        public void OnMessageQueued(BaseMessage message)
        {
            message.IsReliable = true;
            message.MessageUid = nextMessageUidToSend;
            nextMessageUidToSend++;

            messagesToSend.AddEntry(message.MessageUid, message);
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

        public void AddMessagesToPayload(PayloadPacker packer, long timestampNow, out List<ushort> messageIds)
        {
            messageIds = new List<ushort>();
            var nextMessageIdToCheck = oldestUnackedMessageId;

            while(!packer.IsFull)
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

                if(!packer.TryPackMessage(message))
                {
                    continue;
                }

                // overwrite send buffer entry with new sent time
                message.LastSentTimestamp = timestampNow;
                messagesToSend.AddEntry(message.MessageUid, message);

                messageIds.Add(message.MessageUid);
            }
        }

        public void OnPacketSent(ushort packetSequence, List<ushort> messageIds)
        {
            packetToMessageLookup.AddEntry(packetSequence, messageIds);
        }
    }
}