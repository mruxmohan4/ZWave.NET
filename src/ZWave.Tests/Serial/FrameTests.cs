using System.Runtime.InteropServices;

namespace ZWave.Serial.Tests;

[TestClass]
public class FrameTests
{
    private static readonly ReadOnlyMemory<byte> ValidDataFrameData = new byte[]
    {
        FrameHeader.SOF,
        3,                  // Length
        DataFrameType.RES,
        0x00,               // Command id. TODO: Use const
        0xFC                // Checksum
    };

    [TestMethod]
    public void ConstructorEmptyData()
    {
        Assert.ThrowsException<ArgumentException>(() => new Frame(new byte[0]));
    }

    [DataTestMethod]
    [DataRow(FrameHeader.ACK, FrameType.ACK)]
    [DataRow(FrameHeader.NAK, FrameType.NAK)]
    [DataRow(FrameHeader.CAN, FrameType.CAN)]
    public void ConstructorSingleByteFrames(byte frameHeader, FrameType expectedFrameType)
    {
        var frame = new Frame(new[] { frameHeader });
        Assert.AreEqual(expectedFrameType, frame.Type);
    }

    [DataTestMethod]
    [DataRow(FrameHeader.ACK)]
    [DataRow(FrameHeader.NAK)]
    [DataRow(FrameHeader.CAN)]
    public void ConstructorSingleByteFramesWithExtraData(byte frameHeader)
    {
        Assert.ThrowsException<ArgumentException>(() => new Frame(new byte[] { frameHeader, 0x01 }));
    }

    // Single byte frames use a singleton array to avoid holding onto many small arrays
    [DataTestMethod]
    [DataRow(true, new[] { FrameHeader.ACK })]
    [DataRow(true, new[] { FrameHeader.NAK })]
    [DataRow(true, new[] { FrameHeader.CAN })]
    [DataRow(
        false,
        new byte[]
        {
            FrameHeader.SOF,
            3,                  // Length
            DataFrameType.RES,
            0x00,               // Command id. TODO: Use const
            0xFC                // Checksum
        })]
    public void ConstructorDataSingletons(bool expectSingleton, byte[] inputFrameData)
    {
        var frame = new Frame(inputFrameData);

        // Get the actual underlying array
        Assert.IsTrue(MemoryMarshal.TryGetArray(frame.Data, out ArraySegment<byte> arraySegment));
        byte[]? actualFrameData = arraySegment.Array;

        Assert.AreEqual(!expectSingleton, ReferenceEquals(inputFrameData, actualFrameData));
    }

    [TestMethod]
    public void ConstructorDataFrame()
    {
        var frame = new Frame(ValidDataFrameData);
        Assert.AreEqual(FrameType.Data, frame.Type);
    }

    [TestMethod]
    public void ConstructorDataFrameWithInvalidLength()
    {
        byte[] frameData = ValidDataFrameData.ToArray();
        frameData[1]--; // Length

        Assert.ThrowsException<ArgumentException>(() => new Frame(frameData));
    }

    [DataTestMethod]
    [DataRow(true, new[] { FrameHeader.ACK }, new[] { FrameHeader.ACK })]
    [DataRow(true, new[] { FrameHeader.NAK }, new[] { FrameHeader.NAK })]
    [DataRow(true, new[] { FrameHeader.CAN }, new[] { FrameHeader.CAN })]
    [DataRow(false, new[] { FrameHeader.ACK }, new[] { FrameHeader.NAK })]
    [DataRow(false, new[] { FrameHeader.ACK }, new[] { FrameHeader.CAN })]
    [DataRow(false, new[] { FrameHeader.NAK }, new[] { FrameHeader.CAN })]
    [DataRow(
        true,
        new byte[]
        {
            FrameHeader.SOF,
            3,                  // Length
            DataFrameType.RES,
            0x00,               // Command id. TODO: Use const
            0xFC                // Checksum
        },
        new byte[]
        {
            FrameHeader.SOF,
            3,                  // Length
            DataFrameType.RES,
            0x00,               // Command id. TODO: Use const
            0xFC                // Checksum
        })]
    [DataRow(
        false,
        new byte[]
        {
            FrameHeader.SOF,
            3,                  // Length
            DataFrameType.RES,
            0x00,               // Command id. TODO: Use const
            0xFC                // Checksum
        },
        new byte[]
        {
            FrameHeader.SOF,
            3,                  // Length
            DataFrameType.RES,
            0x01,               // Command id. TODO: Use const
            0xFC                // Checksum
        })]
    public void Equality(bool expectedAreEqual, byte[] frameData1, byte[] frameData2)
    {
        var frame1 = new Frame(frameData1);
        var frame2 = new Frame(frameData2);
        Assert.AreEqual(expectedAreEqual, frame1 == frame2);
    }
}