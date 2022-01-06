using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReliableUdp.MessageFactory;
using ReliableUdp.Messages;
using ReliableUdp.Timestamp;

namespace ReliableUdp
{
    public class ReliableUdpClient
    {
        private readonly MessageSender sender;
        private readonly MessageReceiver receiver;
        private readonly ITimestampProvider timestampProvider;

        public ReliableUdpClient(IUdpClient udpClient, ITimestampProvider timestampProvider = null)
        {
            sender = new MessageSender(udpClient);
            receiver = new MessageReceiver(udpClient);

            this.timestampProvider = timestampProvider;
            if(timestampProvider == null)
            {
                this.timestampProvider = new TimestampProvider();
            }
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
            return sender.SendQueuedMessages(timestampProvider.GetCurrentTimestamp(), receiver.CreateNextHeader());
        }

        public async Task<List<BaseMessage>> GetReceivedMessages()
        {
            while(true)
            {
                // get packets and ack them until all have been handled
                var acks = await receiver.ReceiveNextPacket();
                if(acks == null)
                {
                    break;
                }

                sender.AckMessages(acks);
            }
            
            return receiver.GetReceivedMessages();
        }
    }
}