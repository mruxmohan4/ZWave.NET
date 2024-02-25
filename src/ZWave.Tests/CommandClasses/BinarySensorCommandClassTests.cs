using ZWave.Tests.Mocks;
using ZWave.CommandClasses;
using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.Tests.CommandClasses;

[TestClass]
public class BinarySensorCommandClassTests
{
    [TestMethod]
    public async Task TestGetAndReportAsync()
    {
        MockDriver driver = new(NullLogger.Instance);
        var mockNode = new Node(default, driver, NullLogger.Instance);
        CommandClassInfo commandClassInfo = new(CommandClassId.BinarySensor, true, true);
        BinarySensorCommandClass binarySensorCommandClass = new(commandClassInfo, driver, mockNode);
        ReadOnlyMemory<byte> responseData = new byte[] { (byte)CommandClassId.BinarySensor, (byte)BinarySensorCommand.Report, 0xFF };

        // Send and process get command
        var getValueTask = binarySensorCommandClass.GetAsync(BinarySensorType.FirstSupported, CancellationToken.None).ConfigureAwait(false);
        MockDriver.MockResponse(binarySensorCommandClass, (byte)BinarySensorCommand.Get, responseData);
        bool sensorValue = await getValueTask;

        // Verify the report command's parsing
        Assert.IsTrue(sensorValue);

        // Verify Get command's binary format
        Assert.AreEqual(1, driver.SentCommands.Count);
        ReadOnlyMemory<byte> expected = new byte[] { (byte)CommandClassId.BinarySensor, (byte)BinarySensorCommand.Get };
        CollectionAssert.AreEqual(expected.ToArray(), driver.SentCommands[0].ToArray());
    }

    [TestMethod]
    public async Task TestSupportedGetAndReportAsync()
    {
        MockDriver driver = new(NullLogger.Instance);
        var mockNode = new Node(default, driver, NullLogger.Instance);
        CommandClassInfo commandClassInfo = new(CommandClassId.BinarySensor, true, true);
        BinarySensorCommandClass binarySensorCommandClass = new(commandClassInfo, driver, mockNode);
        binarySensorCommandClass.SetVersion(2);
        ReadOnlyMemory<byte> responseData = new byte[] { (byte)CommandClassId.BinarySensor, (byte)BinarySensorCommand.SupportedReport, 0x06 };

        // Send and process get command
        var getValueTask = binarySensorCommandClass.GetAsync(BinarySensorType.Water, CancellationToken.None).ConfigureAwait(false);
        MockDriver.MockResponse(binarySensorCommandClass, (byte)BinarySensorCommand.SupportedGet, responseData);
        bool sensorValue = await getValueTask;

        // Verify the report command's parsing
        Assert.IsTrue(sensorValue);

        // Verify Get command's binary format
        Assert.AreEqual(1, driver.SentCommands.Count);
        ReadOnlyMemory<byte> expected = new byte[] { (byte)CommandClassId.BinarySensor, (byte)BinarySensorCommand.Get };
        CollectionAssert.AreEqual(expected.ToArray(), driver.SentCommands[0].ToArray());
    }
}