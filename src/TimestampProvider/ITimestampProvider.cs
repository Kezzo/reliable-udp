namespace ReliableUdp.Timestamp
{
    public interface ITimestampProvider
    {
        long GetCurrentTimestamp();
    }
}