using ZWave.Serial;

namespace ZWave.Tests.Serial;

[TestClass]
public class DataFrameTests
{
    [TestMethod]
    public void EmptyCommandParameters()
    {
        var frameData = new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            DataFrameType.RES,
            CommandId.SerialApiStarted,
            0xFC // Checksum
        };

        var dataFrame = new DataFrame(frameData);
        Assert.AreEqual(0x00, dataFrame.Type);
        Assert.AreEqual(0x00, dataFrame.CommandId);
        Assert.IsTrue(dataFrame.CommandParameters.IsEmpty);
        Assert.IsTrue(dataFrame.IsChecksumValid);
    }

    [TestMethod]
    public void InvalidChecksum()
    {
        var frameData = new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            DataFrameType.RES,
            CommandId.SerialApiStarted,
            0x00 // Checksum
        };

        var dataFrame = new DataFrame(frameData);
        Assert.AreEqual(0x00, dataFrame.Type);
        Assert.AreEqual(0x00, dataFrame.CommandId);
        Assert.IsTrue(dataFrame.CommandParameters.IsEmpty);
        Assert.IsFalse(dataFrame.IsChecksumValid);
    }

    [TestMethod]
    public void CommandParameters()
    {
        var frameData = new byte[]
        {
            FrameHeader.SOF,
            3, // Length
            DataFrameType.RES,
            CommandId.SerialApiStarted,
            0x01, // Command parameter 1
            0x02, // Command parameter 2
            0x03, // Command parameter 3
            0xFC // Checksum
        };

        var dataFrame = new DataFrame(frameData);
        Assert.AreEqual(0x00, dataFrame.Type);
        Assert.AreEqual(0x00, dataFrame.CommandId);
        Assert.AreEqual(3, dataFrame.CommandParameters.Length);
        Assert.IsTrue(dataFrame.CommandParameters.Span.SequenceEqual(new byte[] { 0x01, 0x02, 0x03 }));
        Assert.IsTrue(dataFrame.IsChecksumValid);
    }
}
