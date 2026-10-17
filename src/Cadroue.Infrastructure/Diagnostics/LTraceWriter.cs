using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Cadroue.Infrastructure;

public static class LTraceWriter
{
    private const int LTraceWriterCapacity = 20000;
    private const int LTracePersistTimeout = 5000;
    private const int LTraceWriterIdle = 250;
    private const int LTraceWriterBatch = 256;
    private const int LTraceWriterRetry = 8;
    private const ThreadPriority LTraceWriterLevel = ThreadPriority.BelowNormal;

    private sealed record LTraceWrite(
        long LTraceWriteSequence,
        string LTraceWritePath,
        string LTraceWriteText,
        Action<long> LTraceWriteAction,
        int LTraceWriteLoss);

    private static readonly BlockingCollection<LTraceWrite> lTraceWriterQueue =
        new(new ConcurrentQueue<LTraceWrite>(), LTraceWriterCapacity);

    private static readonly ManualResetEventSlim lTraceWriterIdle = new(true);
    private static readonly object lTraceStateLock = new();
    private static readonly ReaderWriterLockSlim lTraceRootLock = new();

    private static int lTraceWriterStarted;
    private static int lTraceWriterThread;
    private static long lTraceWriterAccepted;
    private static long lTraceWriterCommitted;
    private static long lTraceWriterDelivered;
    private static int lTraceWriterLoss;

    private static FileStream? lTraceWriterStream;
    private static string? lTraceWriterPath;
    private static Thread? lTraceWriterRunner;

    internal static long LTraceWriterCommitted => Volatile.Read(ref lTraceWriterCommitted);

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
                        LTraceFolder.LTracePathRead(),
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
        lock (LTraceFolder.LTraceFolderLock)
        {
            LTraceWriterClose();
            try
            {
                Directory.CreateDirectory(LTraceFolder.LTraceFolderRead());
                File.WriteAllText(LTraceFolder.LTracePathRead(), string.Empty, Encoding.UTF8);
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

            lock (LTraceFolder.LTraceFolderLock)
            {
                LTraceWriterClose();
                lTraceMove();
                LTraceArchive.LTraceArchiveUpdate();
                return true;
            }
        }
        finally
        {
            lTraceRootLock.ExitWriteLock();
        }
    }

    private static void LTraceWriterStart()
    {
        if (Interlocked.CompareExchange(ref lTraceWriterStarted, 1, 0) != 0)
        {
            return;
        }

        lock (lTraceStateLock)
        {
            lTraceWriterRunner = new Thread(LTraceWriterRun)
            {
                IsBackground = true,
                Name = "Cadroue.Trace",
                Priority = LTraceWriterLevel
            };
            lTraceWriterRunner.Start();
        }
    }

    private static void LTraceWriterRun()
    {
        Volatile.Write(ref lTraceWriterThread, Environment.CurrentManagedThreadId);
        lock (LTraceFolder.LTraceFolderLock)
        {
            LTraceArchive.LTraceArchiveUpdate();
        }

        LTraceWrite? lTraceWaiting = null;
        while (true)
        {
            try
            {
                LTraceWrite? lTraceEntry = lTraceWaiting;
                lTraceWaiting = null;
                if (lTraceEntry is null
                    && !lTraceWriterQueue.TryTake(out lTraceEntry, LTraceWriterIdle))
                {
                    continue;
                }

                var lTraceBatch = new List<LTraceWrite>(LTraceWriterBatch) { lTraceEntry };
                while (lTraceBatch.Count < LTraceWriterBatch
                    && lTraceWriterQueue.TryTake(out LTraceWrite? lTraceNext, 0))
                {
                    if (!string.Equals(
                        lTraceNext.LTraceWritePath,
                        lTraceEntry.LTraceWritePath,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        lTraceWaiting = lTraceNext;
                        break;
                    }

                    lTraceBatch.Add(lTraceNext);
                }

                bool lTraceSaved = false;
                for (int lTraceAttempt = 0; lTraceAttempt < LTraceWriterRetry; lTraceAttempt++)
                {
                    if (LTraceBatchPersist(lTraceBatch))
                    {
                        lTraceSaved = true;
                        break;
                    }

                    Thread.Sleep(LTraceWriterIdle);
                }

                if (lTraceSaved)
                {
                    Volatile.Write(ref lTraceWriterCommitted, lTraceBatch[^1].LTraceWriteSequence);
                }
                else
                {
                    Interlocked.Add(ref lTraceWriterLoss, lTraceBatch.Sum(lTraceWrite => lTraceWrite.LTraceWriteLoss));
                }

                if (lTraceSaved)
                {
                    foreach (LTraceWrite lTraceWrite in lTraceBatch)
                    {
                        try
                        {
                            lTraceWrite.LTraceWriteAction(lTraceWrite.LTraceWriteSequence);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

                lock (lTraceStateLock)
                {
                    lTraceWriterDelivered = lTraceBatch[^1].LTraceWriteSequence;
                    if (lTraceWriterDelivered >= lTraceWriterAccepted)
                    {
                        lTraceWriterIdle.Set();
                    }
                }
            }
            catch (Exception)
            {
                Thread.Sleep(LTraceWriterIdle);
            }
        }
    }

    private static bool LTraceBatchPersist(List<LTraceWrite> lTraceBatch)
    {
        lock (LTraceFolder.LTraceFolderLock)
        {
            long lTraceLength = -1;
            try
            {
                FileStream? lTraceStream = LTraceWriterOpen(lTraceBatch[0].LTraceWritePath);
                if (lTraceStream is null)
                {
                    return false;
                }

                lTraceLength = lTraceStream.Length;
                if (lTraceLength == 0)
                {
                    lTraceStream.Write(Encoding.UTF8.Preamble);
                }

                string lTraceText = string.Concat(lTraceBatch.Select(lTraceEntry => lTraceEntry.LTraceWriteText));
                lTraceStream.Write(Encoding.UTF8.GetBytes(lTraceText));
                lTraceStream.Flush();
                return true;
            }
            catch (Exception lTraceException) when (lTraceException is IOException or UnauthorizedAccessException)
            {
                if (lTraceWriterStream is not null && lTraceLength >= 0)
                {
                    try
                    {
                        lTraceWriterStream.SetLength(lTraceLength);
                        lTraceWriterStream.Flush(flushToDisk: true);
                    }
                    catch (Exception lTraceRollback) when (lTraceRollback is IOException or UnauthorizedAccessException)
                    {
                    }
                }

                LTraceWriterClose();
                return false;
            }
        }
    }

    private static FileStream? LTraceWriterOpen(string lTracePath)
    {
        if (lTraceWriterStream is not null
            && string.Equals(lTraceWriterPath, lTracePath, StringComparison.OrdinalIgnoreCase))
        {
            return lTraceWriterStream;
        }

        LTraceWriterClose();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lTracePath)!);
            var lTraceStream = new FileStream(
                lTracePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
            lTraceStream.Seek(0, SeekOrigin.End);
            lTraceWriterStream = lTraceStream;
            lTraceWriterPath = lTracePath;
            return lTraceWriterStream;
        }
        catch (Exception lTraceException) when (lTraceException is IOException or UnauthorizedAccessException)
        {
            lTraceWriterStream = null;
            lTraceWriterPath = null;
            return null;
        }
    }

    private static void LTraceWriterClose()
    {
        try
        {
            lTraceWriterStream?.Flush(flushToDisk: true);
            lTraceWriterStream?.Dispose();
        }
        catch (IOException)
        {
        }

        lTraceWriterStream = null;
        lTraceWriterPath = null;
    }
}
