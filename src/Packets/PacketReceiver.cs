using System;
using System.Threading.Tasks;
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

        public async Task<Packet> ReceiveNextPacket()
        {
            if(udpClient.Available <= 0) 
            {
                return null;
            }

            var buffer = await udpClient.ReceiveAsync();
            Packet packet = new Packet(buffer);

            receivedSequences.AddEntry(packet.Header.Sequence, new Tuple<bool>(true));
            return packet;
        }

        public PacketHeader CreateNextHeader()
        {
            var header = new PacketHeader{
                LastAck = receivedSequences.MostRecentSequence,
                AckBits = 0
            };

            UInt32 one = 1;
            for (ushort i = 0; i < 32; i++)
            {
                var nextEntry = receivedSequences.GetEntry((ushort) (receivedSequences.MostRecentSequence - (i + 1)));
                
                if(nextEntry != null && nextEntry.Item1 == true)
                {
                    header.AckBits = header.AckBits | (one << i);
                }
            }

            return header;
        }
    }
}