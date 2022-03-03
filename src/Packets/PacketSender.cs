namespace ReliableUdp.Packets
{
    public class PacketSender
    {
        private readonly IUdpClient udpClient;
        private ushort nextSequence;

        public PacketSender(IUdpClient udpClient)
        {
            this.udpClient = udpClient;
        }

        public ushort SendPacket(PacketHeader header, byte[] payload)
        {
            header.Sequence = nextSequence;
            nextSequence++;

            var bytesToSend = header.AddBytes(payload);
            udpClient.Send(bytesToSend);

            return header.Sequence;
        }
    }
}