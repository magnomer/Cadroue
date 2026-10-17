using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The equalizer rules for its gate. Each edit answers the whole band list with the preset it matches, or null.
public interface LEqualizerPort
{
    double LEqualizerGainLeast { get; }

    double LEqualizerGainMost { get; }

    double LEqualizerFrequencyOrigin { get; }

    IReadOnlyList<string> LEqualizerTokensRead();

    (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset) LEqualizerOriginRead();

    (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset) LEqualizerBandSet(
        IReadOnlyList<(double, double)> lBands, int lIndex, double lFrequency, double lGain);

    (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset) LEqualizerBandAdd(
        IReadOnlyList<(double, double)> lBands);

    (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset) LEqualizerBandRemove(
        IReadOnlyList<(double, double)> lBands, int lIndex);

    (IReadOnlyList<(double, double)> LEqualizerBands, string? LEqualizerPreset)? LEqualizerPresetRead(string lToken);

    // The audio step for the gate's switch and bands; static, since it reads only the values given.
    static LWorkAudioStep LEqualizerStepCreate(bool lActive, IReadOnlyList<(double, double)> lBands) =>
        LEqualizerFacade.LEqualizerStepCreate(lActive, lBands);
}
