namespace Cadroue.ShellEngine;

// One encoder window's draft preset as its gate reads and saves it.
public interface LSEncoderPort
{
    // The draft's display name, video encoder and video rate control.
    (string LSEncoderDisplay, string LSEncoderCodec, string LSEncoderRate) LSEncoderDraftRead();

    (string, string, string, IReadOnlyDictionary<string, string>) LSEncoderRateRead();

    string LSEncoderSizeRead();

    // The draft's audio encoder and audio rate control.
    (string, string, string, string, string, string, IReadOnlyDictionary<string, string>, string, string)
        LSEncoderAudioRead();

    (string, bool, string, string, string) LSEncoderVideoRead();

    (IReadOnlyList<string> LSEncoderLayouts, IReadOnlyList<int> LSEncoderSamples) LSEncoderHelpRead(string lEncoder);

    void LSEncoderHelpAttach(Action<string> lHandler);

    void LSEncoderHelpDetach(Action<string> lHandler);

    string LSEncoderSuffixRead(string lPolicy);

    void LSEncoderSuffixSet(string lPolicy, string lSuffix);

    string LSEncoderLocationRead(string lMode);

    void LSEncoderLocationSet(string lMode, string lFolder);

    (string, string, string, string, string) LSEncoderOutputRead();

    void LSEncoderOutputSet(string lDisplay, string lContainer, string lExtension, string lCollision, string lLocation);

    IReadOnlyList<string> LSEncoderExtensionRead(string lContainer);

    void LSEncoderAudioSet(
        string lStream,
        string lMode,
        string lQuality,
        string lSpeed,
        IReadOnlyDictionary<string, string> lExtras,
        string lSample,
        string lChannels);

    void LSEncoderVideoSave(
        string lMode,
        string lQuality,
        string lSpeed,
        bool lReactive,
        string lFps,
        string lLayout,
        IReadOnlyDictionary<string, string> lExtras);

    // Writes the kept values into the draft, then copies the draft back into the source preset.
    void LSEncoderDraftSave(
        string lDisplay,
        string lStream,
        string lVideoEncoder,
        string lVideoRate,
        string lSize,
        string lAudioEncoder,
        string lAudioRate);

    void LSEncoderSave();

    void LSEncoderClose();
}
