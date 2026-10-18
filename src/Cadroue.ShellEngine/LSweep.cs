using System.Globalization;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LSweep
{
    public static string LSweepArgsFormat(string lSweepSource, LDetectorBlank lSweepBlank)
    {
        LDetectorBlank lSweepSpec = LDetectorBlank.LDetectorBlankClamp(lSweepBlank);
        string lSweepFilter = lSweepSpec.LDetectorBlankType == LDetectorType.LDetectorTypeColor
            ? LSweepColorFormat(lSweepSpec)
            : LSweepBlackFormat(lSweepSpec);
        return $"-hide_banner -stats -i {LEncode.LEncodeFormat(lSweepSource)} " +
            $"-map 0:v:0 -vf {LEncode.LEncodeFormat(lSweepFilter)} -an -f null -";
    }

    private static string LSweepBlackFormat(LDetectorBlank lSweepSpec) =>
        string.Create(CultureInfo.InvariantCulture,
            $"blackdetect=d={lSweepSpec.LDetectorBlankMinimum:0.###}:" +
            $"pic_th={lSweepSpec.LDetectorBlankCoverage:0.###}:" +
            $"pix_th={lSweepSpec.LDetectorBlankTolerance:0.###}");

    private static string LSweepColorFormat(LDetectorBlank lSweepSpec)
    {
        (int lSweepRed, int lSweepGreen, int lSweepBlue) = LNeutralColor.LNeutralRgbResolve(
            lSweepSpec.LDetectorBlankHue,
            lSweepSpec.LDetectorBlankSaturation,
            Math.Max(lSweepSpec.LDetectorBlankBrightness, 0.001));
        int lSweepThreshold = (int)Math.Round(lSweepSpec.LDetectorBlankTolerance * 255);
        string lSweepMatch = string.Create(CultureInfo.InvariantCulture,
            $"lt(abs(r(X,Y)-{lSweepRed}),{lSweepThreshold})*" +
            $"lt(abs(g(X,Y)-{lSweepGreen}),{lSweepThreshold})*" +
            $"lt(abs(b(X,Y)-{lSweepBlue}),{lSweepThreshold})");
        string lSweepExpr = $"if({lSweepMatch},0,255)";
        return string.Create(CultureInfo.InvariantCulture,
            $"format=gbrp,geq=r='{lSweepExpr}':g='{lSweepExpr}':b='{lSweepExpr}',format=gray," +
            $"blackdetect=d={lSweepSpec.LDetectorBlankMinimum:0.###}:" +
            $"pic_th={lSweepSpec.LDetectorBlankCoverage:0.###}:pix_th=0.1");
    }

    public static IReadOnlyList<LSweepSpan> LSweepOutputParse(IEnumerable<string> lSweepLines)
    {
        var lSweepIntervals = new List<LSweepSpan>();
        foreach (string lSweepLine in lSweepLines)
        {
            if (lSweepLine is null || !lSweepLine.Contains("black_start:", StringComparison.Ordinal))
            {
                continue;
            }

            double? lSweepStart = LSweepField.LSweepFieldRead(lSweepLine, "black_start:");
            double? lSweepEnd = LSweepField.LSweepFieldRead(lSweepLine, "black_end:");
            if (lSweepStart is { } lSweepFrom && lSweepEnd is { } lSweepTo && lSweepTo > lSweepFrom)
            {
                lSweepIntervals.Add(new LSweepSpan(TimeSpan.FromSeconds(lSweepFrom), TimeSpan.FromSeconds(lSweepTo)));
            }
        }

        return lSweepIntervals;
    }

    public static async Task<IReadOnlyList<LSweepSpan>> LSweepScan(
        string lSweepSource,
        LDetectorBlank lSweepBlank,
        TimeSpan lSweepDuration,
        CancellationToken lSweepToken,
        IProgress<double>? lSweepProgress = null)
    {
        if (string.IsNullOrWhiteSpace(lSweepSource))
        {
            return Array.Empty<LSweepSpan>();
        }

        return LSweepOutputParse(
            await LSweepProcess.LSweepLinesRead(
                LSweepArgsFormat(lSweepSource, lSweepBlank),
                lSweepDuration,
                lSweepToken,
                lSweepProgress)
                .ConfigureAwait(false));
    }
}
