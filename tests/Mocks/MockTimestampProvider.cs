using ReliableUdp.Timestamp;

namespace ReliableUdp.Tests.Mocks
{
    public class MockTimestampProvider : ITimestampProvider
    {
        private long currentTimestamp;

        public void SetTimestamp(long timestamp)
        {
            currentTimestamp = timestamp;
        }

        public long GetCurrentTimestamp()
        {
            return currentTimestamp;
        }
    }
}