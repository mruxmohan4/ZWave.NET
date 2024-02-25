using ZWave.Tests.Mocks;
using ZWave.CommandClasses;
using Microsoft.Extensions.Logging.Abstractions;

namespace ZWave.Tests.CommandClasses;

[TestClass]
public class BasicCommandClassTests
{
    [TestMethod]
    public async Task TestGetAndReportAsync()
    {
        MockDriver driver = new(NullLogger.Instance);
        var mockNode = new Node(default, driver, NullLogger.Instance);
        CommandClassInfo commandClassInfo = new(CommandClassId.Basic, true, true);
        BasicCommandClass basicCommandClass = new(commandClassInfo, driver, mockNode);
        ReadOnlyMemory<byte> responseData = new byte[] { (byte)CommandClassId.Basic, (byte)BasicCommand.Report, 0xFF }; // ON

        // Send and process get command
        var getStateTask = basicCommandClass.GetAsync(CancellationToken.None).ConfigureAwait(false);
        MockDriver.MockResponse(basicCommandClass, (byte)BasicCommand.Get, responseData);
        BasicState state = await getStateTask;

        // Verify the report command's parsing
        Assert.IsNotNull(state);
        Assert.AreEqual(0xFF, state.CurrentValue.Value);
        Assert.IsNull(state.TargetValue);
        Assert.IsNull(state.Duration);

        // Verify Get command's binary format
        Assert.AreEqual(1, driver.SentCommands.Count);
        ReadOnlyMemory<byte> expected = new byte[] { (byte)CommandClassId.Basic, (byte)BasicCommand.Get };
        CollectionAssert.AreEqual(expected.ToArray(), driver.SentCommands[0].ToArray());
    }

    [TestMethod]
    public async Task TestSetAsync()
    {
        MockDriver driver = new(NullLogger.Instance);
        var mockNode = new Node(default, driver, NullLogger.Instance);
        CommandClassInfo commandClassInfo = new(CommandClassId.Basic, true, true);
        BasicCommandClass basicCommandClass = new(commandClassInfo, driver, mockNode);
        await basicCommandClass.SetAsync(0x63, CancellationToken.None).ConfigureAwait(false); // 99% ON

        // Verify Set command's binary format
        Assert.AreEqual(1, driver.SentCommands.Count);
        ReadOnlyMemory<byte> expected = new byte[] { (byte)CommandClassId.Basic, (byte)BasicCommand.Set, 0x63 };
        CollectionAssert.AreEqual(expected.ToArray(), driver.SentCommands[0].ToArray());
    }
}