using System;

namespace ReliableUdp.Timestamp
{
    public class TimestampProvider : ITimestampProvider
    {
        public long GetCurrentTimestamp()
        {
            return (long) (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
        }
    }
}