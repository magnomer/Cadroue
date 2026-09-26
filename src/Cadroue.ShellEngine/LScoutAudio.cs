using System.Globalization;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

public sealed record LScoutAudioInterval(bool LScoutAudioPresent, TimeSpan LScoutAudioOffset);

internal static class LScoutAudio
{
    internal static async Task<bool?> LScoutAudioRead(
        string lScoutSourcePath,
        TimeSpan lScoutOrigin,
        TimeSpan lScoutEnd,
        CancellationToken lScoutToken = default) =>
        (await LScoutAudioResolve(
            lScoutSourcePath, lScoutOrigin, lScoutEnd, true, lScoutToken).ConfigureAwait(false))?.LScoutAudioPresent;

    internal static async Task<LScoutAudioInterval?> LScoutAudioResolve(
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
        string lScoutArguments =
            $"-v quiet -select_streams {(lScoutAllTracks ? "a" : "a:0")} -show_packets -show_format " +
            $"-read_intervals \"+{lScoutInterval}\" " +
            "-show_entries packet=pts_time,dts_time,duration_time:format=start_time -of csv " +
            $"-i {LEncode.LEncodeFormat(lScoutSourcePath)}";

        try
        {
            var lScoutPackets = new List<(double, double)>();
            double lScoutTimelineStart = 0;
            LEmployerResult lScoutResult = await new LEmployer(LTool.LToolFfprobeRead()).LEmployerRun(
                lScoutArguments,
                lScoutToken,
                _ => { },
                lScoutLine =>
                {
                    if (LScoutFormatRead(lScoutLine, out double lScoutFormatStart))
                    {
                        lScoutTimelineStart = lScoutFormatStart;
                    }
                    else if (LScoutPacketRead(
                        lScoutLine, out double lScoutPacketStart, out double lScoutPacketDuration))
                    {
                        lScoutPackets.Add((lScoutPacketStart, lScoutPacketDuration));
                    }
                },
                _ => { }).ConfigureAwait(false);
            lScoutToken.ThrowIfCancellationRequested();
            if (lScoutResult.LEmployerExit != 0)
            {
                return null;
            }

            double lScoutOriginAbsolute = lScoutTimelineStart + lScoutOrigin.TotalSeconds;
            double lScoutEndAbsolute = lScoutTimelineStart + lScoutEnd.TotalSeconds;
            double? lScoutFirstPacket = null;
            foreach (var (lScoutPacketStart, lScoutPacketDuration) in lScoutPackets)
            {
                if (lScoutPacketStart < lScoutEndAbsolute
                    && lScoutPacketStart + lScoutPacketDuration > lScoutOriginAbsolute
                    && (lScoutFirstPacket is null || lScoutPacketStart < lScoutFirstPacket))
                {
                    lScoutFirstPacket = lScoutPacketStart;
                }
            }

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
    }

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
            && double.TryParse(
                lScoutParts[3],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double lScoutParsedDuration)
                ? Math.Max(0, lScoutParsedDuration)
                : 0;
        return true;
    }

    private static bool LScoutFormatRead(string lScoutLine, out double lScoutStart)
    {
        lScoutStart = 0;
        string[] lScoutParts = lScoutLine.Split(',');
        return lScoutParts.Length >= 2
            && string.Equals(lScoutParts[0], "format", StringComparison.Ordinal)
            && double.TryParse(lScoutParts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lScoutStart);
    }
}
