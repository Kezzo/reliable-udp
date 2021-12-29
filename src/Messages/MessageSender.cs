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
            packetSender.OnPacketAcked(acks[i]);
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

        messagesToSend.AddEntry(nextMessageIdToSend, message);
        nextMessageIdToSend++;
    }

    public async Task SendQueuedMessages(PacketHeader headerToUse)
    {
        var packetSequence = await packetSender.SendPacket(
            headerToUse, ConstructNextPayload(out List<UInt16> messageIds));
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

    private byte[] ConstructNextPayload(out List<UInt16> messageIds)
    {
        messageIds = new List<ushort>();
        var timeNow = DateTimeOffset.UtcNow.Millisecond;

        //TODO: Re-use these
        var ms = new MemoryStream(MAX_PACKET_PAYLOAD_SIZE);
        var bufferedStream = new BufferedStream(ms);
        var writer = new BinaryWriter(bufferedStream);

        var messageIdToCheck = oldestUnackedMessageId;

        while(true)
        {
            var message = messagesToSend.GetEntry(messageIdToCheck);
            messageIdToCheck++;

            if(message == null)
            {
                // reached end of send message buffer
                break;
            }
            
            if(message.IsAcked || (timeNow - message.LastSentTimestamp) < 100)
            {
                // skip message
                continue;
            }

            // TODO: Only add message if it fits into buffer
            message.Serialize(writer);

            // does buffer fit into payload?
            if(bufferedStream.Position > ms.Length - ms.Position)
            {
                bufferedStream.Position = 0;
                continue;
            }

            // writer bytes to payload and reset
            bufferedStream.Flush();
            bufferedStream.Position = 0;

            // overwrite send buffer entry with new sent time
            message.LastSentTimestamp = timeNow;
            messagesToSend.AddEntry(messageIdToCheck, message);
            messageIds.Add(messageIdToCheck);
        }

        byte[]? payload = null;
        if(ms.TryGetBuffer(out ArraySegment<byte> buffer))
        {
            payload = buffer.ToArray();
        } else {
            payload = new byte[0];
        }

        ms.Dispose();
        writer.Dispose();

        return payload;
    }
}