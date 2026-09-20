using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    public event Action? PInspectorAudioChange;

    public LVolume LVolume => LInspector.LInspectorAudio.LInspectorVolume;

    public LLoudness LLoudness => LInspector.LInspectorAudio.LInspectorLoudness;

    public LNoise LNoise => LInspector.LInspectorAudio.LInspectorNoise;

    public LFilter LFilterHigh => LInspector.LInspectorAudio.LInspectorHighpass;

    public LFilter LFilterLow => LInspector.LInspectorAudio.LInspectorLowpass;

    public LEqualizer LEqualizer => LInspector.LInspectorAudio.LInspectorEqualizer;

    public LSkip LSkip => LInspector.LInspectorSkip;

    private void PInspectorAudioAttach()
    {
        LVolume.LVolumeChange += PVolumeUpdate;
        LLoudness.LLoudnessChange += PLoudnessUpdate;
        LNoise.LNoiseChange += PNoiseUpdate;
        LFilterHigh.LFilterChange += () => PFilterUpdate(pInspectorHighPass);
        LFilterLow.LFilterChange += () => PFilterUpdate(pInspectorLowPass);
        LEqualizer.LEqualizerChange += PEqualizerUpdate;
        LSkip.LSkipChange += PSkipUpdate;
        LInspector.LInspectorAudio.LInspectorAudioChange += PInspectorActiveRaise;
        PVolumeUpdate();
        PLoudnessUpdate();
        PNoiseUpdate();
        PFilterUpdate(pInspectorHighPass);
        PFilterUpdate(pInspectorLowPass);
        PEqualizerUpdate();
        PSkipUpdate();
    }

    private void PInspectorActiveRaise() => PInspectorAudioChange?.Invoke();
}
