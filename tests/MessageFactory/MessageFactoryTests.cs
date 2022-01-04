using ReliableUdp.MessageFactory;
using ReliableUdp.Tests.Messages;
using ReliableUdp.Tests.Mocks;
using Xunit;

namespace ReliableUdp.Tests.MessageFactory
{
    public class MessageHubTests
    {
        [Fact]
        public void TestRegisterMessageFactory()
        {
            var hub = new ReliableUdpHub(new MockUdpClient(null));
            
            var factory = new MessageFactory<Messages.TestMessage>();
            hub.RegisterMessageFactory<Messages.TestMessage>(0, factory);
        }
    }
}