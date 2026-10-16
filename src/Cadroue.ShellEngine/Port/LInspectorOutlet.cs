using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One plan read for the inspector gate: each tool's step as plain values, or the Core default step when the plan has
// none. A video plan answers the audio tools from an empty audio plan, and the reverse.
public sealed class LInspectorOutlet : LInspectorPort
{
    private readonly LWorkVideo _lInspectorVideo;
    private readonly LWorkAudio _lInspectorAudio;

    public LInspectorOutlet(LWorkVideo lVideo)
    {
        ArgumentNullException.ThrowIfNull(lVideo);
        _lInspectorVideo = lVideo;
        _lInspectorAudio = new LWorkAudio(new List<LWorkAudioStep>());
    }

    public LInspectorOutlet(LWorkAudio lAudio)
    {
        ArgumentNullException.ThrowIfNull(lAudio);
        _lInspectorVideo = LWorkVideo.LWorkVideoCreate();
        _lInspectorAudio = lAudio;
    }

    public (bool LInspectorActive, IReadOnlyList<double> LInspectorValues) LInspectorGammaRead()
    {
        LWorkVideoStep lStep = LInspectorVideoFind(LColorKind.LColorKindGamma)
            ?? LWorkVideoStep.LWorkGammaCreate(false, 0);
        return (lStep.LWorkStepActive, lStep.LWorkGammaRead().LWorkSlotRead());
    }

    public (bool LInspectorActive, double LInspectorValue) LInspectorExposureRead()
    {
        LWorkVideoStep lStep = LInspectorVideoFind(LColorKind.LColorKindExposure)
            ?? LWorkVideoStep.LWorkExposureCreate(false, 0);
        return (lStep.LWorkStepActive, lStep.LWorkStepValue);
    }

    public (bool LInspectorActive, double LInspectorValue) LInspectorVolumeRead()
    {
        LWorkAudioStep lStep = LInspectorAudioFind(LAudioKind.LAudioKindVolume)
            ?? LWorkAudioStep.LWorkVolumeCreate(false, 0);
        return (lStep.LWorkStepActive, lStep is LWorkVolumeStep lVolume ? lVolume.LWorkVolumeGain : 0);
    }

    private LWorkVideoStep? LInspectorVideoFind(LColorKind lKind) =>
        _lInspectorVideo.LWorkVideoSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);

    private LWorkAudioStep? LInspectorAudioFind(LAudioKind lKind) =>
        _lInspectorAudio.LWorkAudioSteps.FirstOrDefault(lStep => lStep.LWorkStepKind == lKind);
}
