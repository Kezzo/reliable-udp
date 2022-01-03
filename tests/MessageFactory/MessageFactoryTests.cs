using ReliableUDP.MessageFactory;
using ReliableUDP.Tests.Messages;
using ReliableUDP.Tests.Mocks;
using Xunit;

namespace ReliableUDP.Tests.MessageFactory;

public class MessageHubTests
{
    [Fact]
    public void TestRegisterMessageFactory()
    {
        var hub = new ReliableUdpHub(new MockUdpClient(null));
        
        var factory = new MessageFactory<TestMessage>();
        hub.RegisterMessageFactory<TestMessage>(0, factory);
    }
}