using System;

namespace ReliableUdp.SequenceBuffer
{
    public class SequenceBuffer<T> where T : class
    {
        private readonly int bufferSize;
        
        private T[] entryDatas;
        private UInt32[] sequenceBuffer;
        public ushort MostRecentSequence { get; private set; }

        public SequenceBuffer(int bufferSize = 1024)
        {
            this.bufferSize = bufferSize;
            entryDatas = new T[this.bufferSize];
            sequenceBuffer = new UInt32[this.bufferSize];
        }

        public T GetEntry(ushort sequence)
        {
            int index = GetBufferIndex(sequence);
            return sequenceBuffer[index] == sequence ? entryDatas[index] : null;
        }

        public void AddEntry(ushort sequence, T entryData)
        {
            //TODO: check for too far away sequences?
            int index = GetBufferIndex(sequence);
            sequenceBuffer[index] = sequence;
            entryDatas[index] = entryData;

            bool wrappedAround = MostRecentSequence > (ushort.MaxValue - bufferSize) && sequence < bufferSize;
            bool isPreviousWrap = MostRecentSequence < bufferSize && sequence > (ushort.MaxValue - bufferSize);

            if(wrappedAround || (!isPreviousWrap && sequence > MostRecentSequence))
            {
                MostRecentSequence = sequence;
            }
        }

        public void RemoveEntry(ushort sequence)
        {
            int index = GetBufferIndex(sequence);
            sequenceBuffer[index] = UInt32.MaxValue;
        }

        private int GetBufferIndex(ushort sequence)
        {
            return sequence % this.bufferSize;
        }
    }
}