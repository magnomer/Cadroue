using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CSEncoderQuality
{
    private readonly LSEncoderPort _csEncoderQualityPort;
    private readonly string _csEncoderQualityRate;
    private readonly string _csEncoderQualityDefault;

    internal CSEncoderQuality(LCapabilityQuality codec, LSEncoderPort draft, string rate)
    {
        _csEncoderQualityPort = draft;
        _csEncoderQualityRate = rate;
        _csEncoderQualityDefault = codec.LCapabilityQualityDefault;
        CSEncoderQualityLabel = codec.LCapabilityQualityLabel;
        CSEncoderQualityOption = codec.LCapabilityQualityOption;
        CSEncoderQualityMinimum = codec.LCapabilityQualityMinimum;
        CSEncoderQualityMaximum = codec.LCapabilityQualityMaximum;
        CSEncoderQualityStep = codec.LCapabilityQualityStep;
        CSEncoderQualityAscending = codec.LCapabilityQualityAscending;
        CSEncoderQualityBitrate = codec.LCapabilityQualityBitrate;
        CSEncoderQualityRange = codec.LCapabilityQualityRange;
    }

    public string CSEncoderQualityLabel { get; }

    public string CSEncoderQualityOption { get; }

    public double? CSEncoderQualityMinimum { get; }

    public double? CSEncoderQualityMaximum { get; }

    public double CSEncoderQualityStep { get; }

    public bool CSEncoderQualityAscending { get; }

    public bool CSEncoderQualityBitrate { get; }

    public string CSEncoderQualityRange { get; }

    public string CSEncoderQualityText
    {
        get
        {
            (string rate, string quality, _, _) = _csEncoderQualityPort.LSEncoderRateRead();
            return CSEncoder.CSEncoderStoredRead(_csEncoderQualityRate, rate, quality, _csEncoderQualityDefault);
        }
    }
}
