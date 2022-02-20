using System.Collections.Generic;

namespace ReliableUdp.Messages
{
    public class UnorderedMessageReceiver : BaseMessageReceiver
    {
        private List<BaseMessage> receivedMessages;

        public UnorderedMessageReceiver(IUdpClient udpClient) : base(udpClient)
        {
            receivedMessages = new List<BaseMessage>();
        }

        public override List<BaseMessage> GetReceivedMessages()
        {
            var messageToReturn = new List<BaseMessage>(receivedMessages);
            receivedMessages.Clear();
            return messageToReturn;
        }

        protected override void OnMessageReceived(BaseMessage message)
        {
            receivedMessages.Add(message);
        }
    }
}