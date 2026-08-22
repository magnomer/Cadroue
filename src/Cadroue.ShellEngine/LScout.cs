using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

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

    internal static IReadOnlyList<TimeSpan> LScoutBridgeRead(
        string lScoutSourcePath, TimeSpan lScoutOrigin, TimeSpan lScoutEnd, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutSourcePath) || !File.Exists(lScoutSourcePath) || lScoutEnd <= lScoutOrigin)
        {
            return Array.Empty<TimeSpan>();
        }

        try
        {
            IReadOnlyList<LKeyframeEntry> lScoutKeyframes = LKeyframeSeeker.LKeyframeRangeScan(
                lScoutSourcePath, lScoutOrigin, lScoutEnd, lScoutToken);
            return lScoutKeyframes
                .Select(lScoutEntry => lScoutEntry.LKeyframePresentationTime)
                .ToArray();
        }
        catch (Exception lScoutException) when (lScoutException is not OperationCanceledException)
        {
            LRunner.LRunnerRecord($"Keyframes could not be read '{Path.GetFileName(lScoutSourcePath)}'", lScoutException);
            return Array.Empty<TimeSpan>();
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

        var lScoutStartInfo = new ProcessStartInfo(LTool.LToolFfprobeRead())
        {
            Arguments = $"-v quiet -select_streams v:0 -show_streams -show_format -print_format json -i {LEncode.LEncodeFormat(lScoutMediaPath)}",
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

            string lScoutJson = lScoutProcess.StandardOutput.ReadToEnd();
            lScoutProcess.WaitForExit();
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
        string lScoutContainer = lScoutDocument.RootElement.TryGetProperty("format", out JsonElement lScoutFormat)
            ? LScoutTextRead(lScoutFormat, "format_name")
            : string.Empty;

        return new LBridgeStream(
            LScoutTextRead(lScoutStream, "codec_name"),
            LScoutTextRead(lScoutStream, "profile"),
            LScoutNumberRead(lScoutStream, "level"),
            LScoutNumberRead(lScoutStream, "width"),
            LScoutNumberRead(lScoutStream, "height"),
            LScoutTextRead(lScoutStream, "pix_fmt"),
            LScoutTextRead(lScoutStream, "r_frame_rate"),
            LScoutTextRead(lScoutStream, "time_base"),
            LScoutTextRead(lScoutStream, "sample_aspect_ratio"),
            LScoutTextRead(lScoutStream, "display_aspect_ratio"),
            LScoutTextRead(lScoutStream, "field_order"),
            LScoutTextRead(lScoutStream, "color_primaries"),
            LScoutTextRead(lScoutStream, "color_transfer"),
            LScoutTextRead(lScoutStream, "color_space"),
            LScoutTextRead(lScoutStream, "color_range"),
            Array.Empty<byte>(),
            lScoutContainer);
    }

    private static string LScoutTextRead(JsonElement lScoutElement, string lScoutName) =>
        lScoutElement.TryGetProperty(lScoutName, out JsonElement lScoutValue) && lScoutValue.ValueKind == JsonValueKind.String
            ? lScoutValue.GetString() ?? string.Empty
            : string.Empty;

    private static int LScoutNumberRead(JsonElement lScoutElement, string lScoutName)
    {
        if (!lScoutElement.TryGetProperty(lScoutName, out JsonElement lScoutValue))
        {
            return 0;
        }

        if (lScoutValue.ValueKind == JsonValueKind.Number && lScoutValue.TryGetInt32(out int lScoutInteger))
        {
            return lScoutInteger;
        }

        return lScoutValue.ValueKind == JsonValueKind.String
            && int.TryParse(lScoutValue.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lScoutParsed)
            ? lScoutParsed
            : 0;
    }
}
