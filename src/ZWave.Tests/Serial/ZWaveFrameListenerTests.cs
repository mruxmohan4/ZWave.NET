using System.Diagnostics;
using System.IO.Pipelines;

using ZWave.Serial;

namespace ZWave.Tests.Serial;

[TestClass]
public class ZWaveFrameListenerTests
{
    // When debugging, don't let it time out. THe timeout is really only so the test doesn't hang on failure
    private static int MillisecondsTimeout => Debugger.IsAttached
        ? int.MaxValue
        : 5000;

    [TestMethod]
    public void ConstructorValidation()
    {
        using var stream = new MemoryStream();
        Action<Frame> frameHandler = _ => { };

        Assert.ThrowsException<ArgumentNullException>(() => new ZWaveFrameListener(stream: null!, frameHandler));
        Assert.ThrowsException<ArgumentNullException>(() => new ZWaveFrameListener(stream, frameHandler: null!));
    }

    [TestMethod]
    public async Task SingleFrame()
    {
        var pipe = new Pipe();

        var framesRead = new List<Frame>();
        var frameReadEvent = new AutoResetEvent(false);
        Action<Frame> frameHandler = frame =>
        {
            framesRead.Add(frame);
            frameReadEvent.Set();
        };

        using var listener = new ZWaveFrameListener(pipe.Reader.AsStream(), frameHandler);

        await pipe.Writer.WriteAsync(new[] { FrameHeader.ACK });
        frameReadEvent.WaitOne(MillisecondsTimeout);

        await pipe.Writer.CompleteAsync();

        Assert.AreEqual(1, framesRead.Count);
        Assert.AreEqual(Frame.ACK, framesRead[0]);
    }

    [TestMethod]
    public async Task MultipleFramesAtOnce()
    {
        var pipe = new Pipe();

        var framesRead = new List<Frame>();
        var frameReadEvent = new AutoResetEvent(false);
        Action<Frame> frameHandler = frame =>
        {
            framesRead.Add(frame);
            frameReadEvent.Set();
        };

        using var listener = new ZWaveFrameListener(pipe.Reader.AsStream(), frameHandler);

        await pipe.Writer.WriteAsync(new[] { FrameHeader.ACK, FrameHeader.NAK, FrameHeader.CAN });
        frameReadEvent.WaitOne(MillisecondsTimeout);

        await pipe.Writer.CompleteAsync();

        Assert.AreEqual(3, framesRead.Count);
        Assert.AreEqual(Frame.ACK, framesRead[0]);
        Assert.AreEqual(Frame.NAK, framesRead[1]);
        Assert.AreEqual(Frame.CAN, framesRead[2]);
    }

    [TestMethod]
    public async Task MultipleFramesSequence()
    {
        var pipe = new Pipe();

        var framesRead = new List<Frame>();
        var frameReadEvent = new AutoResetEvent(false);
        Action<Frame> frameHandler = frame =>
        {
            framesRead.Add(frame);
            frameReadEvent.Set();
        };

        using var listener = new ZWaveFrameListener(pipe.Reader.AsStream(), frameHandler);

        await pipe.Writer.WriteAsync(new[] { FrameHeader.ACK });
        Assert.IsTrue(frameReadEvent.WaitOne(MillisecondsTimeout));

        await pipe.Writer.WriteAsync(new[] { FrameHeader.NAK });
        Assert.IsTrue(frameReadEvent.WaitOne(MillisecondsTimeout));

        await pipe.Writer.WriteAsync(new[] { FrameHeader.CAN });
        Assert.IsTrue(frameReadEvent.WaitOne(MillisecondsTimeout));

        await pipe.Writer.CompleteAsync();

        Assert.AreEqual(3, framesRead.Count);
        Assert.AreEqual(Frame.ACK, framesRead[0]);
        Assert.AreEqual(Frame.NAK, framesRead[1]);
        Assert.AreEqual(Frame.CAN, framesRead[2]);
    }

    [TestMethod]
    public async Task Dispose()
    {
        var pipe = new Pipe();
        Action<Frame> frameHandler = frame => { };

        var listener = new ZWaveFrameListener(pipe.Reader.AsStream(), frameHandler);

        bool keepWriting = true;
        var writeTask = Task.Run(async () =>
        {
            while (keepWriting)
            {
                await pipe.Writer.WriteAsync(new[] { FrameHeader.ACK });
            }
        });

        try
        {
            // Dispose the listener while the pipe (and thus stream) is still being written to
            listener.Dispose();
        }
        finally
        {
            // Clean up our write task
            keepWriting = false;
            await writeTask;
        }
    }
}
