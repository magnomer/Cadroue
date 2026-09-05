using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static partial class LTraceWriter
{
    private const int LTraceWriterCapacity = 20000;
    private const int LTracePersistTimeout = 5000;

    public const string LTraceFolderName = "log";

    private sealed record LTraceWrite(
        long LTraceWriteSequence,
        string LTraceWritePath,
        string LTraceWriteText,
        Action<long> LTraceWriteAction,
        int LTraceWriteLoss);

    private static readonly BlockingCollection<LTraceWrite> lTraceWriterQueue =
        new(new ConcurrentQueue<LTraceWrite>(), LTraceWriterCapacity);

    private static readonly ManualResetEventSlim lTraceWriterIdle = new(true);
    private static readonly object lTraceWriterLock = new();
    private static readonly object lTraceStateLock = new();
    private static readonly ReaderWriterLockSlim lTraceRootLock = new();

    private static readonly string lTraceFileName =
        $"Cadroue-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log";

    private static int lTraceWriterStarted;
    private static int lTraceWriterThread;
    private static long lTraceWriterAccepted;
    private static long lTraceWriterCommitted;
    private static long lTraceWriterDelivered;
    private static int lTraceWriterLoss;

    public static string LTraceFolderRead() => Path.Combine(LDepot.LDepotRootRead(), LTraceFolderName);

    public static string LTracePathRead() => Path.Combine(LTraceFolderRead(), lTraceFileName);

    public static void LTraceWriterRecord(string lTraceEntry) =>
        LTraceWriterRecord(lTraceEntry, _ => { });

    internal static void LTraceWriterRecord(
        string lTraceEntry,
        Action<long> lTraceCommit,
        int lTraceLoss = 1)
    {
        if (string.IsNullOrEmpty(lTraceEntry))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(lTraceCommit);
        LTraceWriterStart();
        int lTraceLossReady = Math.Max(1, lTraceLoss);
        if (lTraceRootLock.IsWriteLockHeld || !lTraceRootLock.TryEnterReadLock(0))
        {
            Interlocked.Add(ref lTraceWriterLoss, lTraceLossReady);
            return;
        }

        try
        {
            lock (lTraceStateLock)
            {
                long lTraceSequence = lTraceWriterAccepted + 1;
                if (!lTraceWriterQueue.TryAdd(new LTraceWrite(
                        lTraceSequence,
                        LTracePathRead(),
                        lTraceEntry,
                        lTraceCommit,
                        lTraceLossReady)))
                {
                    Interlocked.Add(ref lTraceWriterLoss, lTraceLossReady);
                    return;
                }

                lTraceWriterAccepted = lTraceSequence;
                lTraceWriterIdle.Reset();
            }
        }
        finally
        {
            lTraceRootLock.ExitReadLock();
        }
    }

    internal static int LTraceLossRead() => Interlocked.Exchange(ref lTraceWriterLoss, 0);

    public static void LTraceWriterPersist() =>
        _ = LTraceWriterPersist(LTracePersistTimeout);

    internal static bool LTraceWriterPersist(int lTraceTimeoutMilliseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lTraceTimeoutMilliseconds);
        if (Volatile.Read(ref lTraceWriterStarted) == 0
            || Environment.CurrentManagedThreadId == Volatile.Read(ref lTraceWriterThread))
        {
            return true;
        }

        long lTraceWaitStarted = Environment.TickCount64;
        long lTraceTarget;
        lock (lTraceStateLock)
        {
            lTraceTarget = lTraceWriterAccepted;
        }

        while (true)
        {
            lock (lTraceStateLock)
            {
                if (lTraceWriterDelivered >= lTraceTarget)
                {
                    return true;
                }
            }

            long lTraceElapsed = Environment.TickCount64 - lTraceWaitStarted;
            int lTraceRemaining = (int)Math.Max(0, lTraceTimeoutMilliseconds - lTraceElapsed);
            if (lTraceRemaining == 0 || !lTraceWriterIdle.Wait(lTraceRemaining))
            {
                return false;
            }
        }
    }

    public static void LTraceWriterClear()
    {
        LTraceWriterPersist();
        lock (lTraceWriterLock)
        {
            LTraceWriterClose();
            try
            {
                Directory.CreateDirectory(LTraceFolderRead());
                File.WriteAllText(LTracePathRead(), string.Empty, Encoding.UTF8);
            }
            catch (Exception lTraceException) when (lTraceException is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    internal static bool LTraceRootMove(Action lTraceMove)
    {
        ArgumentNullException.ThrowIfNull(lTraceMove);
        lTraceRootLock.EnterWriteLock();
        try
        {
            if (!LTraceWriterPersist(LTracePersistTimeout))
            {
                return false;
            }

            lock (lTraceWriterLock)
            {
                LTraceWriterClose();
                lTraceMove();
                LTraceArchiveUpdate();
                return true;
            }
        }
        finally
        {
            lTraceRootLock.ExitWriteLock();
        }
    }
}
