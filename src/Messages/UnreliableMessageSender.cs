using System.Collections.Generic;

namespace ReliableUdp.Messages
{
    public class UnreliableMessageSender
    {
        private ushort nextMessageUidToSend = 0;

        private List<BaseMessage> messagesToSend = new List<BaseMessage>();

        public void AddMessagesToPayload(PayloadPacker packer, long timestampNow, out List<ushort> messageIds)
        {
            var addedMessageIds = new List<ushort>();

            for (int i = 0; i < messagesToSend.Count; i++)
            {
                var message = messagesToSend[i];
                
                if(!packer.TryPackMessage(message))
                {
                    continue;
                }

                addedMessageIds.Add(message.MessageUid);
            }

            messagesToSend.RemoveAll(m => addedMessageIds.Contains(m.MessageUid));
            messageIds = addedMessageIds;
        }

        public void OnMessageQueued(BaseMessage message)
        {
            message.IsReliable = false;
            message.MessageUid = nextMessageUidToSend;
            nextMessageUidToSend++;

            messagesToSend.Add(message);
        }
    }
}