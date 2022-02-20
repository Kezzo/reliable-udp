using System.Collections.Generic;

namespace ReliableUdp.Messages
{
    public class OrderedMessageReceiver : BaseMessageReceiver
    {
        private readonly List<BaseMessage> unreliableMessagesToReturn;
        private ushort nextReliableMessageIdToReceive = 0;

        public OrderedMessageReceiver(IUdpClient udpClient) : base(udpClient)
        {
            unreliableMessagesToReturn = new List<BaseMessage>();
        }
        
        public override List<BaseMessage> GetReceivedMessages()
        {
            var messagesToReturn = new List<BaseMessage>();

            while(true)
            {
                var nextMessage = receivedReliableMessages.GetEntry(nextReliableMessageIdToReceive);

                if(nextMessage == null)
                {
                    break;
                }

                nextReliableMessageIdToReceive++;
                messagesToReturn.Add(nextMessage);
            }

            messagesToReturn.AddRange(unreliableMessagesToReturn);
            unreliableMessagesToReturn.Clear();
            
            return messagesToReturn;
        }

        protected override void OnMessageReceived(BaseMessage message)
        {
            if(!message.IsReliable)
            {
                // best effort ordered for unreliable messages
                InsertSorted(message);
            }
        }

        private void InsertSorted(BaseMessage message)
        {
            if(unreliableMessagesToReturn.Count == 0)
            {
                unreliableMessagesToReturn.Add(message);
                return;
            }

            for (int i = 0; i < unreliableMessagesToReturn.Count; i++)
            {
                if(unreliableMessagesToReturn[i].MessageUid > message.MessageUid)
                {
                    unreliableMessagesToReturn.Insert(i, message);
                    return;
                }

                if(i == unreliableMessagesToReturn.Count - 1)
                {
                    unreliableMessagesToReturn.Add(message);
                    return;
                }
            }
        }
    }
}