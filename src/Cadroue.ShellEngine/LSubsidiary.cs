using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LSubsidiary
{
    private const int LSubsidiaryIdleMilliseconds = 500;

    private sealed record LSubsidiaryTask(Func<CancellationToken, Task> LSubsidiaryWork);

    private static readonly System.Collections.Concurrent.ConcurrentQueue<LSubsidiaryTask> lSubsidiaryHigh = new();
    private static readonly System.Collections.Concurrent.ConcurrentQueue<LSubsidiaryTask> lSubsidiaryLow = new();
    private static readonly object lSubsidiaryGate = new();
    private static bool lSubsidiaryBusy;
    private static CancellationTokenSource lSubsidiaryCancellation = new();

    public static void LSubsidiaryOutputDefer(LWorkItem lSubsidiaryItem, string lSubsidiaryOutputPath)
    {
        LWorkItem lSubsidiaryCaptured = lSubsidiaryItem;
        string lSubsidiaryPath = lSubsidiaryOutputPath;
        lSubsidiaryHigh.Enqueue(new LSubsidiaryTask(
            lSubsidiaryToken => LSubsidiaryMeasurement.LSubsidiaryOutputRun(
                lSubsidiaryCaptured, lSubsidiaryPath, lSubsidiaryToken)));
        LSubsidiaryStart();
    }

    public static void LSubsidiarySourceDefer(IReadOnlyList<LWorkItem> lSubsidiaryItems)
    {
        foreach (LWorkItem lSubsidiaryItem in lSubsidiaryItems)
        {
            LWorkItem lSubsidiaryCaptured = lSubsidiaryItem;
            lSubsidiaryLow.Enqueue(new LSubsidiaryTask(
                lSubsidiaryToken => LSubsidiaryMeasurement.LSubsidiarySourceRun(
                    lSubsidiaryCaptured, lSubsidiaryToken)));
        }

        LSubsidiaryStart();
    }

    public static void LSubsidiaryCancel()
    {
        CancellationTokenSource lSubsidiaryRetired;
        lock (lSubsidiaryGate)
        {
            while (lSubsidiaryHigh.TryDequeue(out _))
            {
            }

            while (lSubsidiaryLow.TryDequeue(out _))
            {
            }

            lSubsidiaryRetired = lSubsidiaryCancellation;
            lSubsidiaryCancellation = new CancellationTokenSource();
        }

        lSubsidiaryRetired.Cancel();
    }

    private static void LSubsidiaryStart()
    {
        lock (lSubsidiaryGate)
        {
            if (lSubsidiaryBusy || (lSubsidiaryHigh.IsEmpty && lSubsidiaryLow.IsEmpty))
            {
                return;
            }

            lSubsidiaryBusy = true;
        }

        _ = Task.Run(LSubsidiaryRun, CancellationToken.None);
    }

    private static async Task LSubsidiaryRun()
    {
        try
        {
            while (LSubsidiaryNextRead() is { } lSubsidiaryTask)
            {
                CancellationToken lSubsidiaryToken = lSubsidiaryCancellation.Token;
                while (LStationBoard.LStationActiveCheck())
                {
                    lSubsidiaryToken.ThrowIfCancellationRequested();
                    await Task.Delay(LSubsidiaryIdleMilliseconds, lSubsidiaryToken).ConfigureAwait(false);
                }

                await lSubsidiaryTask.LSubsidiaryWork(lSubsidiaryCancellation.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            lock (lSubsidiaryGate)
            {
                lSubsidiaryBusy = false;
            }

            if (!lSubsidiaryHigh.IsEmpty || !lSubsidiaryLow.IsEmpty)
            {
                LSubsidiaryStart();
            }
        }
    }

    private static LSubsidiaryTask? LSubsidiaryNextRead()
    {
        if (lSubsidiaryHigh.TryDequeue(out LSubsidiaryTask? lSubsidiaryHighTask))
        {
            return lSubsidiaryHighTask;
        }

        return lSubsidiaryLow.TryDequeue(out LSubsidiaryTask? lSubsidiaryLowTask) ? lSubsidiaryLowTask : null;
    }
}
