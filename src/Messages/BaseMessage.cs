using System;
using System.IO;

namespace ReliableUDP.Messages
{
    public abstract class BaseMessage
    {
        public ushort MessageTypeId;
        public ushort MessageUid;
        public bool IsAcked;
        public Nullable<long> LastSentTimestamp;

        public virtual void Serialize(BinaryWriter writer) 
        {
            writer.Write(MessageTypeId);
            writer.Write(MessageUid);
        }

        public virtual void Deserialize(BinaryReader reader)
        {
            MessageUid = reader.ReadUInt16();
        }
    }
}