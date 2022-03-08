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
            var client = new ReliableUdpClient(ReceivalMode.Ordered, new MockUdpClient(null));
            
            var factory = new MessageFactory<Messages.TestMessage>();
            client.RegisterMessageFactory<Messages.TestMessage>(0, factory);
        }
    }
}