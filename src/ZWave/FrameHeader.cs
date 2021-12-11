namespace ZWave;

internal static class FrameHeader
{
    /// <summary>
    /// The SOF (Start of Frame) denotes the start of a data frame, which contains the Serial API
    /// command including parameters for the command in question.
    /// </summary>
    public const byte SOF = 0x01;

    /// <summary>
    /// The ACK frame indicates that the receiving end received a valid Data frame.
    /// </summary>
    public const byte ACK = 0x06;

    /// <summary>
    /// The NAK frame indicates that the receiving end received a Data frame with errors.
    /// </summary>
    public const byte NAK = 0x15;

    /// <summary>
    /// The CAN frame indicates that the receiving end discarded an otherwise valid Data frame.
    /// The CAN frame is used to resolve race conditions, where both ends send a Data frame and
    /// subsequently expects an ACK frame from the other end.
    /// </summary>
    public const byte CAN = 0x18;
}
