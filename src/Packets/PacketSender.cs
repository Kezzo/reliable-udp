namespace ReliableUDP.Packets;

public class PacketSender
{
    private readonly IUdpClient udpClient;
    private UInt16 nextSequence;

    public PacketSender(IUdpClient udpClient)
    {
        this.udpClient = udpClient;
    }

    public async Task<UInt16> SendPacket(PacketHeader header, byte[] payload)
    {
        header.Sequence = nextSequence;
        nextSequence++;

        var bytesToSend = header.AddBytes(payload);
        await udpClient.SendAsync(bytesToSend, bytesToSend.Length);

        return header.Sequence;
    }
}