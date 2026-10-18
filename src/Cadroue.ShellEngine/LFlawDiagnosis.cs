using System.Linq;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LFlawDiagnosis
{
    internal static IReadOnlyList<LDossier> LFlawDiagnosisResolve(LFlawEvidence lFlawEvidence)
    {
        string lFlawProbeError = lFlawEvidence.LFlawEvidenceProbe;
        string lFlawCopyError = lFlawEvidence.LFlawEvidenceCopying;
        string lFlawMetaReport = lFlawEvidence.LFlawEvidenceMeta;
        if (!lFlawEvidence.LFlawEvidenceOpened)
        {
            var lFlawUnopened = new List<LDossier>();
            if (LFlawTruncation.LFlawTruncationResolve(lFlawProbeError, lFlawCopyError) is { } lFlawFinal)
            {
                lFlawUnopened.Add(lFlawFinal with { LDossierKind = LFlawKind.LFlawKindTruncation });
            }
            else if (LFlawMux.LFlawContainerResolve(lFlawProbeError, lFlawCopyError) is { } lFlawOpen)
            {
                lFlawUnopened.Add(lFlawOpen with { LDossierKind = LFlawKind.LFlawKindContainer });
            }

            return lFlawUnopened;
        }

        var lFlawDossiers = new List<LDossier>();
        if (LFlawMux.LFlawContainerResolve(lFlawProbeError, lFlawCopyError) is { } lFlawContainer)
        {
            lFlawDossiers.Add(lFlawContainer with { LDossierKind = LFlawKind.LFlawKindContainer });
        }

        if (LFlawTruncation.LFlawTruncationResolve(lFlawProbeError, lFlawCopyError) is { } lFlawTruncation)
        {
            lFlawDossiers.Add(lFlawTruncation with { LDossierKind = LFlawKind.LFlawKindTruncation });
        }

        if (LFlawTransport.LFlawTransportResolve(lFlawMetaReport, lFlawEvidence.LFlawEvidenceTransport)
            is { } lFlawTransport)
        {
            lFlawDossiers.Add(lFlawTransport with { LDossierKind = LFlawKind.LFlawKindTransport });
        }

        if (LFlawMetadata.LFlawMetadataResolve(lFlawMetaReport) is { } lFlawMetadata)
        {
            lFlawDossiers.Add(lFlawMetadata with { LDossierKind = LFlawKind.LFlawKindMetadata });
        }

        if (LFlawIndex.LFlawIndexResolve(
            lFlawCopyError,
            lFlawEvidence.LFlawEvidenceIgnidx,
            lFlawEvidence.LFlawEvidenceSeeking) is { } lFlawIndex)
        {
            lFlawDossiers.Add(lFlawIndex with { LDossierKind = LFlawKind.LFlawKindIndex });
        }

        if (LFlawFraming.LFlawFramingResolve(lFlawCopyError, lFlawMetaReport) is { } lFlawFraming)
        {
            lFlawDossiers.Add(lFlawFraming with { LDossierKind = LFlawKind.LFlawKindFraming });
        }

        if (LFlawConfig.LFlawConfigResolve(lFlawMetaReport, lFlawEvidence.LFlawEvidenceDecode) is { } lFlawConfig)
        {
            lFlawDossiers.Add(lFlawConfig with { LDossierKind = LFlawKind.LFlawKindConfig });
        }

        if (LFlawTiming.LFlawTimingResolve(lFlawEvidence.LFlawEvidencePacket) is { } lFlawTiming)
        {
            lFlawDossiers.Add(lFlawTiming with { LDossierKind = LFlawKind.LFlawKindTiming });
        }

        if (LFlawSecondary.LFlawSecondaryResolve(
            lFlawMetaReport,
            lFlawEvidence.LFlawEvidenceChapter,
            lFlawEvidence.LFlawEvidenceSecondary) is { } lFlawSecondary)
        {
            lFlawDossiers.Add(lFlawSecondary with { LDossierKind = LFlawKind.LFlawKindSecondary });
        }

        if (LFlawFfvone.LFlawFfvoneResolve(lFlawMetaReport, lFlawEvidence.LFlawEvidenceCrc) is { } lFlawFfvone)
        {
            lFlawDossiers.Add(lFlawFfvone with { LDossierKind = LFlawKind.LFlawKindFfvone });
        }

        if (!lFlawDossiers.Any(lFlawDossier => LFlawCarriageCheck(lFlawDossier.LDossierKind))
            && LFlawCoded.LFlawCodedResolve(lFlawEvidence.LFlawEvidenceCoded) is { } lFlawCoded)
        {
            lFlawDossiers.Add(lFlawCoded with { LDossierKind = LFlawKind.LFlawKindCoded });
        }

        return lFlawDossiers;
    }

    private static bool LFlawCarriageCheck(LFlawKind lFlawKind) => lFlawKind switch
    {
        LFlawKind.LFlawKindContainer => true,
        LFlawKind.LFlawKindTruncation => true,
        LFlawKind.LFlawKindTransport => true,
        LFlawKind.LFlawKindFraming => true,
        LFlawKind.LFlawKindConfig => true,
        LFlawKind.LFlawKindFfvone => true,
        _ => false
    };
}
