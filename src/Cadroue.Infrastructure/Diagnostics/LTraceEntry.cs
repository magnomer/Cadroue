using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Cadroue.Infrastructure;

public sealed partial record LTraceEntry(
    string LTraceEntryTime,
    string LTraceEntryDelta,
    LTraceKind LTraceEntryKind,
    string LTraceEntrySummary,
    string? LTraceEntryDetail,
    double? LTraceEntrySpan)
{
    internal const int LTraceIndentWidth = 34;

    private const int LTraceDeltaWidth = 7;
    private const int LTraceKindWidth = 11;

    public bool LTraceEntryDetailed => !string.IsNullOrWhiteSpace(LTraceEntryDetail);

    public static string LTraceKindRead(LTraceKind lTraceKind) => lTraceKind switch
    {
        LTraceKind.LTraceLoading => "Loading",
        LTraceKind.LTraceWarning => "Warning",
        LTraceKind.LTraceError => "Error",
        LTraceKind.LTraceInteraction => "Interaction",
        LTraceKind.LTraceUi => "UI",
        LTraceKind.LTraceWork => "Work",
        LTraceKind.LTraceFfmpeg => "Ffmpeg",
        _ => "Info"
    };

    public static LTraceKind LTraceKindFind(string lTraceText) => lTraceText switch
    {
        "Loading" => LTraceKind.LTraceLoading,
        "Warning" => LTraceKind.LTraceWarning,
        "Error" => LTraceKind.LTraceError,
        "Interaction" => LTraceKind.LTraceInteraction,
        "UI" => LTraceKind.LTraceUi,
        "Draw" => LTraceKind.LTraceUi,
        "View" => LTraceKind.LTraceUi,
        "Work" => LTraceKind.LTraceWork,
        "Ffmpeg" => LTraceKind.LTraceFfmpeg,
        _ => LTraceKind.LTraceInfo
    };

    public static string LTraceEntryFormat(LTraceEntry lTraceEntry)
    {
        var lTraceBuilder = new StringBuilder();
        lTraceBuilder.Append(lTraceEntry.LTraceEntryTime);
        lTraceBuilder.Append("  ");
        lTraceBuilder.Append(lTraceEntry.LTraceEntryDelta.PadRight(LTraceDeltaWidth));
        lTraceBuilder.Append(' ');
        lTraceBuilder.Append(LTraceKindRead(lTraceEntry.LTraceEntryKind).PadRight(LTraceKindWidth));
        lTraceBuilder.Append("  ");
        lTraceBuilder.Append(lTraceEntry.LTraceEntrySummary);
        if (lTraceEntry.LTraceEntrySpan is double lTraceSpan)
        {
            lTraceBuilder.Append(" — ");
            lTraceBuilder.Append(LTraceSpanFormat(lTraceSpan));
        }

        lTraceBuilder.Append(Environment.NewLine);
        LTraceDetailAppend(lTraceBuilder, lTraceEntry.LTraceEntryDetail);
        return lTraceBuilder.ToString();
    }

    public static string LTraceSpanFormat(double lTraceMilliseconds) =>
        lTraceMilliseconds >= 1000
            ? string.Create(CultureInfo.InvariantCulture, $"{lTraceMilliseconds / 1000:0.00}s")
            : string.Create(CultureInfo.InvariantCulture, $"{lTraceMilliseconds:0.0}ms");

    public static List<LTraceEntry> LTraceEntryParse(string lTraceText)
    {
        var lTraceEntries = new List<LTraceEntry>();
        if (string.IsNullOrEmpty(lTraceText))
        {
            return lTraceEntries;
        }

        StringBuilder? lTraceDetail = null;
        LTraceEntry? lTraceOpen = null;

        foreach (string lTraceRaw in lTraceText.Split('\n'))
        {
            string lTraceLine = lTraceRaw.TrimEnd('\r');
            if (lTraceLine.Length == 0)
            {
                continue;
            }

            Match lTraceMatch = LTraceHeaderRead().Match(lTraceLine);
            if (!lTraceMatch.Success)
            {
                if (lTraceOpen is not null)
                {
                    lTraceDetail ??= new StringBuilder();
                    if (lTraceDetail.Length > 0)
                    {
                        lTraceDetail.Append('\n');
                    }

                    lTraceDetail.Append(lTraceLine.Trim());
                }

                continue;
            }

            LTraceEntryClose(lTraceEntries, lTraceOpen, lTraceDetail);
            lTraceDetail = null;

            (string lTraceSummary, double? lTraceSpan) = LTraceSummaryDivide(lTraceMatch.Groups[4].Value);
            lTraceOpen = new LTraceEntry(
                lTraceMatch.Groups[1].Value,
                lTraceMatch.Groups[2].Value,
                LTraceKindFind(lTraceMatch.Groups[3].Value),
                lTraceSummary,
                null,
                lTraceSpan);
        }

        LTraceEntryClose(lTraceEntries, lTraceOpen, lTraceDetail);
        return lTraceEntries;
    }

    private static void LTraceEntryClose(
        List<LTraceEntry> lTraceEntries,
        LTraceEntry? lTraceOpen,
        StringBuilder? lTraceDetail)
    {
        if (lTraceOpen is null)
        {
            return;
        }

        lTraceEntries.Add(lTraceDetail is null || lTraceDetail.Length == 0
            ? lTraceOpen
            : lTraceOpen with { LTraceEntryDetail = lTraceDetail.ToString() });
    }

    private static (string Summary, double? Span) LTraceSummaryDivide(string lTraceTail)
    {
        int lTraceMark = lTraceTail.LastIndexOf(" — ", StringComparison.Ordinal);
        if (lTraceMark < 0)
        {
            return (lTraceTail, null);
        }

        Match lTraceSpanMatch = LTraceSpanRead().Match(lTraceTail[(lTraceMark + 3)..]);
        if (!lTraceSpanMatch.Success)
        {
            return (lTraceTail, null);
        }

        double lTraceValue = double.Parse(lTraceSpanMatch.Groups[1].Value, CultureInfo.InvariantCulture);
        return (
            lTraceTail[..lTraceMark],
            lTraceSpanMatch.Groups[2].Value == "s" ? lTraceValue * 1000 : lTraceValue);
    }

    private static void LTraceDetailAppend(StringBuilder lTraceBuilder, string? lTraceDetail)
    {
        if (string.IsNullOrWhiteSpace(lTraceDetail))
        {
            return;
        }

        foreach (string lTraceLine in lTraceDetail.Split('\n'))
        {
            string lTraceTrimmed = lTraceLine.TrimEnd('\r');
            if (lTraceTrimmed.Length == 0)
            {
                continue;
            }

            lTraceBuilder.Append(' ', LTraceIndentWidth);
            lTraceBuilder.Append(lTraceTrimmed);
            lTraceBuilder.Append(Environment.NewLine);
        }
    }

    [GeneratedRegex(@"^(\d{2}:\d{2}:\d{2}\.\d{3})\s+(\S+)\s+(Info|Loading|Warning|Error|Interaction|UI|Draw|View|Work|Ffmpeg)\s+(.*)$")]
    private static partial Regex LTraceHeaderRead();

    [GeneratedRegex(@"^(\d+(?:\.\d+)?)(ms|s)$")]
    private static partial Regex LTraceSpanRead();
}
