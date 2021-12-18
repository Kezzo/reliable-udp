namespace ReliableUDP.Packets;

public class PacketSender
{
    private readonly IUdpClient udpClient;
    private readonly SequenceBuffer<PacketSendData> sequenceBuffer;
    private UInt16 nextSequence;

    public PacketSender(IUdpClient udpClient)
    {
        this.udpClient = udpClient;
        this.sequenceBuffer = new SequenceBuffer<PacketSendData>();
    }

    public void OnPacketAcked(UInt16 sequence)
    {
        var packetData = sequenceBuffer.GetEntry(sequence);
        if(packetData != null)
        {
            packetData.IsAcked = true;
        }
    }

    public bool IsPacketAcked(UInt16 sequence)
    {
        var packetData = sequenceBuffer.GetEntry(sequence);
        return packetData != null && packetData.IsAcked;
    }

    public Task<int> SendPacket(PacketHeader header, byte[] payload)
    {
        header.Sequence = nextSequence;
        this.sequenceBuffer.AddEntry(nextSequence, new PacketSendData{
            IsAcked = false,
            SendTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
        });

        nextSequence++;

        var bytesToSend = header.AddBytes(payload);
        return udpClient.SendAsync(bytesToSend, bytesToSend.Length);
    }
}