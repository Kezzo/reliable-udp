using ReliableUDP.MessageFactory;
using ReliableUDP.Packets;
using ReliableUDP.SequenceBuffer;

namespace ReliableUDP.Messages;

public class MessageReceiver
{
    private readonly PacketReceiver packetReceiver;
    private readonly SequenceBuffer<BaseMessage> receivedMessages;
    private readonly Dictionary<byte, IMessageFactory> messageFactories;

    // TODO: allow being receiving with any message id?
    private UInt16 nextMessageIdToReceive = 0;

    public MessageReceiver(IUdpClient udpClient)
    {
        packetReceiver = new PacketReceiver(udpClient);
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

    // TODO: support unordered message receival
    public List<BaseMessage> GetReceivedMessages()
    {
        var messagesToReturn = new List<BaseMessage>();

        while(true)
        {
            var nextMessage = receivedMessages.GetEntry(nextMessageIdToReceive);

            if(nextMessage == null)
            {
                break;
            }

            receivedMessages.RemoveEntry(nextMessageIdToReceive);
            nextMessageIdToReceive++;
            messagesToReturn.Add(nextMessage);
        }
        

        return messagesToReturn;
    }

    public async Task<List<UInt16>?> ReceiveAllPackets()
    {
        var packet = await packetReceiver.ReceiveNextPacket();
        if(packet == null)
        {
            return null;
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
            return null;
        }

        return packet.Header.GetAcks();
    }

    public PacketHeader CreateNextHeader()
    {
        return packetReceiver.CreateNextHeader();
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
}