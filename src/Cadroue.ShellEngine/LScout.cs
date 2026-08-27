using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

internal sealed record LScoutAudioInterval(bool LScoutAudioPresent, TimeSpan LScoutAudioOffset);

internal static class LScout
{
    internal static double LScoutMergeRead(IReadOnlyList<string> lScoutMergeSources, CancellationToken lScoutToken = default)
    {
        double lScoutTotalSeconds = 0;
        foreach (string lScoutMergeSource in lScoutMergeSources)
        {
            lScoutTotalSeconds += LScoutMediaRead(lScoutMergeSource, lScoutToken)?.LWorkMediaDuration.TotalSeconds ?? 0;
        }

        return lScoutTotalSeconds;
    }

    internal static LWorkMedia? LScoutMediaRead(string lScoutMediaPath, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutMediaPath) || !File.Exists(lScoutMediaPath))
        {
            return null;
        }

        try
        {
            LMediaInfo lScoutMedia = LMedia.LMediaFfprobeRead(lScoutMediaPath, lScoutToken);
            return new LWorkMedia(
                lScoutMedia.LMediaVideoWidth,
                lScoutMedia.LMediaVideoHeight,
                lScoutMedia.LMediaVideoRate,
                (long)Math.Round(lScoutMedia.LMediaInfoDuration.TotalMilliseconds),
                lScoutMedia.LMediaVideoPresent)
            {
                LWorkMediaCodec = lScoutMedia.LMediaVideoCodec,
                LWorkAudioCodec = lScoutMedia.LMediaAudioCodec,
                LWorkMediaBitrate = lScoutMedia.LMediaAudioBitrate,
                LWorkMediaSamplerate = lScoutMedia.LMediaSampleRate,
                LWorkMediaPixel = lScoutMedia.LMediaVideoPixel,
                LWorkMediaRange = lScoutMedia.LMediaVideoRange
            };
        }
        catch (Exception lScoutException) when (lScoutException is not OperationCanceledException)
        {
            LRunner.LRunnerRecord($"Media could not be read '{Path.GetFileName(lScoutMediaPath)}'", lScoutException);
            return null;
        }
    }

    internal static double? LScoutIntervalRead(string lScoutMediaPath, TimeSpan lScoutMediaDuration, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutMediaPath) || !File.Exists(lScoutMediaPath) || lScoutMediaDuration <= TimeSpan.Zero)
        {
            return null;
        }

        try
        {
            IReadOnlyList<LKeyframeEntry> lScoutKeyframes = LKeyframeSeeker.LKeyframeRangeScan(
                lScoutMediaPath, TimeSpan.Zero, lScoutMediaDuration, lScoutToken);
            if (lScoutKeyframes.Count < 2)
            {
                return null;
            }

            double lScoutSpanMilliseconds =
                (lScoutKeyframes[^1].LKeyframePresentationTime - lScoutKeyframes[0].LKeyframePresentationTime).TotalMilliseconds;
            return lScoutSpanMilliseconds / (lScoutKeyframes.Count - 1);
        }
        catch (Exception lScoutException) when (lScoutException is not OperationCanceledException)
        {
            LRunner.LRunnerRecord($"Keyframe interval could not be read '{Path.GetFileName(lScoutMediaPath)}'", lScoutException);
            return null;
        }
    }

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
            LBridgeStream? lScoutStream = LScoutStreamRead(lScoutSourcePath, lScoutToken);
            if (lScoutStream is null)
            {
                return lScoutKeyframes;
            }

            // Only splice boundaries must be independent. Internal keyframes stay in
            // one continuous copied stream and inspecting every one adds seconds of
            // trace_headers work without changing decodability. Reject unsafe leading
            // candidates until a usable copy start is found. When the requested end
            // is not itself keyed, do the same backwards for the tail bridge start.
            // Probe uncertainty retains a candidate so it cannot silently turn Smart
            // into a full encode.
            var lScoutCandidates = lScoutKeyframes.ToList();
            while (lScoutCandidates.Count > 0
                && LScoutRefreshRead(
                    lScoutSourcePath,
                    lScoutStream.LBridgeCodec,
                    lScoutCandidates[0].LKeyframePresentationTime,
                    lScoutToken) == false)
            {
                lScoutCandidates.RemoveAt(0);
            }

            bool lScoutEndKeyed = lScoutCandidates.Count > 0
                && (lScoutCandidates[^1].LKeyframePresentationTime - lScoutEnd).Duration()
                    <= TimeSpan.FromMilliseconds(1);
            while (!lScoutEndKeyed
                && lScoutCandidates.Count > 1
                && LScoutRefreshRead(
                    lScoutSourcePath,
                    lScoutStream.LBridgeCodec,
                    lScoutCandidates[^1].LKeyframePresentationTime,
                    lScoutToken) == false)
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

    internal static bool? LScoutAudioRead(
        string lScoutSourcePath,
        TimeSpan lScoutOrigin,
        TimeSpan lScoutEnd,
        CancellationToken lScoutToken = default) =>
        LScoutAudioResolve(
            lScoutSourcePath, lScoutOrigin, lScoutEnd, true, lScoutToken)?.LScoutAudioPresent;

    internal static LScoutAudioInterval? LScoutAudioResolve(
        string lScoutSourcePath,
        TimeSpan lScoutOrigin,
        TimeSpan lScoutEnd,
        bool lScoutAllTracks,
        CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutSourcePath)
            || !File.Exists(lScoutSourcePath)
            || lScoutEnd <= lScoutOrigin)
        {
            return new LScoutAudioInterval(false, TimeSpan.Zero);
        }

        double lScoutDuration = (lScoutEnd - lScoutOrigin).TotalSeconds + 1;
        string lScoutInterval = FormattableString.Invariant(
            $"{lScoutOrigin.TotalSeconds:F6}%+{lScoutDuration:F6}");
        var lScoutStartInfo = new ProcessStartInfo(LTool.LToolFfprobeRead())
        {
            Arguments = $"-v quiet -select_streams {(lScoutAllTracks ? "a" : "a:0")} -show_packets -show_format -read_intervals \"+{lScoutInterval}\" "
                + $"-show_entries packet=pts_time,dts_time,duration_time:format=start_time -of csv -i {LEncode.LEncodeFormat(lScoutSourcePath)}",
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
            Task<string> lScoutError = lScoutProcess.StandardError.ReadToEndAsync();

            var lScoutPackets = new List<(double Start, double Duration)>();
            double lScoutTimelineStart = 0;
            string? lScoutLine;
            while ((lScoutLine = lScoutProcess.StandardOutput.ReadLine()) is not null)
            {
                if (LScoutFormatStartRead(lScoutLine, out double lScoutFormatStart))
                {
                    lScoutTimelineStart = lScoutFormatStart;
                }
                else if (LScoutPacketRead(lScoutLine, out double lScoutPacketStart, out double lScoutPacketDuration))
                {
                    lScoutPackets.Add((lScoutPacketStart, lScoutPacketDuration));
                }
            }

            lScoutProcess.WaitForExit();
            lScoutError.Wait(CancellationToken.None);
            lScoutToken.ThrowIfCancellationRequested();
            if (lScoutProcess.ExitCode != 0)
            {
                return null;
            }

            double lScoutOriginAbsolute = lScoutTimelineStart + lScoutOrigin.TotalSeconds;
            double lScoutEndAbsolute = lScoutTimelineStart + lScoutEnd.TotalSeconds;
            double? lScoutFirstPacket = lScoutPackets
                .Where(lScoutPacket => lScoutPacket.Start < lScoutEndAbsolute
                    && lScoutPacket.Start + lScoutPacket.Duration > lScoutOriginAbsolute)
                .Select(lScoutPacket => (double?)lScoutPacket.Start)
                .Min();

            return lScoutFirstPacket is double lScoutFirst
                ? new LScoutAudioInterval(
                    true,
                    TimeSpan.FromSeconds(Math.Max(0, lScoutFirst - lScoutOriginAbsolute)))
                : new LScoutAudioInterval(false, TimeSpan.Zero);
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

    internal static long? LScoutInputRead(LWorkItem lScoutWorkItem, CancellationToken lScoutToken = default)
    {
        if (lScoutWorkItem.LWorkMergeSources.Count > 1)
        {
            long lScoutMergeTotal = 0;
            foreach (string lScoutMergeSource in lScoutWorkItem.LWorkMergeSources)
            {
                if (LScoutBytesRead(lScoutMergeSource) is not { } lScoutMergeBytes)
                {
                    lScoutMergeTotal = 0;
                    break;
                }

                lScoutMergeTotal += lScoutMergeBytes;
            }

            if (lScoutMergeTotal > 0)
            {
                return lScoutMergeTotal;
            }
        }

        return lScoutWorkItem.LWorkSourceBytes ?? LScoutBytesRead(lScoutWorkItem.LWorkSourcePath);
    }

    internal static long? LScoutBytesRead(string lScoutOutputPath)
    {
        if (string.IsNullOrWhiteSpace(lScoutOutputPath))
        {
            return null;
        }

        try
        {
            var lScoutOutputFile = new FileInfo(lScoutOutputPath);
            return lScoutOutputFile.Exists ? lScoutOutputFile.Length : null;
        }
        catch (Exception lScoutException) when (lScoutException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    internal static LBridgeStream? LScoutStreamRead(string lScoutMediaPath, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutMediaPath) || !File.Exists(lScoutMediaPath))
        {
            return null;
        }

        ProcessStartInfo lScoutStartInfo = LScoutStreamStart(lScoutMediaPath);

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

            Task<string> lScoutError = lScoutProcess.StandardError.ReadToEndAsync();
            string lScoutJson = lScoutProcess.StandardOutput.ReadToEnd();
            lScoutProcess.WaitForExit();
            lScoutError.Wait(CancellationToken.None);
            lScoutToken.ThrowIfCancellationRequested();
            return LScoutStreamParse(lScoutJson);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception lScoutException) when (lScoutException is not OperationCanceledException)
        {
            LRunner.LRunnerRecord($"Stream properties could not be read '{Path.GetFileName(lScoutMediaPath)}'", lScoutException);
            return null;
        }
        finally
        {
            if (lScoutProcess is not null && !lScoutProcess.HasExited)
                try { lScoutProcess.Kill(); } catch { }
            lScoutProcess?.Dispose();
        }
    }

    internal static ProcessStartInfo LScoutStreamStart(string lScoutMediaPath)
    {
        var lScoutStartInfo = new ProcessStartInfo(LTool.LToolFfprobeRead())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        lScoutStartInfo.ArgumentList.Add("-v");
        lScoutStartInfo.ArgumentList.Add("quiet");
        lScoutStartInfo.ArgumentList.Add("-select_streams");
        lScoutStartInfo.ArgumentList.Add("v:0");
        lScoutStartInfo.ArgumentList.Add("-show_entries");
        lScoutStartInfo.ArgumentList.Add(
            "stream=codec_name,profile,pix_fmt,color_space,color_primaries,color_transfer,color_range,r_frame_rate,bit_rate,time_base");
        lScoutStartInfo.ArgumentList.Add("-print_format");
        lScoutStartInfo.ArgumentList.Add("json");
        lScoutStartInfo.ArgumentList.Add("-i");
        lScoutStartInfo.ArgumentList.Add(lScoutMediaPath);
        return lScoutStartInfo;
    }

    private static LBridgeStream? LScoutStreamParse(string lScoutJson)
    {
        if (string.IsNullOrWhiteSpace(lScoutJson))
        {
            return null;
        }

        using JsonDocument lScoutDocument = JsonDocument.Parse(lScoutJson);
        if (!lScoutDocument.RootElement.TryGetProperty("streams", out JsonElement lScoutStreams)
            || lScoutStreams.ValueKind != JsonValueKind.Array
            || lScoutStreams.GetArrayLength() == 0)
        {
            return null;
        }

        JsonElement lScoutStream = lScoutStreams[0];

        return new LBridgeStream(
            LScoutTextRead(lScoutStream, "codec_name"),
            LScoutTextRead(lScoutStream, "profile"),
            LScoutTextRead(lScoutStream, "pix_fmt"),
            LScoutTextRead(lScoutStream, "color_space"),
            LScoutTextRead(lScoutStream, "color_primaries"),
            LScoutTextRead(lScoutStream, "color_transfer"),
            LScoutTextRead(lScoutStream, "color_range"),
            LScoutTextRead(lScoutStream, "r_frame_rate"),
            LScoutLongRead(lScoutStream, "bit_rate"),
            LScoutTextRead(lScoutStream, "time_base"));
    }

    private static string LScoutTextRead(JsonElement lScoutElement, string lScoutName) =>
        lScoutElement.TryGetProperty(lScoutName, out JsonElement lScoutValue) && lScoutValue.ValueKind == JsonValueKind.String
            ? lScoutValue.GetString() ?? string.Empty
            : string.Empty;

    private static bool LScoutPacketRead(
        string lScoutLine,
        out double lScoutStart,
        out double lScoutDuration)
    {
        lScoutStart = 0;
        lScoutDuration = 0;
        string[] lScoutParts = lScoutLine.Split(',');
        if (lScoutParts.Length < 3
            || !string.Equals(lScoutParts[0], "packet", StringComparison.Ordinal))
        {
            return false;
        }

        bool lScoutPts = double.TryParse(
            lScoutParts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lScoutPtsSeconds);
        bool lScoutDts = double.TryParse(
            lScoutParts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double lScoutDtsSeconds);
        if (!lScoutPts && !lScoutDts)
        {
            return false;
        }

        lScoutStart = lScoutPts ? lScoutPtsSeconds : lScoutDtsSeconds;
        lScoutDuration = lScoutParts.Length > 3
            && double.TryParse(lScoutParts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double lScoutParsedDuration)
                ? Math.Max(0, lScoutParsedDuration)
                : 0;
        return true;
    }

    private static bool LScoutFormatStartRead(string lScoutLine, out double lScoutStart)
    {
        lScoutStart = 0;
        string[] lScoutParts = lScoutLine.Split(',');
        return lScoutParts.Length >= 2
            && string.Equals(lScoutParts[0], "format", StringComparison.Ordinal)
            && double.TryParse(lScoutParts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lScoutStart);
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

    private static long LScoutLongRead(JsonElement lScoutElement, string lScoutName)
    {
        if (!lScoutElement.TryGetProperty(lScoutName, out JsonElement lScoutValue))
        {
            return 0;
        }

        if (lScoutValue.ValueKind == JsonValueKind.Number && lScoutValue.TryGetInt64(out long lScoutInteger))
        {
            return lScoutInteger;
        }

        return lScoutValue.ValueKind == JsonValueKind.String
            && long.TryParse(lScoutValue.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long lScoutParsed)
            ? lScoutParsed
            : 0;
    }
}
