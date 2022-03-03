using System;
using System.Collections.Generic;
using System.IO;
using ReliableUdp.MessageFactory;
using ReliableUdp.Packets;
using ReliableUdp.SequenceBuffer;

namespace ReliableUdp.Messages
{
    public abstract class BaseMessageReceiver
    {
        private readonly PacketReceiver packetReceiver;
        private readonly Dictionary<ushort, IMessageFactory> messageFactories;
        protected readonly SequenceBuffer<BaseMessage> receivedReliableMessages;
        protected readonly SequenceBuffer<BaseMessage> receivedUnreliableMessages;

        public BaseMessageReceiver(IUdpClient udpClient)
        {
            packetReceiver = new PacketReceiver(udpClient);
            messageFactories = new Dictionary<ushort, IMessageFactory>();
            receivedReliableMessages = new SequenceBuffer<BaseMessage>();
            receivedUnreliableMessages = new SequenceBuffer<BaseMessage>();
        }

        public abstract List<BaseMessage> GetReceivedMessages();
        protected abstract void OnMessageReceived(BaseMessage message);

        public void RegisterMessageFactory<T>(ushort messageTypeId, IMessageFactory factory)
        {
            if(messageFactories.ContainsKey(messageTypeId))
            {
                throw new InvalidOperationException("Message type with id was already registered.");
            }

            messageFactories.Add(messageTypeId, factory);
        }

        public List<ushort> ReceiveNextPacket()
        {
            var packet = packetReceiver.ReceiveNextPacket();
            if(packet == null)
            {
                return null;
            }

            ExtractMessagesFromPayload(packet.Payload);

            // DO NOT ack packet if there was an exception when deserializing it.
            // TODO: track deserialization error and reject client after continuous errors.
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
                //Console.WriteLine("WARN: ExtractMessagesFromPayload: payload is null");
                return;
            }

            using(var ms = new MemoryStream(payload.Array, payload.Offset, payload.Count))
            {
                while(ms.Position < ms.Length)
                {
                    using (BinaryReader reader = new BinaryReader(ms, System.Text.UTF8Encoding.UTF8, true))
                    {
                        ushort messageTypeId = reader.ReadUInt16();
                        if(!messageFactories.TryGetValue(messageTypeId, out IMessageFactory factory) || factory == null)
                        {
                            throw new KeyNotFoundException($"Couldn't find factory for message type id: {messageTypeId}");
                        }

                        var message = factory.CreateMessage(reader);
                        HandleReceivedMessage(message);
                    }
                }
            }
        }

        private void HandleReceivedMessage(BaseMessage message)
        {
            if(message.IsReliable && receivedReliableMessages.GetEntry(message.MessageUid) == null)
            {
                receivedReliableMessages.AddEntry(message.MessageUid, message);
                OnMessageReceived(message);
            }
            // filter out duplicates
            else if(receivedUnreliableMessages.GetEntry(message.MessageUid) == null)
            {
                receivedUnreliableMessages.AddEntry(message.MessageUid, message);
                OnMessageReceived(message);
            }
        }
    }
}