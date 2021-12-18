namespace ReliableUDP.Packets;

public class PacketHeader : IEquatable<PacketHeader>
{
    public UInt16 Sequence;
    public UInt16 LastAck;
    public UInt32 AckBits;

    public const int Length = 8;

    public PacketHeader()
    {

    }

    public PacketHeader(byte[] bytes)
    {
        using(MemoryStream ms = new MemoryStream(bytes))
        {
            using (BinaryReader reader = new BinaryReader(ms, System.Text.UTF8Encoding.UTF8, true))
            {
                Sequence = reader.ReadUInt16();
                LastAck = reader.ReadUInt16();
                AckBits = reader.ReadUInt32();
            }
        }
    }

    public byte[] AddBytes(byte[] buffer)
    {
        byte[] bytesToReturn = new byte[buffer.Length + 8];

        using(MemoryStream ms = new MemoryStream(bytesToReturn))
        {
            using (BinaryWriter writer = new BinaryWriter(ms, System.Text.UTF8Encoding.UTF8, false))
            {
                writer.Write(Sequence);
                writer.Write(LastAck);
                writer.Write(AckBits);

                writer.Write(buffer);
            }
        }
        
        return bytesToReturn;
    }

    public List<UInt16> GetAcks()
    {
        var acks = new List<UInt16>(32);
        acks.Add(LastAck);

        for (UInt16 i = 0; i < 32; i++)
        {
            var shift = 1 << i;
            if((AckBits & (shift)) != 0 )
            {
                acks.Add((UInt16) (LastAck - (i + 1)));
            }
        }

        return acks;
    }

    public bool Equals(PacketHeader? other)
    {
        return other != null && 
            Sequence == other.Sequence && 
            LastAck == other.LastAck &&
            AckBits == other.AckBits;
    }
}