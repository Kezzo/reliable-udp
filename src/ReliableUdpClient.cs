using System.Collections.Generic;
using ReliableUdp.MessageFactory;
using ReliableUdp.Messages;
using ReliableUdp.Timestamp;

namespace ReliableUdp
{
    public class ReliableUdpClient
    {
        
        private readonly BaseMessageReceiver receiver;
        private readonly MessageSender sender;
        private readonly ITimestampProvider timestampProvider;

        public ReliableUdpClient(ReceivalMode receivalMode, IUdpClient udpClient, ITimestampProvider timestampProvider = null)
        {
            sender = new MessageSender(udpClient);

            switch(receivalMode)
            {
                case ReceivalMode.Ordered:
                    receiver = new OrderedMessageReceiver(udpClient);
                    break;

                case ReceivalMode.Unordered:
                    receiver = new UnorderedMessageReceiver(udpClient);
                    break;
            }

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

        public void QueueMessage(BaseMessage message, bool sendReliable = true)
        {
            sender.QueueMessage(message, sendReliable);
        }

        public void SendQueuedMessages()
        {
            sender.SendQueuedMessages(timestampProvider.GetCurrentTimestamp(), receiver.CreateNextHeader());
        }

        public List<BaseMessage> GetReceivedMessages()
        {
            while(true)
            {
                // get packets and ack them until all have been handled
                var acks = receiver.ReceiveNextPacket();
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