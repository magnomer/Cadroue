using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal static partial class TInterface
{
    internal static IReadOnlyList<string> TContourTokensRead() => LContourCatalog.LContourTokensRead();
    internal static double[]? TContourGainsRead(string token) => LContourCatalog.LContourGainsRead(token);
    internal static bool TContourMatch(
        IReadOnlyList<double> frequencies, IReadOnlyList<double> gains, double[] expected) =>
        LContourCatalog.LContourMatch(frequencies, gains, expected);
    internal static string? TContourPresetFind(IReadOnlyList<double> frequencies, IReadOnlyList<double> gains) =>
        LContourCatalog.LContourPresetFind(frequencies, gains);

    internal static string TGrainFormat(LGrain grain) => LGrainCatalog.LGrainFormat(grain);
    internal static LGrain TGrainParse(string token) => LGrainCatalog.LGrainParse(token);
    internal static LGrainPreset? TGrainRead(string token) => LGrainCatalog.LGrainRead(token);
    internal static string? TGrainMatch(
        double reduction, double floor, double smooth, double adaptivity, double residual, LGrain grain) =>
        LGrainCatalog.LGrainMatch(reduction, floor, smooth, adaptivity, residual, grain);

    internal static LPassbandPreset? TPassbandRead(bool high, string token) => LPassband.LPassbandRead(high, token);
    internal static string? TPassbandMatch(bool high, double frequency, int stages, int poles, double resonance) =>
        LPassband.LPassbandMatch(high, frequency, stages, poles, resonance);
    internal static LWorkAudioStep TPassbandStepCreate(bool high, bool active) =>
        LPassband.LPassbandStepCreate(high, active);

    internal static LLevelingLoudnessPreset? TLevelingLoudnessRead(string token) =>
        LLevelingCatalog.LLevelingLoudnessRead(token);
    internal static LLevelingDynamicPreset? TLevelingDynamicRead(string token) =>
        LLevelingCatalog.LLevelingDynamicRead(token);
    internal static string? TLevelingLoudnessMatch(double target, double peak, double range) =>
        LLevelingCatalog.LLevelingLoudnessMatch(target, peak, range);
    internal static string? TLevelingDynamicMatch(double frame, double gauss, double maxGain, double compress) =>
        LLevelingCatalog.LLevelingDynamicMatch(frame, gauss, maxGain, compress);
    internal static LLevelingDefault TLevelingDefaultRead() => LLevelingCatalog.LLevelingDefaultRead();

    internal static IReadOnlyList<LRepertoireAudio> TRepertoireAudioRead() =>
        LRepertoireCatalog.LRepertoireAudioCandidates;
    internal static string? TRepertoireAudioResolve(string text) => LRepertoireCatalog.LRepertoireAudioResolve(text);
    internal static string TRepertoireAudioFind(string codecName) => LRepertoireCatalog.LRepertoireAudioFind(codecName);
    internal static IReadOnlyList<LRepertoireEncoder> TRepertoireEncodersRead() =>
        LRepertoireCatalog.LRepertoireEncodersRead();

    internal static LTrialResult TTrialResultCreate(bool success, string message) => new(success, message);
    internal static Task TTrialSetStart(Func<string, Task<LTrialResult>> trial) => LTrialSet.LTrialSetStart(trial);
    internal static IReadOnlySet<string>? TTrialSetRead() => LTrialSet.LTrialSetRead();
    internal static void TTrialSetApply(IEnumerable<string> available) => LTrialSet.LTrialSetApply(available);
    internal static void TTrialSetReset() => LTrialSet.LTrialSetReset();
    internal static bool TTrialSetCheck(LRepertoireEncoder candidate) => LTrialSet.LTrialSetCheck(candidate);
}
