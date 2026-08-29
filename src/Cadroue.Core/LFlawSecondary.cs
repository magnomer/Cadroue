using System.Globalization;

namespace Cadroue.Core;

public static class LFlawSecondary
{
    private static readonly string[] lFlawSecondaryKinds =
    {
        "subtitle", "attachment", "data"
    };

    // A secondary-only copy over an input carrying no subtitle/data output stream
    // makes the null muxer complain; that is not a defect in a secondary object.
    private static readonly string[] lFlawSecondaryBenign =
    {
        "does not contain any stream", "at least one output file must be specified",
        "no such file"
    };

    public static LDossier? LFlawSecondaryResolve(
        string lFlawStreamReport, string lFlawChapterReport, string lFlawSecondaryError)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams =
            LFlaw.LFlawSectionRead(lFlawStreamReport, "STREAM");
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawSecondaryStreams = lFlawStreams
            .Where(lFlawStream => lFlawStream.TryGetValue("codec_type", out string? lFlawType)
                && lFlawSecondaryKinds.Contains(lFlawType.ToLowerInvariant()))
            .ToList();
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawChapters =
            LFlaw.LFlawSectionRead(lFlawChapterReport, "CHAPTER");

        // Principal A/V never reaches this diagnosis: with no secondary stream and no
        // chapters there is no secondary object to be malformed.
        if (lFlawSecondaryStreams.Count == 0 && lFlawChapters.Count == 0)
        {
            return null;
        }

        var lFlawFindings = new List<string>();

        string lFlawCarriage = LFlawCarriageRead(lFlawSecondaryError);
        if (lFlawSecondaryStreams.Count > 0 && lFlawCarriage.Length > 0)
        {
            lFlawFindings.Add($"Malformed secondary stream carriage: {lFlawCarriage}");
        }

        foreach (IReadOnlyDictionary<string, string> lFlawAttachment in lFlawSecondaryStreams.Where(
            lFlawStream => lFlawStream.TryGetValue("codec_type", out string? lFlawType)
                && string.Equals(lFlawType, "attachment", StringComparison.OrdinalIgnoreCase)))
        {
            if (!lFlawAttachment.Keys.Any(lFlawKey =>
                lFlawKey.EndsWith(":filename", StringComparison.OrdinalIgnoreCase)))
            {
                lFlawFindings.Add("Attachment stream missing its filename declaration");
                break;
            }
        }

        if (LFlawChaptersCheck(lFlawChapters))
        {
            lFlawFindings.Add("Malformed chapter timing: overlapping or reversed entries");
        }

        if (lFlawFindings.Count == 0)
        {
            return null;
        }

        return new LDossier(
            "Secondary data",
            1.0,
            "ffprobe -show_streams -show_chapters; secondary-only -map 0:s? -map 0:d? -c copy -f null",
            string.Join(" | ", lFlawFindings.Distinct(StringComparer.Ordinal).Take(3)),
            "Each secondary stream and chapter object inspected independently of principal A/V",
            "Secondary subtitle, chapter, tag and attachment objects",
            "Remux -map 0 -c copy rewriting the malformed secondary object; principal streams stay packet-exact",
            "The malformed secondary object only",
            LDossierPreservation.LDossierPreservationPacket,
            "Principal coded packets copied unchanged; malformed secondary object rewritten",
            "Preserved",
            "None",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategorySecondary);
    }

    private static string LFlawCarriageRead(string lFlawSecondaryError)
    {
        IEnumerable<string> lFlawLines = lFlawSecondaryError
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(lFlawLine => lFlawLine.Length > 0)
            .Where(lFlawLine => !lFlawSecondaryBenign.Any(
                lFlawTerm => lFlawLine.Contains(lFlawTerm, StringComparison.OrdinalIgnoreCase)));

        return string.Join(" | ", lFlawLines.Distinct(StringComparer.Ordinal).Take(3));
    }

    private static bool LFlawChaptersCheck(IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawChapters)
    {
        double lFlawPreviousEnd = double.NegativeInfinity;
        foreach (IReadOnlyDictionary<string, string> lFlawChapter in lFlawChapters)
        {
            double? lFlawStart = LFlawSecondsRead(lFlawChapter, "start_time");
            double? lFlawEnd = LFlawSecondsRead(lFlawChapter, "end_time");
            if (lFlawStart is not { } lFlawFrom || lFlawEnd is not { } lFlawTo)
            {
                return true;
            }

            if (lFlawFrom < 0 || lFlawTo <= lFlawFrom || lFlawFrom < lFlawPreviousEnd)
            {
                return true;
            }

            lFlawPreviousEnd = lFlawTo;
        }

        return false;
    }

    private static double? LFlawSecondsRead(IReadOnlyDictionary<string, string> lFlawChapter, string lFlawKey) =>
        lFlawChapter.TryGetValue(lFlawKey, out string? lFlawText)
        && double.TryParse(lFlawText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lFlawValue)
            ? lFlawValue
            : null;
}
