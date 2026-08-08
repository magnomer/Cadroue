using System;
using System.Collections.Generic;

namespace Cadroue.Core;

public enum LGrain
{
    LGrainWhite,
    LGrainVinyl,
    LGrainShellac
}

public sealed record LGrainPreset(
    string LGrainToken,
    double LGrainReduction,
    double LGrainFloor,
    double LGrainSmooth,
    double LGrainAdaptivity,
    double LGrainResidual,
    LGrain LGrainType);

public static class LGrainCatalog
{
    public static readonly IReadOnlyList<LGrainPreset> LGrainPresets = new[]
    {
        new LGrainPreset("Light", 8, -50, 4, 0.5, -38, LGrain.LGrainWhite),
        new LGrainPreset("Medium", 12, -50, 6, 0.5, -38, LGrain.LGrainWhite),
        new LGrainPreset("Strong", 24, -45, 10, 0.4, -30, LGrain.LGrainWhite),
        new LGrainPreset("Dialogue", 10, -50, 5, 0.8, -40, LGrain.LGrainWhite),
        new LGrainPreset("Vinyl", 12, -50, 6, 0.5, -38, LGrain.LGrainVinyl),
        new LGrainPreset("Shellac", 12, -50, 8, 0.5, -35, LGrain.LGrainShellac)
    };

    public static LGrainPreset? LGrainRead(string lToken)
    {
        foreach (LGrainPreset lPreset in LGrainPresets)
        {
            if (lPreset.LGrainToken == lToken)
            {
                return lPreset;
            }
        }

        return null;
    }

    public static string? LGrainMatch(
        double lReduction,
        double lFloor,
        double lSmooth,
        double lAdaptivity,
        double lResidual,
        LGrain lType)
    {
        foreach (LGrainPreset lPreset in LGrainPresets)
        {
            if (Math.Abs(lReduction - lPreset.LGrainReduction) < 0.05
                && Math.Abs(lFloor - lPreset.LGrainFloor) < 0.05
                && Math.Abs(lSmooth - lPreset.LGrainSmooth) < 0.05
                && Math.Abs(lAdaptivity - lPreset.LGrainAdaptivity) < 0.005
                && Math.Abs(lResidual - lPreset.LGrainResidual) < 0.05
                && lType == lPreset.LGrainType)
            {
                return lPreset.LGrainToken;
            }
        }

        return null;
    }
}
