namespace Cadroue.ShellEngine;

// The draft over the read and the save the encoder window hands in with its draft preset.
public sealed class LSEncoderOutlet : LSEncoderPort
{
    private readonly Func<(string, string, string)> _lsEncoderReader;
    private readonly Action<string, string, string, string, string, string, string> _lsEncoderWriter;
    private readonly Func<(string, string, string, IReadOnlyDictionary<string, string>)> _lsEncoderRates;
    private readonly Func<string> _lsEncoderSize;
    private readonly Func<(string, string)> _lsEncoderAudioReader;

    public LSEncoderOutlet(
        Func<(string, string, string)> lSource,
        Action<string, string, string, string, string, string, string> lOutput,
        Func<(string, string, string, IReadOnlyDictionary<string, string>)> lRates,
        Func<string> lSize,
        Func<(string, string)> lAudio)
    {
        ArgumentNullException.ThrowIfNull(lSource);
        ArgumentNullException.ThrowIfNull(lOutput);
        ArgumentNullException.ThrowIfNull(lRates);
        ArgumentNullException.ThrowIfNull(lSize);
        ArgumentNullException.ThrowIfNull(lAudio);
        _lsEncoderReader = lSource;
        _lsEncoderWriter = lOutput;
        _lsEncoderRates = lRates;
        _lsEncoderSize = lSize;
        _lsEncoderAudioReader = lAudio;
    }

    public (string LSEncoderDisplay, string LSEncoderCodec, string LSEncoderRate) LSEncoderDraftRead() =>
        _lsEncoderReader();

    public (string, string, string, IReadOnlyDictionary<string, string>) LSEncoderRateRead() =>
        _lsEncoderRates();

    public string LSEncoderSizeRead() => _lsEncoderSize();

    public (string, string) LSEncoderAudioRead() => _lsEncoderAudioReader();

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
