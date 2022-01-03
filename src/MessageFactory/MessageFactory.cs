using System.IO;
using ReliableUDP.Messages;

namespace ReliableUDP.MessageFactory
{
    public interface IMessageFactory
    {
        BaseMessage CreateMessage(BinaryReader reader);
    }

    public class MessageFactory<T> : IMessageFactory where T : BaseMessage, new()
    {
        public BaseMessage CreateMessage(BinaryReader reader)
        {
            var message = new T();
            message.Deserialize(reader);

            return message;
        }
    }
}