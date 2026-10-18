using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LScoutBridge
{
    internal static async Task<IReadOnlyList<LKeyframeEntry>> LScoutBridgeRead(
        string lScoutSourcePath,
        TimeSpan lScoutOrigin,
        TimeSpan lScoutEnd,
        CancellationToken lScoutToken = default,
        LBridgeStream? lScoutStream = null)
    {
        if (string.IsNullOrWhiteSpace(lScoutSourcePath) || !File.Exists(lScoutSourcePath) || lScoutEnd <= lScoutOrigin)
        {
            return Array.Empty<LKeyframeEntry>();
        }

        try
        {
            lScoutStream ??= await LScoutStream.LScoutStreamRead(lScoutSourcePath, lScoutToken).ConfigureAwait(false);
            if (lScoutStream is not null
                && LKeyframeCodec.LKeyframeKindResolve(lScoutStream.LBridgeCodec) == LKeyframeKind.LKeyframeKindIntra)
            {
                return new[] { new LKeyframeEntry(lScoutOrigin), new LKeyframeEntry(lScoutEnd) };
            }

            IReadOnlyList<LKeyframeEntry> lScoutKeyframes = await LKeyframeEngine.LKeyframeScanSeam(
                lScoutSourcePath, lScoutOrigin, lScoutEnd, lScoutToken).ConfigureAwait(false);
            if (lScoutStream is null)
            {
                return lScoutKeyframes;
            }

            bool lScoutHevc = lScoutStream.LBridgeCodec.ToLowerInvariant() is "hevc" or "h265";
            var lScoutCandidates = lScoutKeyframes.ToList();
            while (lScoutCandidates.Count > 0
                && !await LScoutBoundaryCheck(
                    lScoutSourcePath,
                    lScoutStream.LBridgeCodec,
                    lScoutHevc,
                    lScoutCandidates[0].LKeyframePresentationTime,
                    lScoutToken).ConfigureAwait(false))
            {
                lScoutCandidates.RemoveAt(0);
            }

            bool lScoutEndKeyed = lScoutCandidates.Count > 0
                && (lScoutCandidates[^1].LKeyframePresentationTime - lScoutEnd).Duration()
                    <= TimeSpan.FromMilliseconds(1);
            while (!lScoutEndKeyed
                && lScoutCandidates.Count > 1
                && !await LScoutBoundaryCheck(
                    lScoutSourcePath,
                    lScoutStream.LBridgeCodec,
                    lScoutHevc,
                    lScoutCandidates[^1].LKeyframePresentationTime,
                    lScoutToken).ConfigureAwait(false))
            {
                lScoutCandidates.RemoveAt(lScoutCandidates.Count - 1);
            }

            return lScoutCandidates;
        }
        catch (Exception lScoutException) when (lScoutException is not OperationCanceledException)
        {
            LRunnerTrace.LRunnerRecord(
                $"Keyframes could not be read '{Path.GetFileName(lScoutSourcePath)}'",
                lScoutException);
            return Array.Empty<LKeyframeEntry>();
        }
    }

    private static async Task<bool> LScoutBoundaryCheck(
        string lScoutSourcePath,
        string lScoutCodec,
        bool lScoutHevc,
        TimeSpan lScoutKeyframe,
        CancellationToken lScoutToken)
    {
        bool? lScoutRefresh = await LScoutRefresh.LScoutRefreshRead(
            lScoutSourcePath, lScoutCodec, lScoutKeyframe, lScoutToken).ConfigureAwait(false);
        return lScoutHevc ? lScoutRefresh != false : lScoutRefresh == true;
    }
}
