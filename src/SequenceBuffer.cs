namespace ReliableUDP;

public class SequenceBuffer<T> where T : class
{
    private readonly int bufferSize;
    
    private T[] entryDatas;
    private UInt32[] sequenceBuffer;
    public UInt16 MostRecentSequence { get; private set; }

    public SequenceBuffer(int bufferSize = 1024)
    {
        this.bufferSize = bufferSize;
        entryDatas = new T[this.bufferSize];
        sequenceBuffer = new UInt32[this.bufferSize];
    }

    public T? GetEntry(UInt16 sequence)
    {
        int index = GetBufferIndex(sequence);
        return sequenceBuffer[index] == sequence ? entryDatas[index] : null;
    }

    public void AddEntry(UInt16 sequence, T entryData)
    {
        //TODO: check for too far away sequences?
        int index = GetBufferIndex(sequence);
        sequenceBuffer[index] = sequence;
        entryDatas[index] = entryData;

        bool wrappedAround = MostRecentSequence > (UInt16.MaxValue - bufferSize) && sequence < bufferSize;
        bool isPreviousWrap = MostRecentSequence < bufferSize && sequence > (UInt16.MaxValue - bufferSize);

        if(wrappedAround || (!isPreviousWrap && sequence > MostRecentSequence))
        {
            MostRecentSequence = sequence;
        }
    }

    private int GetBufferIndex(UInt16 sequence)
    {
        return sequence % this.bufferSize;
    }
}