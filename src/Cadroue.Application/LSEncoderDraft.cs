namespace Cadroue.Application;

// One encoder window's draft over its source preset: the draft is the source's clone, read and written for the
// window, and a save copies it back into the source. The draft preset is lent whole to the rows that still write it
// directly, until their own gates take those rows.
public sealed class LSEncoderDraft
{
    private readonly LPreset _lsEncoderDraftSource;

    public LSEncoderDraft(LPreset lSource)
    {
        ArgumentNullException.ThrowIfNull(lSource);
        _lsEncoderDraftSource = lSource;
        LSEncoderDraftPreset = lSource.LPresetClone();
    }

    public LPreset LSEncoderDraftPreset { get; }

    // The draft's display name, video encoder and video rate control.
    public (string, string, string) LSEncoderDraftRead() =>
        (LSEncoderDraftPreset.LPresetDisplay,
            LSEncoderDraftPreset.LPresetVideo.LPresetEncoder,
            LSEncoderDraftPreset.LPresetVideo.LPresetRateControl);

    public (string, string, string, IReadOnlyDictionary<string, string>) LSEncoderRateRead() =>
        (LSEncoderDraftPreset.LPresetVideo.LPresetRateControl,
            LSEncoderDraftPreset.LPresetVideo.LPresetQuality,
            LSEncoderDraftPreset.LPresetVideo.LPresetSpeedPreset,
            LSEncoderDraftPreset.LPresetVideo.LPresetExtras);

    public string LSEncoderSizeRead() => LSEncoderDraftPreset.LPresetVideo.LPresetSize;

    // The draft's audio encoder and audio rate control.
    public (string, string) LSEncoderAudioRead() =>
        (LSEncoderDraftPreset.LPresetAudio.LPresetEncoder, LSEncoderDraftPreset.LPresetAudio.LPresetRateControl);

    // Writes the kept values into the draft, then copies the draft back into the source preset.
    public void LSEncoderDraftSave(
        string lDisplay,
        string lStream,
        string lVideoEncoder,
        string lVideoRate,
        string lSize,
        string lAudioEncoder,
        string lAudioRate)
    {
        LSEncoderDraftPreset.LPresetDisplay = lDisplay;
        LSEncoderDraftPreset.LPresetVideo.LPresetStream = lStream;
        LSEncoderDraftPreset.LPresetVideo.LPresetEncoder = lVideoEncoder;
        LSEncoderDraftPreset.LPresetVideo.LPresetRateControl = lVideoRate;
        LSEncoderDraftPreset.LPresetVideo.LPresetSize = lSize;
        LSEncoderDraftPreset.LPresetAudio.LPresetEncoder = lAudioEncoder;
        LSEncoderDraftPreset.LPresetAudio.LPresetRateControl = lAudioRate;
        _lsEncoderDraftSource.LPresetCopy(LSEncoderDraftPreset);
    }
}
