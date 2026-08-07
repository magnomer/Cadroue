using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Core;

public static class LSidecarKeyframe
{
    public static List<long> LSidecarKeyframeFormat(IReadOnlyCollection<long> lSidecarKeyframeMilliseconds)
    {
        var lSidecarDeltas = new List<long>(lSidecarKeyframeMilliseconds.Count);
        long lSidecarPrevious = 0;
        foreach (long lSidecarKeyframe in lSidecarKeyframeMilliseconds
                     .Where(lKeyframe => lKeyframe >= 0)
                     .Distinct()
                     .OrderBy(lKeyframe => lKeyframe))
        {
            lSidecarDeltas.Add(lSidecarKeyframe - lSidecarPrevious);
            lSidecarPrevious = lSidecarKeyframe;
        }

        return lSidecarDeltas;
    }

    public static IReadOnlyList<long> LSidecarKeyframeParse(IReadOnlyList<long> lSidecarDeltas)
    {
        var lSidecarKeyframes = new List<long>(lSidecarDeltas.Count);
        long lSidecarRunning = 0;
        foreach (long lSidecarDelta in lSidecarDeltas)
        {
            lSidecarRunning += lSidecarDelta;
            if (lSidecarRunning >= 0)
            {
                lSidecarKeyframes.Add(lSidecarRunning);
            }
        }

        return lSidecarKeyframes;
    }

    public static int LSidecarKeyframeCountRead(IReadOnlyList<long> lSidecarKeyframes) => lSidecarKeyframes.Count;

    public static long LSidecarKeyframeLastRead(IReadOnlyList<long> lSidecarKeyframes) =>
        lSidecarKeyframes.Count == 0 ? 0 : lSidecarKeyframes[^1];

    public static bool LSidecarKeyframeIntegrityCheck(
        IReadOnlyList<long> lSidecarKeyframes,
        int lSidecarExpectedCount,
        long lSidecarExpectedLast) =>
        lSidecarKeyframes.Count == lSidecarExpectedCount
        && LSidecarKeyframeLastRead(lSidecarKeyframes) == lSidecarExpectedLast;
}
