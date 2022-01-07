using System.IO;
using ReliableUdp.Messages;

namespace ServerExample
{
    public class HelloMessage : BaseMessage
    {
        public string MessageText;

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(MessageText);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            MessageText = reader.ReadString();
        }
    }
}