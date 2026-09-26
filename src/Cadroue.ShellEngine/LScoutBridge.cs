using System.Globalization;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

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

            IReadOnlyList<LKeyframeEntry> lScoutKeyframes = await LKeyframeSeeker.LKeyframeRangeScan(
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
            LRunner.LRunnerRecord(
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
        bool? lScoutRefresh = await LScoutRefreshRead(lScoutSourcePath, lScoutCodec, lScoutKeyframe, lScoutToken)
            .ConfigureAwait(false);
        return lScoutHevc ? lScoutRefresh != false : lScoutRefresh == true;
    }

    internal static async Task<bool?> LScoutRefreshRead(
        string lScoutSourcePath,
        string lScoutCodec,
        TimeSpan lScoutKeyframe,
        CancellationToken lScoutToken = default)
    {
        string lScoutNormalizedCodec = lScoutCodec.ToLowerInvariant();
        bool lScoutH264 = lScoutNormalizedCodec is "h264" or "avc";
        bool lScoutHevc = lScoutNormalizedCodec is "hevc" or "h265";
        if (!lScoutH264 && !lScoutHevc)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(lScoutSourcePath) || !File.Exists(lScoutSourcePath))
        {
            return null;
        }

        TimeSpan lScoutSeek = lScoutKeyframe > TimeSpan.FromSeconds(1)
            ? lScoutKeyframe - TimeSpan.FromSeconds(1)
            : TimeSpan.Zero;
        TimeSpan lScoutDuration = lScoutKeyframe - lScoutSeek + TimeSpan.FromMilliseconds(1);
        string lScoutArguments = FormattableString.Invariant(
            $"-hide_banner -loglevel info -ss {lScoutSeek.TotalSeconds:0.######} ")
            + $"-i {LEncode.LEncodeFormat(lScoutSourcePath)} "
            + FormattableString.Invariant($"-t {lScoutDuration.TotalSeconds:0.######} -map 0:v:0 ")
            + "-c:v copy -bsf:v trace_headers -f null -";

        try
        {
            bool lScoutKeyPacket = false;
            bool? lScoutPacketIndependent = null;
            bool? lScoutLastIndependent = null;
            LEmployerResult lScoutResult = await new LEmployer(LTool.LToolFfmpegRead()).LEmployerRun(
                lScoutArguments,
                lScoutToken,
                _ => { },
                _ => { },
                lScoutLine =>
                {
                    if (lScoutLine.Contains("] Packet:", StringComparison.Ordinal))
                    {
                        if (lScoutKeyPacket && lScoutPacketIndependent is bool lScoutIndependent)
                        {
                            lScoutLastIndependent = lScoutIndependent;
                        }

                        lScoutKeyPacket = lScoutLine.Contains("key frame", StringComparison.Ordinal);
                        lScoutPacketIndependent = null;
                        return;
                    }

                    if (!lScoutKeyPacket
                        || lScoutPacketIndependent is not null
                        || !LScoutNalRead(lScoutLine, out int lScoutNalType))
                    {
                        return;
                    }

                    bool lScoutVcl = lScoutH264
                        ? lScoutNalType is >= 1 and <= 5
                        : lScoutNalType is >= 0 and <= 31;
                    if (lScoutVcl)
                    {
                        lScoutPacketIndependent = lScoutH264
                            ? lScoutNalType == 5
                            : lScoutNalType is >= 16 and <= 20;
                    }
                }).ConfigureAwait(false);

            if (lScoutKeyPacket && lScoutPacketIndependent is bool lScoutIndependentLast)
            {
                lScoutLastIndependent = lScoutIndependentLast;
            }

            lScoutToken.ThrowIfCancellationRequested();
            return lScoutResult.LEmployerExit == 0 ? lScoutLastIndependent : null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            lScoutToken.ThrowIfCancellationRequested();
            return null;
        }
    }

    private static bool LScoutNalRead(string lScoutLine, out int lScoutNalType)
    {
        lScoutNalType = 0;
        if (!lScoutLine.Contains("nal_unit_type", StringComparison.Ordinal))
        {
            return false;
        }

        int lScoutEquals = lScoutLine.LastIndexOf('=');
        return lScoutEquals >= 0
            && int.TryParse(
                lScoutLine[(lScoutEquals + 1)..].Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out lScoutNalType);
    }
}
