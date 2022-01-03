using System.IO;
using ReliableUDP.Messages;

namespace ReliableUDP.Tests.Messages;

public class TestMessage : BaseMessage
{
    public int ID;

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);
        ID = reader.ReadInt32();
    }

    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);
        writer.Write(ID);
    }
}