using System;
using System.Collections.Generic;
using ReliableUdp.Packets;

namespace ReliableUdp.Messages
{
    public class MessageSender
    {
        private readonly ReliableMessageSender reliableSender;
        private readonly UnreliableMessageSender unreliableSender;
        private readonly PacketSender packetSender;
        protected Dictionary<Type, ushort> messageTypeIds;
        
        public MessageSender(IUdpClient udpClient)
        {
            reliableSender = new ReliableMessageSender();
            unreliableSender = new UnreliableMessageSender();
            packetSender = new PacketSender(udpClient);

            messageTypeIds = new Dictionary<Type, ushort>();
        }

        public void AckMessages(List<ushort> acks)
        {
            reliableSender.AckMessages(acks);
        }

        public void QueueMessage(BaseMessage message, bool sendReliable)
        {
            if(message == null)
            {
                return;
            }

            if(!messageTypeIds.TryGetValue(message.GetType(), out ushort messageTypeId))
            {
                throw new InvalidOperationException("Message type has no registered message type id");
            }

            // Set all BaseMessage fields to correct and intial values
            message.MessageTypeId = messageTypeId;
            message.IsAcked = false;
            message.LastSentTimestamp = null;

            if(sendReliable)
            {
                reliableSender.OnMessageQueued(message);
            }
            else
            {
                unreliableSender.OnMessageQueued(message);
            }
        }

        public void SendQueuedMessages(long timestampNow, PacketHeader headerToUse)
        {
            while(true)
            {
                var payloadPacker = new PayloadPacker();

                // send packets until message queue has no message available for sending
                reliableSender.AddMessagesToPayload(payloadPacker, timestampNow, out List<ushort> reliableMessageIds);
                unreliableSender.AddMessagesToPayload(payloadPacker, timestampNow, out List<ushort> unreliableMessageIds);

                if(payloadPacker.IsEmpty)
                {
                    return;
                }

                var packetSequence = packetSender.SendPacket(headerToUse, payloadPacker.GetBytes());
                reliableSender.OnPacketSent(packetSequence, reliableMessageIds);
            }
        }

        public void OnPacketSent(ushort packetSequence, List<ushort> messageIds) 
        { 
            reliableSender.OnPacketSent(packetSequence, messageIds);
        }

        public void RegisterMessageTypeId(Type type, ushort messageTypeId)
        {
            if(messageTypeIds.ContainsKey(type))
            {
                throw new InvalidOperationException("Message type with id was already registered.");
            }

            messageTypeIds.Add(type, messageTypeId);
        }
    }
}