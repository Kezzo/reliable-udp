using System;
using System.Collections.Generic;
using System.IO;
using ReliableUdp.SequenceBuffer;

namespace ReliableUdp.Packets
{
    public class PacketHeader : IEquatable<PacketHeader>
    {
        public ushort Sequence;
        public ushort LastAck;
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

        public static PacketHeader Create(SequenceBuffer<Tuple<bool>> receivedSequences)
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

        public List<ushort> GetAcks()
        {
            var acks = new List<ushort>(32);
            acks.Add(LastAck);

            for (ushort i = 0; i < 32; i++)
            {
                var shift = 1 << i;
                if((AckBits & (shift)) != 0 )
                {
                    acks.Add((ushort) (LastAck - (i + 1)));
                }
            }

            return acks;
        }

        public bool Equals(PacketHeader other)
        {
            return other != null && 
                Sequence == other.Sequence && 
                LastAck == other.LastAck &&
                AckBits == other.AckBits;
        }
    }
}