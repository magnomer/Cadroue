using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The noise reduction rules from the grain catalogue, on plain values in slot order: reduction, smoothing, floor,
// residual, adaptivity. Every value is held inside the catalogue's range; a preset is matched on values and type.
public sealed class LNoiseFacade
{
    internal LNoiseFacade()
    {
    }

    private static readonly LGrain[] _lNoiseTypes = { LGrain.LGrainWhite, LGrain.LGrainVinyl, LGrain.LGrainShellac };

    private static readonly IReadOnlyList<double> _lNoiseDefaults =
        Array.AsReadOnly(new double[] { 12, 6, -50, -38, 0.5 });

    public IReadOnlyList<double> LNoiseLeasts { get; } = Array.AsReadOnly(new[]
    {
        LGrainCatalog.LGrainReductionLeast,
        LGrainCatalog.LGrainSmoothLeast,
        LGrainCatalog.LGrainFloorLeast,
        LGrainCatalog.LGrainFloorLeast,
        LGrainCatalog.LGrainAdaptivityLeast
    });

    public IReadOnlyList<double> LNoiseMosts { get; } = Array.AsReadOnly(new[]
    {
        LGrainCatalog.LGrainReductionMost,
        LGrainCatalog.LGrainSmoothMost,
        LGrainCatalog.LGrainFloorMost,
        LGrainCatalog.LGrainFloorMost,
        LGrainCatalog.LGrainAdaptivityMost
    });

    public IReadOnlyList<string> LNoiseTokensRead() =>
        LGrainCatalog.LGrainPresets.Select(lPreset => lPreset.LGrainToken).ToList();

    public IReadOnlyList<string> LNoiseTypesRead() => _lNoiseTypes.Select(LGrainCatalog.LGrainFormat).ToList();

    // The default step: the default values on white noise, with the preset they match.
    public (IReadOnlyList<double> LNoiseValues, int LNoiseType, string? LNoisePreset) LNoiseOriginRead()
    {
        IReadOnlyList<double> lValues = LNoiseNormalize(_lNoiseDefaults);
        return (lValues, 0, LNoiseMatchFind(lValues, 0));
    }

    // The values a reset returns to: the chosen preset's, or the default ones without a known preset.
    public IReadOnlyList<double> LNoiseDefaultRead(string? lToken) =>
        lToken is not null && LGrainCatalog.LGrainRead(lToken) is { } lPreset
            ? LNoiseValuesRead(lPreset)
            : _lNoiseDefaults;

    public IReadOnlyList<double> LNoiseNormalize(IReadOnlyList<double> lValues)
    {
        ArgumentNullException.ThrowIfNull(lValues);
        return LNoiseValuesRead((LWorkNoiseStep)LNoiseStepCreate(false, lValues, false, 0));
    }

    // The preset's values held inside their ranges, with its type; null for an unknown preset.
    public (IReadOnlyList<double> LNoiseValues, int LNoiseType)? LNoisePresetRead(string lToken)
    {
        if (LGrainCatalog.LGrainRead(lToken) is not { } lPreset)
        {
            return null;
        }

        return (LNoiseNormalize(LNoiseValuesRead(lPreset)), Array.IndexOf(_lNoiseTypes, lPreset.LGrainType));
    }

    public string? LNoiseMatchFind(IReadOnlyList<double> lValues, int lType)
    {
        ArgumentNullException.ThrowIfNull(lValues);
        return LGrainCatalog.LGrainMatch(
            lValues[0], lValues[2], lValues[1], lValues[4], lValues[3], _lNoiseTypes[lType]);
    }

    // The audio step for the gate's switch, values, tracking and type; the values are held inside their ranges.
    internal static LWorkAudioStep LNoiseStepCreate(bool lActive, IReadOnlyList<double> lValues, bool lTrack, int lType)
    {
        ArgumentNullException.ThrowIfNull(lValues);
        return LWorkAudioStep.LWorkNoiseCreate(
            lActive, lValues[0], lValues[2], lTrack, _lNoiseTypes[lType], lValues[1], lValues[4], lValues[3]);
    }

    internal static IReadOnlyList<double> LNoiseValuesRead(LWorkNoiseStep lStep) =>
        Array.AsReadOnly(new[]
        {
            lStep.LWorkNoiseReduction,
            lStep.LWorkNoiseSmooth,
            lStep.LWorkNoiseFloor,
            lStep.LWorkNoiseResidual,
            lStep.LWorkNoiseAdaptivity
        });

    internal static int LNoiseTypeRead(LWorkNoiseStep lStep) => Array.IndexOf(_lNoiseTypes, lStep.LWorkNoiseType);

    private static IReadOnlyList<double> LNoiseValuesRead(LGrainPreset lPreset) =>
        Array.AsReadOnly(new[]
        {
            lPreset.LGrainReduction,
            lPreset.LGrainSmooth,
            lPreset.LGrainFloor,
            lPreset.LGrainResidual,
            lPreset.LGrainAdaptivity
        });
}
