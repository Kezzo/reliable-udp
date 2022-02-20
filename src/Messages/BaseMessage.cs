using System;
using System.IO;

namespace ReliableUdp.Messages
{
    public abstract class BaseMessage
    {
        public bool IsReliable;
        public ushort MessageTypeId;
        public ushort MessageUid;
        public bool IsAcked;
        public Nullable<long> LastSentTimestamp;

        public virtual void Serialize(BinaryWriter writer) 
        {
            writer.Write(MessageTypeId);
            writer.Write(IsReliable);
            writer.Write(MessageUid);
        }

        public virtual void Deserialize(BinaryReader reader)
        {
            IsReliable = reader.ReadBoolean();
            MessageUid = reader.ReadUInt16();
        }
    }
}