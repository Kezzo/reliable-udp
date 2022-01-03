using ReliableUDP.Packets;
using ReliableUDP.SequenceBuffer;

namespace ReliableUDP.Messages;

public class MessageSender
{
    private readonly PacketSender packetSender;
    private readonly SequenceBuffer<BaseMessage> messagesToSend;
    private readonly SequenceBuffer<List<UInt16>> packetToMessageLookup;

    private UInt16 oldestUnackedMessageId = 0;
    private UInt16 nextMessageIdToSend = 0;

    // Keep packet at this max or below to avoid ip packet fragmentation
    // and allow each packet to be transmitted in one ethernet frame
    private const int MAX_PACKET_PAYLOAD_SIZE = 508;

    public MessageSender(IUdpClient udpClient)
    {
        packetSender = new PacketSender(udpClient);
        messagesToSend = new SequenceBuffer<BaseMessage>();
        packetToMessageLookup = new SequenceBuffer<List<UInt16>>();
    }
    public void AckMessages(List<UInt16>? acks)
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
        var packetSequence = await packetSender.SendPacket(
            headerToUse, ConstructNextPayload(timestampNow, out List<UInt16> messageIds));
        packetToMessageLookup.AddEntry(packetSequence, messageIds);
    }

    private void MarkSendBufferMessagesAcked(UInt16 ackedPacketSequence)
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

    private byte[] ConstructNextPayload(long timestampNow, out List<UInt16> messageIds)
    {
        messageIds = new List<ushort>();

        //TODO: Re-use these
        var ms = new MemoryStream(MAX_PACKET_PAYLOAD_SIZE);
        var bufferedStream = new BufferedStream(ms);
        var writer = new BinaryWriter(bufferedStream);

        var nextMessageIdToCheck = oldestUnackedMessageId;

        while(true)
        {
            var message = messagesToSend.GetEntry(nextMessageIdToCheck);
            nextMessageIdToCheck++;

            if(message == null)
            {
                // reached end of send message buffer
                break;
            }
            
            // TODO: Use RTT here
            if(message.IsAcked || (timestampNow - message.LastSentTimestamp) < 100)
            {
                // skip message
                continue;
            }

            // TODO: Only add message if it fits into buffer
            message.Serialize(writer);

            // does buffer fit into payload?
            if(bufferedStream.Position > ms.Capacity - ms.Position)
            {
                // Reset position back to last flushed position
                bufferedStream.Position = ms.Position;
                continue;
            }

            // writer bytes to payload and reset
            bufferedStream.Flush();

            // overwrite send buffer entry with new sent time
            message.LastSentTimestamp = timestampNow;
            messagesToSend.AddEntry(message.MessageId, message);
            messageIds.Add(message.MessageId);
        }

        byte[]? payload = null;
        if(ms.TryGetBuffer(out ArraySegment<byte> buffer))
        {
            payload = buffer.ToArray();
        } else {
            payload = new byte[0];
        }

        ms.Dispose();
        bufferedStream.Dispose();
        writer.Dispose();

        return payload;
    }
}