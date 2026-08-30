using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal static class TCheckup
{
    internal const string Untested = "Not run";
    internal const string Scanning = "Checking";
    internal const string Clean = "No defect";
    internal const string Failed = "Failed";
    internal const string DefectLabel = "Defect";
    internal const string EvidenceLabel = "Evidence";
    internal const string RepairLabel = "Repair";

    private static readonly LCheckupStrings Strings = new(
        Untested,
        Scanning,
        Clean,
        Failed,
        DefectLabel,
        EvidenceLabel,
        RepairLabel);

    internal static string CleanFormat() =>
        BodyFormat(new LCheckupResult("a.mp4", LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeClean));

    internal static string FailedFormat() =>
        BodyFormat(new LCheckupResult("a.mp4", LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeFailed));

    internal static string DefectMissingDossierFormat() =>
        BodyFormat(new LCheckupResult("a.mp4", LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeDefect));

    internal static string DefectFormat(string defect, string evidence, string repair)
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
        return BodyFormat(new LCheckupResult("a.mp4", LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeDefect, dossier));
    }

    private static string BodyFormat(LCheckupResult result) =>
        LCheckupFormat.LCheckupBodyFormat(result, Strings);

    internal static void ProgressApply(
        string line,
        TimeSpan duration,
        double start,
        double end,
        IProgress<double> progress) =>
        LFlawScan.LFlawProgressApply(line, duration, start, end, progress);
}

internal readonly record struct TCheckupJobResult(string Path, bool Completed, bool Clean);
internal readonly record struct TCheckupProgress(string Path, double Value);

internal sealed class TCheckupJob : IDisposable
{
    private readonly Func<string, IReadOnlyCollection<LFlawKind>, CancellationToken, IProgress<double>?, IReadOnlyList<LDossier>>? tCheckupScanner;
    private readonly Func<string, IReadOnlyList<LSidecarDossier>?>? tCheckupReader;
    private readonly LCheckup tCheckup = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<TCheckupJobResult> tCheckupResults = new();
    private readonly System.Collections.Concurrent.ConcurrentQueue<TCheckupProgress> tCheckupProgress = new();

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
            tCheckupProgress.Enqueue(new TCheckupProgress(path, value));
    }

    internal void Start(string path) =>
        tCheckup.LCheckupStart(new[] { path }, new[] { LFlawKind.LFlawKindContainer });

    internal void Cancel(string path) =>
        tCheckup.LCheckupCancel(path, LFlawKind.LFlawKindContainer);

    internal IReadOnlyList<TCheckupJobResult> ResultsRead() => tCheckupResults.ToArray();

    internal IReadOnlyList<TCheckupProgress> ProgressRead() => tCheckupProgress.ToArray();

    public void Dispose()
    {
        tCheckup.Dispose();
        LCheckup.LCheckupScannerSeam = tCheckupScanner;
        LLibrarian.LLibrarianDiagnosisReader = tCheckupReader;
    }
}
