using Xunit;

namespace ReliableUDP.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        var test = new ReliableUDP.Class1();
        test.Test();
    }
}