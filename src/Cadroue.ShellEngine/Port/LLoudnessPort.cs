using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The leveling rules for its gate. Values run in one slot order: target, peak, range, then frame, smoothness,
// maximum gain, compression; the loudness mode reads the first three slots and the dynamic mode the last four.
public interface LLoudnessPort
{
    IReadOnlyList<double> LLoudnessLeastsRead(bool lDynamic);

    IReadOnlyList<double> LLoudnessMostsRead(bool lDynamic);

    IReadOnlyList<string> LLoudnessTokensRead(bool lDynamic);

    (bool LLoudnessDynamic, IReadOnlyList<double> LLoudnessValues, bool LLoudnessTwoPass, string? LLoudnessPreset)
        LLoudnessOriginRead();

    IReadOnlyList<double> LLoudnessDefaultRead(bool lDynamic, string? lToken);

    IReadOnlyList<double> LLoudnessNormalize(IReadOnlyList<double> lValues);

    IReadOnlyList<double>? LLoudnessPresetRead(bool lDynamic, string lToken, IReadOnlyList<double> lValues);

    string? LLoudnessMatchFind(bool lDynamic, IReadOnlyList<double> lValues);

    // The leveling step for the gate's switch, mode, values and two-pass; static, since it reads only the values given.
    static LWorkAudioStep LLoudnessStepCreate(
        bool lActive, bool lDynamic, IReadOnlyList<double> lValues, bool lTwoPass) =>
        LLoudnessFacade.LLoudnessStepCreate(lActive, lDynamic, lValues, lTwoPass);
}
