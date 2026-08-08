using System;
using System.Collections.Generic;

namespace Cadroue.Core;

public static class LContourCatalog
{
    public static readonly double[] LContourBandGrid =
        { 31, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };

    private static readonly (string Token, double[] Gains)[] LContourPresets =
    {
        ("Flat", new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }),
        ("Bass boost", new double[] { 6, 5, 3, 1, 0, 0, 0, 0, 0, 0 }),
        ("Bright", new double[] { 0, 0, 0, 0, 0, 0, 1, 3, 5, 6 }),
        ("Warm", new double[] { 2, 3, 2, 1, 0, 0, -1, -2, -3, -2 }),
        ("Loudness", new double[] { 6, 4, 2, 0, -2, -3, -1, 1, 4, 6 }),
        ("Vocal", new double[] { -3, -2, 0, 1, 2, 2, 3, 2, 1, 0 }),
        ("De-ess", new double[] { 0, 0, 0, 0, 0, 0, 0, -2, -6, -3 }),
        ("Podcast", new double[] { -6, -3, 0, -1, 0, 1, 2, 3, 2, 1 }),
        ("Telephone", new double[] { -12, -10, -4, 0, 2, 4, 3, 0, -8, -12 })
    };

    public static IReadOnlyList<string> LContourTokensRead()
    {
        var lTokens = new string[LContourPresets.Length];
        for (int i = 0; i < LContourPresets.Length; i++)
        {
            lTokens[i] = LContourPresets[i].Token;
        }

        return lTokens;
    }

    public static double[]? LContourGainsRead(string lToken)
    {
        foreach ((string lPresetToken, double[] lGains) in LContourPresets)
        {
            if (lPresetToken == lToken)
            {
                return (double[])lGains.Clone();
            }
        }

        return null;
    }

    public static bool LContourMatch(
        IReadOnlyList<double> lFrequencies, IReadOnlyList<double> lGains, double[] lPresetGains)
    {
        if (lFrequencies.Count != 10 || lGains.Count != 10 || lPresetGains.Length != 10)
        {
            return false;
        }

        for (int i = 0; i < 10; i++)
        {
            if (Math.Abs(lFrequencies[i] - LContourBandGrid[i]) > 0.5
                || Math.Abs(lGains[i] - lPresetGains[i]) > 0.05)
            {
                return false;
            }
        }

        return true;
    }

    public static string? LContourPresetFind(IReadOnlyList<double> lFrequencies, IReadOnlyList<double> lGains)
    {
        foreach ((string lToken, double[] lPresetGains) in LContourPresets)
        {
            if (LContourMatch(lFrequencies, lGains, lPresetGains))
            {
                return lToken;
            }
        }

        return null;
    }
}
