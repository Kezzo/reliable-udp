using System;
using System.IO;

namespace ReliableUDP.Messages
{
    public abstract class BaseMessage
    {
        public ushort MessageId;
        public bool IsAcked;
        public Nullable<long> LastSentTimestamp;

        public virtual void Serialize(BinaryWriter writer) 
        {
            writer.Write(MessageId);
        }

        public virtual void Deserialize(BinaryReader reader)
        {
            MessageId = reader.ReadUInt16();
        }
    }
}