using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReliableUDP.MessageFactory;
using ReliableUDP.Messages;

namespace ReliableUDP
{
    public class ReliableUdpHub
    {
        private readonly MessageSender sender;
        private readonly MessageReceiver receiver;

        public ReliableUdpHub(IUdpClient udpClient)
        {
            sender = new MessageSender(udpClient);
            receiver = new MessageReceiver(udpClient);
        }

        public void RegisterMessageFactory<T>(ushort messageTypeId, IMessageFactory factory)
        {
            sender.RegisterMessageTypeId(typeof(T), messageTypeId);
            receiver.RegisterMessageFactory<T>(messageTypeId, factory);
        }

        public void QueueMessage(BaseMessage message)
        {
            sender.QueueMessage(message);
        }

        public Task SendQueuedMessages()
        {
            long utcTimestamp = (long) (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
            return sender.SendQueuedMessages(utcTimestamp, receiver.CreateNextHeader());
        }

        public async Task<List<BaseMessage>> GetReceivedMessages()
        {
            var acks = await receiver.ReceiveAllPackets();
            sender.AckMessages(acks);
            return receiver.GetReceivedMessages();
        }
    }
}