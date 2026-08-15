using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LEncodeChain
{
    internal static int LEncodePassRead(LWorkAudio lWorkAudio)
    {
        int lFound = -1;
        for (int lIndex = 0; lIndex < lWorkAudio.LWorkAudioSteps.Count; lIndex++)
        {
            if (!lWorkAudio.LWorkAudioSteps[lIndex].LWorkStepLoudness)
            {
                continue;
            }

            if (lFound >= 0)
            {
                return -1;
            }

            lFound = lIndex;
        }

        return lFound;
    }

    internal static string? LEncodeChainBuild(LWorkAudio lWorkAudio, LEncodeChainMode lChainMode, int lTwoPassIndex)
    {
        string lGraph = lWorkAudio.LWorkAudioFormat();
        if (lGraph.Length == 0)
        {
            return null;
        }

        if (lTwoPassIndex < 0)
        {
            return lGraph;
        }

        string lToken = lChainMode switch
        {
            LEncodeChainMode.LEncodeChainAnalyze => ":print_format=json",
            LEncodeChainMode.LEncodeChainApply => LEncode.LEncodeMeasureToken,
            _ => string.Empty
        };

        if (lToken.Length == 0)
        {
            return lGraph;
        }

        string[] lFilters = lGraph.Split(',');
        for (int lIndex = 0; lIndex < lFilters.Length; lIndex++)
        {
            if (lFilters[lIndex].StartsWith("loudnorm=", StringComparison.Ordinal))
            {
                lFilters[lIndex] += lToken;
                break;
            }
        }

        return string.Join(',', lFilters);
    }
}
