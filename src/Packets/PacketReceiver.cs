using System;
using ReliableUdp.SequenceBuffer;

namespace ReliableUdp.Packets
{
    public class PacketReceiver
    {
        private readonly IUdpClient udpClient;
        private readonly SequenceBuffer<Tuple<bool>> receivedSequences;

        public PacketReceiver(IUdpClient udpClient)
        {
            this.udpClient = udpClient;
            this.receivedSequences = new SequenceBuffer<Tuple<bool>>();
        }

        public Packet ReceiveNextPacket()
        {
            if(udpClient.Available <= 0) 
            {
                return null;
            }

            var buffer = udpClient.Receive();
            Packet packet = new Packet(buffer);

            receivedSequences.AddEntry(packet.Header.Sequence, new Tuple<bool>(true));
            return packet;
        }

        public PacketHeader CreateNextHeader()
        {
            return PacketHeader.Create(receivedSequences);
        }
    }
}