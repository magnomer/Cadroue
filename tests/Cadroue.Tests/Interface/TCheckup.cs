using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal static class TCheckup
{
    internal const string TCheckupUntested = "Not run";
    internal const string TCheckupScanning = "Checking";
    internal const string TCheckupClean = "No defect";
    internal const string TCheckupFailed = "Failed";
    internal const string TCheckupDefectLabel = "Defect";
    internal const string TCheckupEvidenceLabel = "Evidence";
    internal const string TCheckupRepairLabel = "Repair";

    private static readonly LCheckupStrings TCheckupStrings = new(
        TCheckupUntested,
        TCheckupScanning,
        TCheckupClean,
        TCheckupFailed,
        TCheckupDefectLabel,
        TCheckupEvidenceLabel,
        TCheckupRepairLabel);

    internal static string TCheckupCleanFormat() =>
        TCheckupBodyFormat(new LCheckupResult("a.mp4", LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeClean));

    internal static string TCheckupFailedFormat() =>
        TCheckupBodyFormat(new LCheckupResult("a.mp4", LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeFailed));

    internal static string TCheckupMissingFormat() =>
        TCheckupBodyFormat(new LCheckupResult("a.mp4", LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeDefect));

    internal static string TCheckupDefectFormat(string defect, string evidence, string repair)
    {
        var dossier = new LDossier(
            defect,
            0.9,
            evidence,
            string.Empty,
            string.Empty,
            string.Empty,
            repair,
            string.Empty,
            LDossierPreservation.LDossierPreservationExact,
            string.Empty,
            string.Empty,
            string.Empty,
            LDossierValidation.LDossierValidationUntested,
            LDossierCategory.LDossierCategoryContainer,
            LDossierKind: LFlawKind.LFlawKindContainer);
        return TCheckupBodyFormat(new LCheckupResult("a.mp4", LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeDefect, dossier));
    }

    private static string TCheckupBodyFormat(LCheckupResult result) =>
        LCheckupFormat.LCheckupBodyFormat(result, TCheckupStrings);

    internal static void TCheckupProgressApply(
        string line,
        TimeSpan duration,
        double start,
        double end,
        IProgress<double> progress) =>
        LFlawScan.LFlawProgressApply(line, duration, start, end, progress);
}

internal readonly record struct TCheckupJobResult(string TCheckupPath, bool TCheckupCompleted, bool TCheckupClean);
internal readonly record struct TCheckupSample(string TCheckupPath, double TCheckupValue);

internal sealed class TCheckupJob : IDisposable
{
    private readonly Func<string, IReadOnlyCollection<LFlawKind>, CancellationToken, IProgress<double>?, IReadOnlyList<LDossier>>? tCheckupScanner;
    private readonly Func<string, IReadOnlyList<LSidecarDossier>?>? tCheckupReader;
    private readonly LCheckup tCheckup = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<TCheckupJobResult> tCheckupResults = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<TCheckupSample> tCheckupProgress = new();

    internal TCheckupJob(Action<string, CancellationToken> scanner)
        : this((path, token, _) => scanner(path, token))
    {
    }

    internal TCheckupJob(Action<string, CancellationToken, IProgress<double>?> scanner)
    {
        tCheckupScanner = LCheckup.LCheckupScannerSeam;
        tCheckupReader = LLibrarian.LLibrarianDiagnosisReader;
        LLibrarian.LLibrarianDiagnosisReader = _ => null;
        LCheckup.LCheckupScannerSeam = (path, _, token, progress) =>
        {
            scanner(path, token, progress);
            return Array.Empty<LDossier>();
        };
        tCheckup.LCheckupReady += result => tCheckupResults.Enqueue(new TCheckupJobResult(
            result.LCheckupSource,
            result.LCheckupOutcome is LCheckupOutcome.LCheckupOutcomeClean
                or LCheckupOutcome.LCheckupOutcomeDefect
                or LCheckupOutcome.LCheckupOutcomeFailed,
            result.LCheckupOutcome == LCheckupOutcome.LCheckupOutcomeClean));
        tCheckup.LCheckupProgress += (path, value) =>
            tCheckupProgress.Enqueue(new TCheckupSample(path, value));
    }

    internal void TCheckupStart(string path) =>
        tCheckup.LCheckupStart(new[] { path }, new[] { LFlawKind.LFlawKindContainer });

    internal void TCheckupCancel(string path) =>
        tCheckup.LCheckupCancel(path, LFlawKind.LFlawKindContainer);

    internal IReadOnlyList<TCheckupJobResult> TScoutResultsRead() => tCheckupResults.ToArray();

    internal IReadOnlyList<TCheckupSample> TCheckupProgressRead() => tCheckupProgress.ToArray();

    public void Dispose()
    {
        tCheckup.Dispose();
        LCheckup.LCheckupScannerSeam = tCheckupScanner;
        LLibrarian.LLibrarianDiagnosisReader = tCheckupReader;
    }
}
