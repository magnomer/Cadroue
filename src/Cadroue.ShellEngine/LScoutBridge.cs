using System.Diagnostics;
using System.Globalization;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

internal static class LScoutBridge
{
    internal static IReadOnlyList<LKeyframeEntry> LScoutBridgeRead(
        string lScoutSourcePath, TimeSpan lScoutOrigin, TimeSpan lScoutEnd, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutSourcePath) || !File.Exists(lScoutSourcePath) || lScoutEnd <= lScoutOrigin)
        {
            return Array.Empty<LKeyframeEntry>();
        }

        try
        {
            IReadOnlyList<LKeyframeEntry> lScoutKeyframes = LKeyframeSeeker.LKeyframeRangeScan(
                lScoutSourcePath, lScoutOrigin, lScoutEnd, lScoutToken);
            LBridgeStream? lScoutStream = LScoutStream.LScoutStreamRead(lScoutSourcePath, lScoutToken);
            if (lScoutStream is null)
            {
                return lScoutKeyframes;
            }

            // Only splice boundaries must be independent. Internal keyframes stay in
            // one continuous copied stream and inspecting every one adds seconds of
            // trace_headers work without changing decodability. Reject unsafe leading
            // candidates until a usable copy start is found. When the requested end
            // is not itself keyed, do the same backwards for the tail bridge start.
            bool lScoutHevc = lScoutStream.LBridgeCodec.ToLowerInvariant() is "hevc" or "h265";
            var lScoutCandidates = lScoutKeyframes.ToList();
            while (lScoutCandidates.Count > 0
                && !LScoutBoundaryCheck(
                    lScoutSourcePath,
                    lScoutStream.LBridgeCodec,
                    lScoutHevc,
                    lScoutCandidates[0].LKeyframePresentationTime,
                    lScoutToken))
            {
                lScoutCandidates.RemoveAt(0);
            }

            bool lScoutEndKeyed = lScoutCandidates.Count > 0
                && (lScoutCandidates[^1].LKeyframePresentationTime - lScoutEnd).Duration()
                    <= TimeSpan.FromMilliseconds(1);
            while (!lScoutEndKeyed
                && lScoutCandidates.Count > 1
                && !LScoutBoundaryCheck(
                    lScoutSourcePath,
                    lScoutStream.LBridgeCodec,
                    lScoutHevc,
                    lScoutCandidates[^1].LKeyframePresentationTime,
                    lScoutToken))
            {
                lScoutCandidates.RemoveAt(lScoutCandidates.Count - 1);
            }

            return lScoutCandidates;
        }
        catch (Exception lScoutException) when (lScoutException is not OperationCanceledException)
        {
            LRunner.LRunnerRecord($"Keyframes could not be read '{Path.GetFileName(lScoutSourcePath)}'", lScoutException);
            return Array.Empty<LKeyframeEntry>();
        }
    }

    private static bool LScoutBoundaryCheck(
        string lScoutSourcePath,
        string lScoutCodec,
        bool lScoutHevc,
        TimeSpan lScoutKeyframe,
        CancellationToken lScoutToken)
    {
        bool? lScoutRefresh = LScoutRefreshRead(lScoutSourcePath, lScoutCodec, lScoutKeyframe, lScoutToken);
        return lScoutHevc ? lScoutRefresh != false : lScoutRefresh == true;
    }

    internal static bool? LScoutRefreshRead(
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

        // Seek before the target so a reordered key packet is not lost because its
        // DTS precedes its PTS. FFmpeg may also expose the preceding key packet;
        // therefore inspect all packets in the short window and classify the last
        // key packet, which is the requested boundary.
        TimeSpan lScoutSeek = lScoutKeyframe > TimeSpan.FromSeconds(1)
            ? lScoutKeyframe - TimeSpan.FromSeconds(1)
            : TimeSpan.Zero;
        TimeSpan lScoutDuration = lScoutKeyframe - lScoutSeek + TimeSpan.FromMilliseconds(1);
        var lScoutStartInfo = new ProcessStartInfo(LTool.LToolFfmpegRead())
        {
            Arguments = FormattableString.Invariant(
                $"-hide_banner -loglevel info -ss {lScoutSeek.TotalSeconds:0.######} ")
                + $"-i {LEncode.LEncodeFormat(lScoutSourcePath)} "
                + FormattableString.Invariant($"-t {lScoutDuration.TotalSeconds:0.######} -map 0:v:0 ")
                + "-c:v copy -bsf:v trace_headers -f null -",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process? lScoutProcess = null;
        try
        {
            lScoutProcess = Process.Start(lScoutStartInfo);
            if (lScoutProcess is null)
            {
                return null;
            }

            LCustody.LCustodyAttach(lScoutProcess);
            using CancellationTokenRegistration lScoutKill = lScoutToken.Register(
                static p => { try { ((Process)p!).Kill(); } catch { } }, lScoutProcess);
            Task<string> lScoutOutput = lScoutProcess.StandardOutput.ReadToEndAsync();
            bool lScoutKeyPacket = false;
            bool? lScoutPacketIndependent = null;
            bool? lScoutLastIndependent = null;
            string? lScoutLine;
            while ((lScoutLine = lScoutProcess.StandardError.ReadLine()) is not null)
            {
                if (lScoutLine.Contains("] Packet:", StringComparison.Ordinal))
                {
                    if (lScoutKeyPacket && lScoutPacketIndependent is bool lScoutIndependent)
                    {
                        lScoutLastIndependent = lScoutIndependent;
                    }

                    lScoutKeyPacket = lScoutLine.Contains("key frame", StringComparison.Ordinal);
                    lScoutPacketIndependent = null;
                    continue;
                }

                if (!lScoutKeyPacket
                    || lScoutPacketIndependent is not null
                    || !LScoutNalRead(lScoutLine, out int lScoutNalType))
                {
                    continue;
                }

                bool lScoutVcl = lScoutH264
                    ? lScoutNalType is >= 1 and <= 5
                    : lScoutNalType is >= 0 and <= 31;
                if (!lScoutVcl)
                {
                    continue;
                }

                lScoutPacketIndependent = lScoutH264
                    ? lScoutNalType == 5
                    : lScoutNalType is >= 16 and <= 20;
            }

            if (lScoutKeyPacket && lScoutPacketIndependent is bool lScoutIndependentLast)
            {
                lScoutLastIndependent = lScoutIndependentLast;
            }

            lScoutProcess.WaitForExit();
            lScoutOutput.Wait(CancellationToken.None);
            lScoutToken.ThrowIfCancellationRequested();
            return lScoutProcess.ExitCode == 0 ? lScoutLastIndependent : null;
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
        finally
        {
            if (lScoutProcess is not null && !lScoutProcess.HasExited)
                try { lScoutProcess.Kill(); } catch { }
            lScoutProcess?.Dispose();
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
