using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Cadroue.Media;

public static class LMediaLoudness
{
    public static async Task<double?> LMediaLoudnessRead(string sourcePath, CancellationToken lMediaToken = default)
    {
        lMediaToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return null;
        }

        string[] lMediaArguments =
        [
            "-hide_banner", "-nostats", "-i", sourcePath, "-map", "0:a:0", "-af", "ebur128", "-f", "null", "-"
        ];

        try
        {
            LMediaProcessResult lMediaResult = await LMediaProcess.LMediaProcessRun(
                LTool.LToolFfmpegRead(), lMediaArguments, lMediaToken).ConfigureAwait(false);
            return lMediaResult.LMediaProcessStalled ? null : LMediaLoudnessParse(lMediaResult.LMediaProcessError);
        }
        catch (Exception lMediaException) when (
            lMediaException is System.ComponentModel.Win32Exception
                or InvalidOperationException
                or IOException)
        {
            return null;
        }
    }

    private static double? LMediaLoudnessParse(string ffmpegText)
    {
        MatchCollection lMediaMatches = Regex.Matches(ffmpegText, @"I:\s*(-?\d+(?:\.\d+)?)\s*LUFS");
        if (lMediaMatches.Count == 0)
        {
            return null;
        }

        string lMediaValue = lMediaMatches[^1].Groups[1].Value;
        return double.TryParse(lMediaValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double lMediaLoudness)
            ? lMediaLoudness
            : null;
    }
}
