using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The draft over the read and the save the encoder window hands in with its draft preset.
public sealed class LSEncoderOutlet : LSEncoderPort
{
    private readonly LPresetSelection _lsEncoderOutletSelection;
    private readonly LPreset _lsEncoderOutletSource;
    private readonly LSEncoderDraft _lsEncoderOutletDraft;

    internal LSEncoderOutlet(string lPreset)
    {
        ArgumentNullException.ThrowIfNull(lPreset);
        _lsEncoderOutletSelection = new LPresetSelection(lPreset, string.Empty);
        _lsEncoderOutletSource = LPreset.LPresetStateCreate(_lsEncoderOutletSelection.LPresetSelectionValue);
        _lsEncoderOutletDraft = new LSEncoderDraft(_lsEncoderOutletSource);
    }

    public static Func<string, LSEncoderPort> LSEncoderOutletCreate() => lPreset => new LSEncoderOutlet(lPreset);

    public (string, bool, string, string, string) LSEncoderVideoRead() => _lsEncoderOutletDraft.LSEncoderVideoRead();

    public void LSEncoderVideoSave(
        string lMode,
        string lQuality,
        string lSpeed,
        bool lReactive,
        string lFps,
        string lLayout,
        IReadOnlyDictionary<string, string> lExtras) =>
        _lsEncoderOutletDraft.LSEncoderVideoSave(lMode, lQuality, lSpeed, lReactive, lFps, lLayout, lExtras);

    public (string LSEncoderDisplay, string LSEncoderCodec, string LSEncoderRate) LSEncoderDraftRead() =>
        _lsEncoderOutletDraft.LSEncoderDraftRead();

    public (string, string, string, IReadOnlyDictionary<string, string>) LSEncoderRateRead() =>
        _lsEncoderOutletDraft.LSEncoderRateRead();

    public string LSEncoderSizeRead() => _lsEncoderOutletDraft.LSEncoderSizeRead();

    public (string, string, string, string, string, string, IReadOnlyDictionary<string, string>, string, string)
        LSEncoderAudioRead() => _lsEncoderOutletDraft.LSEncoderAudioRead();

    public (IReadOnlyList<string> LSEncoderLayouts, IReadOnlyList<int> LSEncoderSamples) LSEncoderHelpRead(
        string lEncoder) =>
        (LInventoryEngine.LInventoryLayoutSeam(lEncoder), LInventoryEngine.LInventorySampleSeam(lEncoder));

    public void LSEncoderHelpAttach(Action<string> lHandler) => LInventoryEngine.LInventoryEnrollmentSeam(lHandler);

    public void LSEncoderHelpDetach(Action<string> lHandler) => LInventoryEngine.LInventoryWithdrawalSeam(lHandler);

    public string LSEncoderSuffixRead(string lPolicy) =>
        _lsEncoderOutletDraft.LSEncoderDraftPreset.LPresetSuffixRead(lPolicy);

    public void LSEncoderSuffixSet(string lPolicy, string lSuffix) =>
        _lsEncoderOutletDraft.LSEncoderDraftPreset.LPresetSuffixSet(lPolicy, lSuffix);

    public string LSEncoderLocationRead(string lMode) =>
        _lsEncoderOutletDraft.LSEncoderDraftPreset.LPresetLocationRead(lMode);

    public void LSEncoderLocationSet(string lMode, string lFolder) =>
        _lsEncoderOutletDraft.LSEncoderDraftPreset.LPresetLocationSet(lMode, lFolder);

    public (string, string, string, string, string) LSEncoderOutputRead()
    {
        LPreset lDraft = _lsEncoderOutletDraft.LSEncoderDraftPreset;
        return (lDraft.LPresetDisplay,
            lDraft.LPresetContainer,
            lDraft.LPresetExtension,
            lDraft.LPresetCollision,
            lDraft.LPresetLocation);
    }

    public void LSEncoderOutputSet(
        string lDisplay, string lContainer, string lExtension, string lCollision, string lLocation)
    {
        LPreset lDraft = _lsEncoderOutletDraft.LSEncoderDraftPreset;
        lDraft.LPresetDisplay = lDisplay;
        lDraft.LPresetContainer = lContainer;
        lDraft.LPresetExtension = lExtension;
        lDraft.LPresetCollision = lCollision;
        lDraft.LPresetLocation = lLocation;
    }

    public IReadOnlyList<string> LSEncoderExtensionRead(string lContainer) =>
        LPreset.LPresetExtensionsRead(lContainer);

    public void LSEncoderAudioSet(
        string lStream,
        string lMode,
        string lQuality,
        string lSpeed,
        IReadOnlyDictionary<string, string> lExtras,
        string lSample,
        string lChannels)
    {
        LPresetAudio lAudio = _lsEncoderOutletDraft.LSEncoderDraftPreset.LPresetAudio;
        lAudio.LPresetStream = lStream;
        lAudio.LPresetMode = lMode;
        lAudio.LPresetQuality = lQuality;
        lAudio.LPresetSpeed = lSpeed;
        lAudio.LPresetExtras = new Dictionary<string, string>(lExtras, StringComparer.Ordinal);
        lAudio.LPresetSampleRate = lSample;
        lAudio.LPresetChannels = lChannels;
    }

    public void LSEncoderDraftSave(
        string lDisplay,
        string lStream,
        string lVideoEncoder,
        string lVideoRate,
        string lSize,
        string lAudioEncoder,
        string lAudioRate) =>
        _lsEncoderOutletDraft.LSEncoderDraftSave(
            lDisplay, lStream, lVideoEncoder, lVideoRate, lSize, lAudioEncoder, lAudioRate);

    public void LSEncoderSave()
    {
        LPresetDocument lSource = _lsEncoderOutletSource.LPresetRecordCreate();
        lSource.LPresetName = _lsEncoderOutletSelection.LPresetSelectionName;
        _lsEncoderOutletSelection.LPresetSelectionValue = lSource;
    }

    public void LSEncoderClose() => _lsEncoderOutletSelection.LPresetSelectionClose();
}
