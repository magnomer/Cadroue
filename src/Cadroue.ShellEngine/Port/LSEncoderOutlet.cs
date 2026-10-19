namespace Cadroue.ShellEngine;

// The draft over the read and the save the encoder window hands in with its draft preset.
public sealed class LSEncoderOutlet : LSEncoderPort
{
    private readonly Func<(string, string, string)> _lsEncoderReader;
    private readonly Action<string, string, string, string, string, string, string> _lsEncoderWriter;
    private readonly Func<(string, string, string, IReadOnlyDictionary<string, string>)> _lsEncoderRates;

    public LSEncoderOutlet(
        Func<(string, string, string)> lSource,
        Action<string, string, string, string, string, string, string> lOutput,
        Func<(string, string, string, IReadOnlyDictionary<string, string>)> lRates)
    {
        ArgumentNullException.ThrowIfNull(lSource);
        ArgumentNullException.ThrowIfNull(lOutput);
        ArgumentNullException.ThrowIfNull(lRates);
        _lsEncoderReader = lSource;
        _lsEncoderWriter = lOutput;
        _lsEncoderRates = lRates;
    }

    public (string LSEncoderDisplay, string LSEncoderCodec, string LSEncoderRate) LSEncoderDraftRead() =>
        _lsEncoderReader();

    public (string, string, string, IReadOnlyDictionary<string, string>) LSEncoderRateRead() =>
        _lsEncoderRates();

    public void LSEncoderDraftSave(
        string lDisplay,
        string lStream,
        string lVideoEncoder,
        string lVideoRate,
        string lSize,
        string lAudioEncoder,
        string lAudioRate) =>
        _lsEncoderWriter(lDisplay, lStream, lVideoEncoder, lVideoRate, lSize, lAudioEncoder, lAudioRate);
}
