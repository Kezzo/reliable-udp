using System;
using ReliableUdp.SequenceBuffer;
using Xunit;

namespace ReliableUdp.Tests.SequenceBuffer
{
    public class SequenceBufferTests
    {
        [Fact]
        public void TestAddAndGet()
        {
            var buffer = new SequenceBuffer<Tuple<int>>(10);

            var testValue1 = new Tuple<int>(123);
            buffer.AddEntry(2, testValue1);

            var testValue2 = new Tuple<int>(456);
            buffer.AddEntry(4, testValue2);

            var testValue3 = new Tuple<int>(789);
            buffer.AddEntry(5, testValue3);

            Assert.NotNull(buffer.GetEntry(2));
            Assert.Equal(buffer.GetEntry(2), testValue1);

            Assert.NotNull(buffer.GetEntry(4));
            Assert.Equal(buffer.GetEntry(4), testValue2);

            Assert.NotNull(buffer.GetEntry(5));
            Assert.Equal(buffer.GetEntry(5), testValue3);

            // have not been set so should be null
            Assert.Null(buffer.GetEntry(1));
            Assert.Null(buffer.GetEntry(9));
            Assert.Null(buffer.GetEntry(10));

            // test ring buffer index 
            Assert.Null(buffer.GetEntry(11));
            Assert.Null(buffer.GetEntry(14));

            var testValue4 = new Tuple<int>(111);
            // test overwriting in ring buffer
            buffer.AddEntry(12, testValue4);

            Assert.NotNull(buffer.GetEntry(12));
            Assert.Equal(buffer.GetEntry(12), testValue4);
        }

        [Fact]
        public void TestMostRecentSequence()
        {
            var buffer = new SequenceBuffer<Tuple<int>>();

            var testValue = new Tuple<int>(123);

            buffer.AddEntry(2, testValue);
            Assert.Equal(2, buffer.MostRecentSequence);

            buffer.AddEntry(1, testValue);
            Assert.Equal(2, buffer.MostRecentSequence);

            buffer.AddEntry(4, testValue);
            Assert.Equal(4, buffer.MostRecentSequence);

            buffer.AddEntry(10000, testValue);
            Assert.Equal(10000, buffer.MostRecentSequence);

            buffer.AddEntry(ushort.MaxValue - 100, testValue);
            Assert.Equal(ushort.MaxValue - 100, buffer.MostRecentSequence);

            buffer.AddEntry(0, testValue);
            Assert.Equal(0, buffer.MostRecentSequence);

            buffer.AddEntry(3000, testValue);
            Assert.Equal(3000, buffer.MostRecentSequence);

            buffer.AddEntry(ushort.MaxValue, testValue);
            Assert.Equal(ushort.MaxValue, buffer.MostRecentSequence);

            buffer.AddEntry(10, testValue);
            Assert.Equal(10, buffer.MostRecentSequence);

            buffer.AddEntry(ushort.MaxValue - 50, testValue);
            Assert.Equal(10, buffer.MostRecentSequence);

            buffer.AddEntry(ushort.MaxValue, testValue);
            Assert.Equal(10, buffer.MostRecentSequence);

            buffer.AddEntry(11, testValue);
            Assert.Equal(11, buffer.MostRecentSequence);

            buffer.AddEntry(100, testValue);
            Assert.Equal(100, buffer.MostRecentSequence);
        }
    }
}