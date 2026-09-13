using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal static partial class TInterface
{
    internal static LRemedyPlan TRemedyPlanCreate(IReadOnlyList<LDossier> dossiers) =>
        LRemedy.LRemedyPlanCreate(dossiers);

    internal static LDossier? TFlawContainerResolve(string probeError, string copyError) =>
        LFlawMux.LFlawContainerResolve(probeError, copyError);

    internal static LDossier? TFlawTransportResolve(string probeReport, string copyError) =>
        LFlawTransport.LFlawTransportResolve(probeReport, copyError);

    internal static LDossier? TFlawTruncationResolve(string probeError, string copyError) =>
        LFlawTruncation.LFlawTruncationResolve(probeError, copyError);

    internal static LDossier? TFlawMetadataResolve(string probeReport) =>
        LFlawMetadata.LFlawMetadataResolve(probeReport);

    internal static LDossier? TFlawIndexResolve(string indexedError, string ignidxError, string seekError) =>
        LFlawIndex.LFlawIndexResolve(indexedError, ignidxError, seekError);

    internal static LDossier? TFlawFramingResolve(string copyError, string probeReport) =>
        LFlawStream.LFlawFramingResolve(copyError, probeReport);

    internal static LDossier? TFlawConfigResolve(string probeReport, string decodeError) =>
        LFlawStream.LFlawConfigResolve(probeReport, decodeError);

    internal static LDossier? TFlawTimingResolve(string packetReport) =>
        LFlawStream.LFlawTimingResolve(packetReport);

    internal static LDossier? TFlawSecondaryResolve(string streamReport, string chapterReport, string secondaryError) =>
        LFlawSecondary.LFlawSecondaryResolve(streamReport, chapterReport, secondaryError);

    internal static LDossier? TFlawCodedResolve(string decodeError) =>
        LFlawCoded.LFlawCodedResolve(decodeError);

    internal static LDossier? TFlawFfvoneResolve(string probeReport, string crcError) =>
        LFlawFfvone.LFlawFfvoneResolve(probeReport, crcError);

    internal static LDossier TDossierDefectCreate(
        string defect,
        LDossierCategory category,
        LDossierPreservation preservation = LDossierPreservation.LDossierPreservationExact) =>
        new(
            defect, 1.0, string.Empty, string.Empty, string.Empty, string.Empty,
            string.Empty, string.Empty, preservation, string.Empty, string.Empty,
            string.Empty, LDossierValidation.LDossierValidationPassed, category);
}
