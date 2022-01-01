namespace ZWave.Serial.Commands;

internal enum RssiMeasurementType
{
    /// <summary>
    /// The RSSI measurement has a value
    /// </summary>
    Measured,

    /// <summary>
    /// The RSSI is below sensitivity and could not be measured.
    /// </summary>
    BelowSensitivity,

    /// <summary>
    /// The radio receiver is saturated and the RSSI could not be measured.
    /// </summary>
    RadioReceiverSaturated,

    /// <summary>
    /// The RSSI is not available.
    /// </summary>
    Unavailable,
}

internal struct RssiMeasurement
{
    private readonly sbyte _value;

    public RssiMeasurement(sbyte value)
    {
        _value = value;
    }

    public RssiMeasurementType MeasurementType
        => _value switch
        {
            125 => RssiMeasurementType.BelowSensitivity,
            126 => RssiMeasurementType.RadioReceiverSaturated,
            127 => RssiMeasurementType.Unavailable,
            _ => RssiMeasurementType.Measured,
        };

    public bool HasValue => MeasurementType == RssiMeasurementType.Measured;

    public sbyte? Value => HasValue ? _value : null;

    public static implicit operator RssiMeasurement(byte b) => new RssiMeasurement(b.ToInt8());
}
