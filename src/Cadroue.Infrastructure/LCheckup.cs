using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public sealed class LCheckup : IDisposable
{
    private readonly record struct LCheckupRequest(string LCheckupPath, LFlawKind[] LCheckupTargets);

    private sealed class LCheckupFeed(Action<double> lCheckupReport) : IProgress<double>
    {
        public void Report(double lCheckupValue) => lCheckupReport(lCheckupValue);
    }

    private readonly object lCheckupLock = new();
    private readonly Queue<LCheckupRequest> lCheckupQueue = new();
    private LCheckupRequest? lCheckupActive;
    private CancellationTokenSource? lCheckupCancellationSource;
    private bool lCheckupRunning;
    private bool lCheckupDisposed;

    public static Func<string, IReadOnlyCollection<LFlawKind>, CancellationToken, IProgress<double>?, IReadOnlyList<LDossier>>? LCheckupScannerSeam;

    public event Action<LCheckupResult>? LCheckupReady;
    public event Action<string, double>? LCheckupProgress;

    public void LCheckupStart(IReadOnlyList<string> lCheckupSources, IReadOnlyCollection<LFlawKind> lCheckupKinds)
    {
        string[] lCheckupPaths = lCheckupSources.ToArray();
        LFlawKind[] lCheckupTargets = lCheckupKinds.Count > 0
            ? lCheckupKinds.ToArray()
            : Enum.GetValues<LFlawKind>();

        lock (lCheckupLock)
        {
            if (lCheckupDisposed)
            {
                return;
            }

            foreach (string lCheckupPath in lCheckupPaths)
            {
                lCheckupQueue.Enqueue(new LCheckupRequest(lCheckupPath, lCheckupTargets));
            }

            if (lCheckupRunning)
            {
                return;
            }

            lCheckupRunning = true;
        }

        _ = Task.Run(LCheckupQueueRun, CancellationToken.None);
    }

    public void LCheckupCancel(string lCheckupSource, LFlawKind lCheckupKind)
    {
        lock (lCheckupLock)
        {
            if (lCheckupDisposed)
            {
                return;
            }

            var lCheckupRetained = new Queue<LCheckupRequest>();
            while (lCheckupQueue.TryDequeue(out LCheckupRequest lCheckupQueued))
            {
                if (!string.Equals(lCheckupQueued.LCheckupPath, lCheckupSource, StringComparison.OrdinalIgnoreCase))
                {
                    lCheckupRetained.Enqueue(lCheckupQueued);
                    continue;
                }

                LFlawKind[] lCheckupTargets = lCheckupQueued.LCheckupTargets
                    .Where(lCheckupTarget => lCheckupTarget != lCheckupKind)
                    .ToArray();
                if (lCheckupTargets.Length > 0)
                {
                    lCheckupRetained.Enqueue(new LCheckupRequest(lCheckupQueued.LCheckupPath, lCheckupTargets));
                }
            }

            while (lCheckupRetained.TryDequeue(out LCheckupRequest lCheckupQueued))
            {
                lCheckupQueue.Enqueue(lCheckupQueued);
            }

            if (lCheckupActive is not { } lCheckupActiveRequest
                || !string.Equals(lCheckupActiveRequest.LCheckupPath, lCheckupSource, StringComparison.OrdinalIgnoreCase)
                || !lCheckupActiveRequest.LCheckupTargets.Contains(lCheckupKind))
            {
                return;
            }

            LFlawKind[] lCheckupRemaining = lCheckupActiveRequest.LCheckupTargets
                .Where(lCheckupTarget => lCheckupTarget != lCheckupKind)
                .ToArray();
            if (lCheckupRemaining.Length > 0)
            {
                var lCheckupRestart = new Queue<LCheckupRequest>();
                lCheckupRestart.Enqueue(new LCheckupRequest(lCheckupActiveRequest.LCheckupPath, lCheckupRemaining));
                while (lCheckupQueue.TryDequeue(out LCheckupRequest lCheckupQueued))
                {
                    lCheckupRestart.Enqueue(lCheckupQueued);
                }

                while (lCheckupRestart.TryDequeue(out LCheckupRequest lCheckupQueued))
                {
                    lCheckupQueue.Enqueue(lCheckupQueued);
                }
            }

            lCheckupActive = null;
            lCheckupCancellationSource?.Cancel();
        }
    }

    private void LCheckupQueueRun()
    {
        while (true)
        {
            LCheckupRequest lCheckupRequest;
            CancellationToken lCheckupToken;
            lock (lCheckupLock)
            {
                if (lCheckupDisposed || lCheckupQueue.Count == 0)
                {
                    lCheckupRunning = false;
                    return;
                }

                lCheckupRequest = lCheckupQueue.Dequeue();
                lCheckupCancellationSource = new CancellationTokenSource();
                lCheckupToken = lCheckupCancellationSource.Token;
                lCheckupActive = lCheckupRequest;
            }

            LCheckupSourceRun(lCheckupRequest.LCheckupPath, lCheckupRequest.LCheckupTargets, lCheckupToken);
            lock (lCheckupLock)
            {
                lCheckupActive = null;
                lCheckupCancellationSource?.Dispose();
                lCheckupCancellationSource = null;
            }
        }
    }

    private void LCheckupSourceRun(string lCheckupPath, IReadOnlyList<LFlawKind> lCheckupTargets, CancellationToken lCheckupToken)
    {
        foreach (LFlawKind lCheckupKind in lCheckupTargets)
        {
            if (lCheckupToken.IsCancellationRequested)
            {
                return;
            }

            LCheckupPublish(new LCheckupResult(lCheckupPath, lCheckupKind, LCheckupOutcome.LCheckupOutcomeScanning));
        }

        IReadOnlyList<LDossier>? lCheckupCached = LCheckupCachedRead(lCheckupPath);
        if (lCheckupCached is not null)
        {
            LCheckupResultsPublish(lCheckupPath, lCheckupTargets, lCheckupCached, lCheckupToken);
            return;
        }

        try
        {
            IReadOnlyList<LDossier> lCheckupScanned =
                LCheckupScannerSeam?.Invoke(
                    lCheckupPath,
                    Array.Empty<LFlawKind>(),
                    lCheckupToken,
                    new LCheckupFeed(lCheckupValue => LCheckupProgress?.Invoke(lCheckupPath, lCheckupValue)))
                ?? Array.Empty<LDossier>();
            if (lCheckupToken.IsCancellationRequested)
            {
                return;
            }

            LCheckupCachedSave(lCheckupPath, lCheckupScanned);
            LCheckupResultsPublish(lCheckupPath, lCheckupTargets, lCheckupScanned, lCheckupToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception lCheckupException)
        {
            if (lCheckupToken.IsCancellationRequested)
            {
                return;
            }

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

    private void LCheckupResultsPublish(
        string lCheckupPath,
        IReadOnlyList<LFlawKind> lCheckupTargets,
        IReadOnlyList<LDossier> lCheckupDossiers,
        CancellationToken lCheckupToken)
    {
        foreach (LFlawKind lCheckupKind in lCheckupTargets)
        {
            if (lCheckupToken.IsCancellationRequested)
            {
                return;
            }

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
            lCheckupCancellationSource?.Cancel();
        }
    }
}
