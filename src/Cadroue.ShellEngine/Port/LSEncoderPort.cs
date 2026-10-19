namespace Cadroue.ShellEngine;

// One encoder window's draft preset as its gate reads and saves it.
public interface LSEncoderPort
{
    // The draft's display name, video encoder and video rate control.
    (string LSEncoderDisplay, string LSEncoderCodec, string LSEncoderRate) LSEncoderDraftRead();

    (string, string, string, IReadOnlyDictionary<string, string>) LSEncoderRateRead();

    string LSEncoderSizeRead();

    // The draft's audio encoder and audio rate control.
    (string, string) LSEncoderAudioRead();

    // Writes the kept values into the draft, then copies the draft back into the source preset.
    void LSEncoderDraftSave(
        string lDisplay,
        string lStream,
        string lVideoEncoder,
        string lVideoRate,
        string lSize,
        string lAudioEncoder,
        string lAudioRate);
}
