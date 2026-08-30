using System.Diagnostics;
using System.Globalization;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

internal sealed record LScoutAudioInterval(bool LScoutAudioPresent, TimeSpan LScoutAudioOffset);

internal static class LScoutAudio
{
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
                if (LScoutFormatRead(lScoutLine, out double lScoutFormatStart))
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

    private static bool LScoutFormatRead(string lScoutLine, out double lScoutStart)
    {
        lScoutStart = 0;
        string[] lScoutParts = lScoutLine.Split(',');
        return lScoutParts.Length >= 2
            && string.Equals(lScoutParts[0], "format", StringComparison.Ordinal)
            && double.TryParse(lScoutParts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lScoutStart);
    }
}
