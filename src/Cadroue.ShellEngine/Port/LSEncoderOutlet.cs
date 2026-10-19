namespace Cadroue.ShellEngine;

// The draft over the read and the save the encoder window hands in with its draft preset.
public sealed class LSEncoderOutlet : LSEncoderPort
{
    private readonly Func<(string, string, string)> _lsEncoderReader;
    private readonly Action<string, string, string, string, string, string, string> _lsEncoderWriter;

    public LSEncoderOutlet(
        Func<(string, string, string)> lSource,
        Action<string, string, string, string, string, string, string> lOutput)
    {
        ArgumentNullException.ThrowIfNull(lSource);
        ArgumentNullException.ThrowIfNull(lOutput);
        _lsEncoderReader = lSource;
        _lsEncoderWriter = lOutput;
    }

    public (string LSEncoderDisplay, string LSEncoderCodec, string LSEncoderRate) LSEncoderDraftRead() =>
        _lsEncoderReader();

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
