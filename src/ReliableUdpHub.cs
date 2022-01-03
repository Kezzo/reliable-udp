using ReliableUDP.MessageFactory;
using ReliableUDP.Messages;

namespace ReliableUDP;

public class ReliableUdpHub
{
    private readonly MessageSender sender;
    private readonly MessageReceiver receiver;

    public ReliableUdpHub(IUdpClient udpClient)
    {
        sender = new MessageSender(udpClient);
        receiver = new MessageReceiver(udpClient);
    }

    public void RegisterMessageFactory<T>(byte messageTypeId, IMessageFactory factory)
    {
        receiver.RegisterMessageFactory<T>(messageTypeId, factory);
    }

    public void QueueMessage(BaseMessage message)
    {
        sender.QueueMessage(message);
    }

    public Task SendQueuedMessages()
    {
        return sender.SendQueuedMessages(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), 
            receiver.CreateNextHeader());
    }

    public async Task<List<BaseMessage>?> GetReceivedMessages()
    {
        var acks = await receiver.ReceiveAllPackets();
        sender.AckMessages(acks);
        return receiver.GetReceivedMessages();
    }
}