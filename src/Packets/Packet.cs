using System;

namespace ReliableUdp.Packets
{
    public class Packet
    {
        public readonly PacketHeader Header;
        public readonly ArraySegment<byte> Payload;

        public Packet(byte[] bytes)
        {
            this.Header = new PacketHeader(bytes);
            this.Payload = new ArraySegment<byte>(bytes, PacketHeader.Length, bytes.Length - PacketHeader.Length);
        }
    }
}