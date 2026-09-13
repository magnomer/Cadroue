using System.Globalization;

namespace Cadroue.Core;

public static class LFlawMetadata
{
    public static LDossier? LFlawMetadataResolve(string lFlawProbeReport)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams =
            LFlaw.LFlawSectionRead(lFlawProbeReport, "STREAM");
        IReadOnlyDictionary<string, string>? lFlawFormat =
            LFlaw.LFlawSectionRead(lFlawProbeReport, "FORMAT").FirstOrDefault();
        if (lFlawFormat is null)
        {
            return null;
        }

        var lFlawFindings = new List<string>();

        if (lFlawFormat.TryGetValue("nb_streams", out string? lFlawDeclaredCount)
            && int.TryParse(lFlawDeclaredCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out int lFlawCount)
            && lFlawCount != lFlawStreams.Count)
        {
            lFlawFindings.Add(FormattableString.Invariant(
                $"Declared stream count {lFlawCount} contradicts {lFlawStreams.Count} present"));
        }

        double lFlawObserved = LFlawLongestResolve(lFlawStreams);
        double lFlawDeclared = LFlawDurationRead(lFlawFormat);
        if (lFlawObserved > 0)
        {
            if (lFlawDeclared <= 0)
            {
                lFlawFindings.Add("Declared duration missing; establishable from stream timing");
            }
            else if (Math.Abs(lFlawDeclared - lFlawObserved) > 1.0
                && Math.Abs(lFlawDeclared - lFlawObserved) / lFlawObserved > 0.05)
            {
                lFlawFindings.Add(FormattableString.Invariant(
                    $"Declared duration {lFlawDeclared:0.###}s contradicts stream timing {lFlawObserved:0.###}s"));
            }
        }

        (double lFlawLow, double lFlawHigh) = LFlawSpanResolve(lFlawStreams);
        if (lFlawLow > 0
            && lFlawHigh - lFlawLow > 1.0
            && (lFlawHigh - lFlawLow) / lFlawLow > 0.05)
        {
            lFlawFindings.Add(FormattableString.Invariant(
                $"Track timelines disagree: {lFlawHigh:0.###}s against {lFlawLow:0.###}s"));
        }

        if (lFlawFindings.Count == 0)
        {
            return null;
        }

        return new LDossier(
            "Technical metadata",
            1.0,
            "ffprobe -show_streams -show_format -count_packets",
            string.Join(" | ", lFlawFindings),
            "All streams probed",
            "Container technical metadata",
            "Remux -map 0 -c copy regenerating the corrected technical field",
            "Technical interpretation metadata",
            LDossierPreservation.LDossierPreservationPacket,
            "Coded packets copied unchanged; technical metadata rewritten",
            "Preserved",
            "None",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryMetadata);
    }

    private static double LFlawDurationRead(IReadOnlyDictionary<string, string> lFlawSection) =>
        lFlawSection.TryGetValue("duration", out string? lFlawText)
        && double.TryParse(lFlawText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lFlawValue)
            ? lFlawValue
            : 0;

    private static (double, double) LFlawSpanResolve(
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams)
    {
        double lFlawLow = double.MaxValue;
        double lFlawHigh = 0;
        int lFlawTimed = 0;
        foreach (IReadOnlyDictionary<string, string> lFlawStream in lFlawStreams)
        {
            double lFlawValue = LFlawDurationRead(lFlawStream);
            if (lFlawValue <= 0)
            {
                continue;
            }

            lFlawTimed++;
            lFlawLow = Math.Min(lFlawLow, lFlawValue);
            lFlawHigh = Math.Max(lFlawHigh, lFlawValue);
        }

        return lFlawTimed >= 2 ? (lFlawLow, lFlawHigh) : (0, 0);
    }

    private static double LFlawLongestResolve(IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawStreams)
    {
        double lFlawMax = 0;
        foreach (IReadOnlyDictionary<string, string> lFlawStream in lFlawStreams)
        {
            double lFlawValue = LFlawDurationRead(lFlawStream);
            if (lFlawValue > lFlawMax)
            {
                lFlawMax = lFlawValue;
            }
        }

        return lFlawMax;
    }
}
