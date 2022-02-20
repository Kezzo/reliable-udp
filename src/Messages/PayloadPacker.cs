using System.IO;

namespace ReliableUdp.Messages
{
    public class PayloadPacker
    {
        // Keep packet at this max or below to avoid ip packet fragmentation
        // and allow each packet to be transmitted in one ethernet frame
        private const int MAX_PACKET_PAYLOAD_SIZE = 508;

        //TODO: Re-use these
        private readonly MemoryStream ms;
        private readonly BinaryWriter writer;

        public bool IsFull { get; private set; }
        public bool IsEmpty { get; private set; }
        
        public PayloadPacker()
        {
            ms = new MemoryStream(MAX_PACKET_PAYLOAD_SIZE);
            writer = new BinaryWriter(ms);
            IsEmpty = true;
        }

        public bool TryPackMessage(BaseMessage message)
        {
            var preWritePosition = ms.Position;
            var preWriteLength = ms.Length;

            message.Serialize(writer);

            // allow sending huge payloads in separate packets
            if(IsEmpty && ms.Position > MAX_PACKET_PAYLOAD_SIZE)
            {
                // stop packing messages after this message
                IsFull = true;
            }
            // does buffer fit into payload?
            else if(ms.Position > MAX_PACKET_PAYLOAD_SIZE)
            {
                // Reset position back to last flushed position
                ms.Position = preWritePosition;
                ms.SetLength(preWriteLength);
                return false;
            }

            IsEmpty = false;
            return true;
        }

        public byte[] GetBytes()
        {
            ms.Dispose();
            writer.Dispose();
            
            return ms.ToArray();
        }
    }
}