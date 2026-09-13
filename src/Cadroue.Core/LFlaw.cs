using System.Globalization;
using System.Text.RegularExpressions;

namespace Cadroue.Core;

public enum LFlawKind
{
    LFlawKindContainer,
    LFlawKindTruncation,
    LFlawKindTransport,
    LFlawKindMetadata,
    LFlawKindIndex,
    LFlawKindFraming,
    LFlawKindConfig,
    LFlawKindTiming,
    LFlawKindSecondary,
    LFlawKindCoded,
    LFlawKindFfvone
}

internal static class LFlaw
{
    internal static readonly string[] lFlawFramingFault =
    {
        "invalid nal unit size", "nal unit size", "annexb", "annex b", "mp4toannexb",
        "error splitting the input into nal units", "missing picture in access unit"
    };

    internal static readonly string[] lFlawConfigFault =
    {
        "non-existing pps", "non-existing sps", "non-existing vps",
        "sps unavailable", "pps unavailable", "vps unavailable",
        "missing sps", "missing pps", "no frame!", "could not find codec parameters"
    };

    internal static readonly string[] lFlawFramingDamage =
    {
        "error while decoding", "concealing", "corrupt", "damaged", "decode_slice",
        "invalid data found", "out of range", "slice below image", "slice mismatch",
        "slice end mismatch", "mb incr damaged", "ac-tex damaged"
    };

    internal static readonly string[] lFlawTransportFault =
    {
        "continuity", "pes packet", "pes header", "sync byte", "pcr",
        "invalid packet size", "program map", "program association", "invalid ts packet"
    };

    internal static readonly string[] lFlawTruncationTail =
    {
        "truncated", "invalid data found", "premature end", "unexpected end",
        "truncating", "partial file"
    };

    internal static readonly string[] lFlawTruncationFinal =
    {
        "moov atom not found"
    };

    private static readonly Regex lFlawOffset = new(
        @"(?:pos|offset|at)[:=]?\s*(\d{2,})", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal static string LFlawScopeResolve(string lFlawEvidence)
    {
        Match lFlawMatch = lFlawOffset.Match(lFlawEvidence);
        return lFlawMatch.Success
            && long.TryParse(
                lFlawMatch.Groups[1].Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out long lFlawByte)
                ? FormattableString.Invariant($"Byte offset {lFlawByte}")
                : "Container structure";
    }

    internal static IReadOnlyList<IReadOnlyDictionary<string, string>> LFlawSectionRead(
        string lFlawReport, string lFlawSection)
    {
        var lFlawSections = new List<IReadOnlyDictionary<string, string>>();
        Dictionary<string, string>? lFlawCurrent = null;
        foreach (string lFlawRaw in lFlawReport.Split('\n'))
        {
            string lFlawLine = lFlawRaw.Trim();
            if (lFlawLine.Equals($"[{lFlawSection}]", StringComparison.Ordinal))
            {
                lFlawCurrent = new Dictionary<string, string>(StringComparer.Ordinal);
            }
            else if (lFlawLine.Equals($"[/{lFlawSection}]", StringComparison.Ordinal))
            {
                if (lFlawCurrent is not null)
                {
                    lFlawSections.Add(lFlawCurrent);
                    lFlawCurrent = null;
                }
            }
            else if (lFlawCurrent is not null)
            {
                int lFlawEquals = lFlawLine.IndexOf('=', StringComparison.Ordinal);
                if (lFlawEquals > 0)
                {
                    lFlawCurrent[lFlawLine[..lFlawEquals]] = lFlawLine[(lFlawEquals + 1)..];
                }
            }
        }

        return lFlawSections;
    }
}
