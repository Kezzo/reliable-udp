using System.IO;
using ReliableUDP.Messages;

namespace ReliableUDP.Tests.Messages;

public class TestMessage : BaseMessage
{
    public int ID;
    public byte[]? Payload;

    public override void Deserialize(BinaryReader reader)
    {
        base.Deserialize(reader);
        ID = reader.ReadInt32();
        var payloadSize = reader.ReadInt32();
        Payload = reader.ReadBytes(payloadSize);
    }

    public override void Serialize(BinaryWriter writer)
    {
        base.Serialize(writer);
        writer.Write(ID);

        if(Payload == null)
        {
            writer.Write(0);
            return;
        }
        
        writer.Write(Payload.Length);
        writer.Write(Payload);
    }
}