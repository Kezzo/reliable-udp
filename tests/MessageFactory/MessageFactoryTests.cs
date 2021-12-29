using System.IO;
using ReliableUDP.MessageFactory;
using ReliableUDP.Messages;
using ReliableUDP.Tests.Mocks;
using Xunit;

namespace ReliableUDP.Tests;

public class MessageHubTests
{
    public class TestMessage : BaseMessage
    {
        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
        }
    }

    [Fact]
    public void TestRegisterMessageFactory()
    {
        var hub = new MessageHub(new MockUDPClient(null));
        
        var factory = new MessageFactory<TestMessage>();
        hub.RegisterMessageFactory<TestMessage>(0, factory);
    }
}