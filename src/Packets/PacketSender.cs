namespace ReliableUDP.Packets;

public class PacketSender
{
    private readonly IUdpClient udpClient;
    private readonly SequenceBuffer<PacketSendData> sentBuffer;
    private UInt16 nextSequence;

    public PacketSender(IUdpClient udpClient)
    {
        this.udpClient = udpClient;
        this.sentBuffer = new SequenceBuffer<PacketSendData>();
    }

    public void OnPacketAcked(UInt16 sequence)
    {
        var packetData = sentBuffer.GetEntry(sequence);
        if(packetData != null)
        {
            packetData.IsAcked = true;
        }
    }

    public bool IsPacketAcked(UInt16 sequence)
    {
        var packetData = sentBuffer.GetEntry(sequence);
        return packetData != null && packetData.IsAcked;
    }

    public async Task<UInt16> SendPacket(PacketHeader header, byte[] payload)
    {
        header.Sequence = nextSequence;
        this.sentBuffer.AddEntry(nextSequence, new PacketSendData{
            IsAcked = false,
            SendTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
        });

        nextSequence++;

        var bytesToSend = header.AddBytes(payload);
        await udpClient.SendAsync(bytesToSend, bytesToSend.Length);

        return header.Sequence;
    }
}