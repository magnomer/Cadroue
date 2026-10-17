using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The noise reduction rules for its gate, on values in slot order: reduction, smoothing, floor, residual,
// adaptivity. The type is an index into the type tokens.
public interface LNoisePort
{
    IReadOnlyList<double> LNoiseLeasts { get; }

    IReadOnlyList<double> LNoiseMosts { get; }

    IReadOnlyList<string> LNoiseTokensRead();

    IReadOnlyList<string> LNoiseTypesRead();

    (IReadOnlyList<double> LNoiseValues, int LNoiseType, string? LNoisePreset) LNoiseOriginRead();

    IReadOnlyList<double> LNoiseDefaultRead(string? lToken);

    IReadOnlyList<double> LNoiseNormalize(IReadOnlyList<double> lValues);

    (IReadOnlyList<double> LNoiseValues, int LNoiseType)? LNoisePresetRead(string lToken);

    string? LNoiseMatchFind(IReadOnlyList<double> lValues, int lType);

    // The audio step for the gate's switch, values, tracking and type; static, since it reads only the values given.
    static LWorkAudioStep LNoiseStepCreate(bool lActive, IReadOnlyList<double> lValues, bool lTrack, int lType) =>
        LNoiseFacade.LNoiseStepCreate(lActive, lValues, lTrack, lType);
}
