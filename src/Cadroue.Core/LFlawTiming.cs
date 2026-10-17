using System.Globalization;
using System.Linq;

namespace Cadroue.Core;

public static class LFlawTiming
{
    private const long LFlawWrapGuard = 8_000_000_000L;

    public static LDossier? LFlawTimingResolve(string lFlawPacketReport)
    {
        IReadOnlyList<IReadOnlyDictionary<string, string>> lFlawPackets =
            LFlawSection.LFlawSectionRead(lFlawPacketReport, "PACKET");
        if (lFlawPackets.Count == 0)
        {
            return null;
        }

        var lFlawLastDts = new Dictionary<string, long>(StringComparer.Ordinal);
        var lFlawPresentPts = new Dictionary<string, int>(StringComparer.Ordinal);
        var lFlawMissingPtsCount = new Dictionary<string, int>(StringComparer.Ordinal);
        bool lFlawMissingDts = false;
        bool lFlawDisorderDts = false;

        foreach (IReadOnlyDictionary<string, string> lFlawPacket in lFlawPackets)
        {
            string lFlawStream = lFlawPacket.TryGetValue("stream_index", out string? lFlawIndex)
                ? lFlawIndex
                : "0";
            long? lFlawPts = LFlawTicksRead(lFlawPacket, "pts");
            long? lFlawDts = LFlawTicksRead(lFlawPacket, "dts");

            if (lFlawPts is not null)
            {
                lFlawPresentPts[lFlawStream] = lFlawPresentPts.GetValueOrDefault(lFlawStream) + 1;
            }

            if (lFlawPts is null && lFlawDts is not null)
            {
                lFlawMissingPtsCount[lFlawStream] = lFlawMissingPtsCount.GetValueOrDefault(lFlawStream) + 1;
            }
            else if (lFlawDts is null && lFlawPts is not null)
            {
                lFlawMissingDts = true;
            }

            if (lFlawDts is { } lFlawCurrent)
            {
                if (lFlawLastDts.TryGetValue(lFlawStream, out long lFlawPrevious)
                    && lFlawCurrent < lFlawPrevious
                    && lFlawPrevious < LFlawWrapGuard)
                {
                    lFlawDisorderDts = true;
                }

                lFlawLastDts[lFlawStream] = lFlawCurrent;
            }
        }

        bool lFlawMissingPts = lFlawMissingPtsCount.Any(lFlawEntry =>
            lFlawEntry.Value > 0
            && lFlawPresentPts.GetValueOrDefault(lFlawEntry.Key) > lFlawEntry.Value);

        var lFlawFindings = new List<string>();
        if (lFlawMissingPts)
        {
            lFlawFindings.Add("Missing presentation timestamps recoverable from decode timestamps");
        }

        if (lFlawMissingDts)
        {
            lFlawFindings.Add("Missing decode timestamps with authoritative presentation timestamps");
        }

        if (lFlawDisorderDts)
        {
            lFlawFindings.Add("Non-monotonic decode timestamps");
        }

        if (lFlawFindings.Count == 0)
        {
            return null;
        }

        var lFlawFlags = new List<string>();
        if (lFlawMissingPts)
        {
            lFlawFlags.Add("+genpts");
        }

        if (lFlawMissingDts || lFlawDisorderDts)
        {
            lFlawFlags.Add("+igndts");
        }

        string lFlawInput = $"-fflags {string.Concat(lFlawFlags)}";

        return new LDossier(
            "Timeline and timestamps",
            1.0,
            "ffprobe -show_packets reading pts/dts/duration",
            string.Join(" | ", lFlawFindings),
            "Full packet traversal across all streams",
            "Packet presentation and decode timing",
            "Regenerate container timing with demuxer flags, packets copied unchanged, no decode",
            "Presentation and decode timestamps",
            LDossierPreservation.LDossierPreservationPacket,
            "Coded packets copied unchanged; container timestamps regenerated",
            "Reconstructed",
            "None",
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryTimeline,
            LDossierRepairInput: lFlawInput);
    }

    private static long? LFlawTicksRead(IReadOnlyDictionary<string, string> lFlawPacket, string lFlawKey) =>
        lFlawPacket.TryGetValue(lFlawKey, out string? lFlawText)
        && long.TryParse(lFlawText, NumberStyles.Integer, CultureInfo.InvariantCulture, out long lFlawValue)
            ? lFlawValue
            : null;
}
