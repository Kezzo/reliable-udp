using ReliableUDP.Packets;

namespace ReliableUDP;

public class MessageHub
{
    private readonly PacketSender sender;
    private readonly PacketReceiver receiver;
    private readonly SequenceBuffer<BaseMessage> messagesToSend;
    private readonly SequenceBuffer<List<UInt16>> packetToMessageLookup;
    private readonly SequenceBuffer<BaseMessage> receivedMessages;
    private readonly Dictionary<byte, IMessageFactory> messageFactories;
    
    private UInt16 oldestUnackedMessageId = 0;
    private UInt16 nextMessageIdToSend = 0;
    
    // TODO: allow being receiving with any message id?
    private UInt16 nextMessageIdToReceive = 0;

    // Keep packet at this max or below to avoid ip packet fragmentation
    // and allow each packet to be transmitted in one ethernet frame
    private const int MAX_PACKET_PAYLOAD_SIZE = 508;

    public MessageHub(IUdpClient udpClient)
    {
        sender = new PacketSender(udpClient);
        receiver = new PacketReceiver(udpClient);
        messagesToSend = new SequenceBuffer<BaseMessage>();
        packetToMessageLookup = new SequenceBuffer<List<UInt16>>();
        receivedMessages = new SequenceBuffer<BaseMessage>();
        messageFactories = new Dictionary<byte, IMessageFactory>();
    }

    public void RegisterMessageFactory<T>(byte messageTypeId, IMessageFactory factory)
    {
        if(messageFactories.ContainsKey(messageTypeId))
        {
            throw new InvalidOperationException("Message type with id was already registered.");
        }

        messageFactories.Add(messageTypeId, factory);
    }

    public void SendMessage(BaseMessage message)
    {
        if(message == null)
        {
            return;
        }

        messagesToSend.AddEntry(nextMessageIdToSend, message);
        nextMessageIdToSend++;
    }

    public BaseMessage? ReceiveMessage()
    {
        var nextMessage = receivedMessages.GetEntry(nextMessageIdToReceive);

        if(nextMessage != null)
        {
            receivedMessages.RemoveEntry(nextMessageIdToReceive);
            nextMessageIdToReceive++;
        }

        return nextMessage;
    }

    public async void Update()
    {
        await ReceiveAllPackets();

        var packetSequence = await sender.SendPacket(
            receiver.CreateNextHeader(), ConstructNextPayload(out List<UInt16> messageIds));
        packetToMessageLookup.AddEntry(packetSequence, messageIds);
    }

    private async Task ReceiveAllPackets()
    {
        var packet = await receiver.ReceiveNextPacket();
        if(packet == null)
        {
            return;
        }

        try 
        {
            ExtractMessagesFromPayload(packet.Payload);
        }
        catch(Exception e)
        {
            Console.WriteLine(e);
            // DO NOT ack packet if there was an exception when deserializing it.
            // TODO: track deserialization error and reject client after continuous errors.
            return;
        }

        var acks = packet.Header.GetAcks();
        for (int i = 0; i < acks.Count; i++)
        {
            sender.OnPacketAcked(acks[i]);
            MarkSendBufferMessagesAcked(acks[i]);
            UpdateOldestAckedMessageId();
        }
    }

    private void ExtractMessagesFromPayload(ArraySegment<byte> payload)
    {
        if(payload.Array == null)
        {
            Console.WriteLine("WARN: ExtractMessagesFromPayload: payload is null");
            return;
        }

        using(var ms = new MemoryStream(payload.Array, payload.Offset, payload.Count))
        {
            while(ms.Position < ms.Length)
            {
                using (BinaryReader reader = new BinaryReader(ms, System.Text.UTF8Encoding.UTF8, true))
                {
                    var messageTypeId = reader.ReadByte();
                    if(!messageFactories.TryGetValue(messageTypeId, out IMessageFactory? factory) || factory == null)
                    {
                        throw new Exception($"Couldn't find factory for message type id: {messageTypeId}");
                    }

                    var message = factory.CreateMessage(reader);
                    receivedMessages.AddEntry(message.MessageId, message);
                }
            }
        }
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