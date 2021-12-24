namespace ReliableUDP;

public abstract class BaseMessage
{
    public UInt16 MessageId;
    public bool IsAcked;
    public long LastSentTimestamp;

    public virtual void Serialize(BinaryWriter writer) 
    {
        writer.Write(MessageId);
    }

    public virtual void Deserialize(BinaryReader reader)
    {
        MessageId = reader.ReadUInt16();
    }
}