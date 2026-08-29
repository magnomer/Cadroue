using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LCheckup : IDisposable
{
    private readonly record struct LCheckupRequest(string[] LCheckupPaths, LFlawKind[] LCheckupTargets);

    private readonly object lCheckupLock = new();
    private readonly Queue<LCheckupRequest> lCheckupQueue = new();
    private CancellationTokenSource? lCheckupCancelSource;
    private bool lCheckupRunning;
    private bool lCheckupDisposed;

    public static Func<string, IReadOnlyCollection<LFlawKind>, CancellationToken, IReadOnlyList<LDossier>>? LCheckupScannerSeam;

    public event Action<LCheckupResult>? LCheckupReady;

    public void LCheckupStart(IReadOnlyList<string> lCheckupSources, IReadOnlyCollection<LFlawKind> lCheckupKinds)
    {
        string[] lCheckupPaths = lCheckupSources.ToArray();
        LFlawKind[] lCheckupTargets = lCheckupKinds.Count > 0
            ? lCheckupKinds.ToArray()
            : Enum.GetValues<LFlawKind>();

        CancellationToken lCheckupToken;
        lock (lCheckupLock)
        {
            if (lCheckupDisposed)
            {
                return;
            }

            lCheckupQueue.Enqueue(new LCheckupRequest(lCheckupPaths, lCheckupTargets));
            if (lCheckupRunning)
            {
                return;
            }

            lCheckupCancelSource ??= new CancellationTokenSource();
            lCheckupToken = lCheckupCancelSource.Token;
            lCheckupRunning = true;
        }

        _ = Task.Run(() => LCheckupQueueRun(lCheckupToken), CancellationToken.None);
    }

    private void LCheckupQueueRun(CancellationToken lCheckupToken)
    {
        while (true)
        {
            LCheckupRequest lCheckupRequest;
            lock (lCheckupLock)
            {
                if (lCheckupDisposed || lCheckupQueue.Count == 0)
                {
                    lCheckupRunning = false;
                    return;
                }

                lCheckupRequest = lCheckupQueue.Dequeue();
            }

            LCheckupRun(lCheckupRequest.LCheckupPaths, lCheckupRequest.LCheckupTargets, lCheckupToken);
        }
    }

    private void LCheckupRun(IReadOnlyList<string> lCheckupPaths, IReadOnlyList<LFlawKind> lCheckupTargets, CancellationToken lCheckupToken)
    {
        foreach (string lCheckupPath in lCheckupPaths)
        {
            if (lCheckupToken.IsCancellationRequested)
            {
                return;
            }

            LCheckupSourceRun(lCheckupPath, lCheckupTargets, lCheckupToken);
        }
    }

    private void LCheckupSourceRun(string lCheckupPath, IReadOnlyList<LFlawKind> lCheckupTargets, CancellationToken lCheckupToken)
    {
        foreach (LFlawKind lCheckupKind in lCheckupTargets)
        {
            LCheckupPublish(new LCheckupResult(lCheckupPath, lCheckupKind, LCheckupOutcome.LCheckupOutcomeScanning));
        }

        IReadOnlyList<LDossier>? lCheckupCached = LCheckupCachedRead(lCheckupPath);
        if (lCheckupCached is not null)
        {
            LCheckupResultsPublish(lCheckupPath, lCheckupTargets, lCheckupCached);
            return;
        }

        try
        {
            IReadOnlyList<LDossier> lCheckupScanned =
                LCheckupScannerSeam?.Invoke(lCheckupPath, Array.Empty<LFlawKind>(), lCheckupToken)
                ?? Array.Empty<LDossier>();
            if (lCheckupToken.IsCancellationRequested)
            {
                return;
            }

            LCheckupCachedSave(lCheckupPath, lCheckupScanned);
            LCheckupResultsPublish(lCheckupPath, lCheckupTargets, lCheckupScanned);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception lCheckupException)
        {
            LTraceLog.LTraceErrorRecord($"Diagnosis could not be completed '{Path.GetFileName(lCheckupPath)}'", lCheckupException);
            foreach (LFlawKind lCheckupKind in lCheckupTargets)
            {
                LCheckupPublish(new LCheckupResult(lCheckupPath, lCheckupKind, LCheckupOutcome.LCheckupOutcomeFailed));
            }
        }
    }

    public static IReadOnlyList<LDossier>? LCheckupCachedRead(string lCheckupPath)
    {
        IReadOnlyList<LSidecarDossier>? lCheckupStored = LLibrarian.LLibrarianDiagnosisLoad(lCheckupPath);
        if (lCheckupStored is null)
        {
            return null;
        }

        var lCheckupDossiers = new List<LDossier>(lCheckupStored.Count);
        foreach (LSidecarDossier lCheckupDto in lCheckupStored)
        {
            lCheckupDossiers.Add(LCheckupDossier.LCheckupDossierResolve(lCheckupDto));
        }

        return lCheckupDossiers;
    }

    public static void LCheckupCachedSave(string lCheckupPath, IReadOnlyList<LDossier> lCheckupDossiers)
    {
        TimeSpan lCheckupDuration = LLibrarian.LLibrarianDurationResolve(lCheckupPath);
        LKeyframeSourceIdentity lCheckupIdentity;
        try
        {
            lCheckupIdentity = LKeyframeSourceIdentity.LKeyframeIdentityCreate(lCheckupPath, lCheckupDuration);
        }
        catch (Exception lCheckupException) when (
            lCheckupException is ArgumentException or FileNotFoundException or IOException or UnauthorizedAccessException)
        {
            return;
        }

        var lCheckupDtos = new List<LSidecarDossier>(lCheckupDossiers.Count);
        foreach (LDossier lCheckupDossier in lCheckupDossiers)
        {
            lCheckupDtos.Add(LCheckupDossier.LCheckupDossierResolve(lCheckupDossier));
        }

        LLibrarian.LLibrarianDiagnosisSave(lCheckupPath, lCheckupIdentity, lCheckupDtos);
    }

    private void LCheckupResultsPublish(string lCheckupPath, IReadOnlyList<LFlawKind> lCheckupTargets, IReadOnlyList<LDossier> lCheckupDossiers)
    {
        foreach (LFlawKind lCheckupKind in lCheckupTargets)
        {
            LDossier? lCheckupMatch = null;
            foreach (LDossier lCheckupDossier in lCheckupDossiers)
            {
                if (lCheckupDossier.LDossierKind == lCheckupKind)
                {
                    lCheckupMatch = lCheckupDossier;
                    break;
                }
            }

            LCheckupPublish(lCheckupMatch is { } lCheckupFound
                ? new LCheckupResult(lCheckupPath, lCheckupKind, LCheckupOutcome.LCheckupOutcomeDefect, lCheckupFound)
                : new LCheckupResult(lCheckupPath, lCheckupKind, LCheckupOutcome.LCheckupOutcomeClean));
        }
    }

    private void LCheckupPublish(LCheckupResult lCheckupResult)
    {
        LCheckupReady?.Invoke(lCheckupResult);
    }

    public void Dispose()
    {
        lock (lCheckupLock)
        {
            if (lCheckupDisposed)
            {
                return;
            }

            lCheckupDisposed = true;
            lCheckupQueue.Clear();
            lCheckupCancelSource?.Cancel();
            lCheckupCancelSource?.Dispose();
            lCheckupCancelSource = null;
        }
    }
}
