using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LTraceWriter
{
    private const int LTraceWriterCapacity = 20000;
    private const int LTraceWriterIdle = 250;
    private const int LTraceWriterBatch = 256;
    private const int LTraceWriterRetry = 8;

    public const string LTraceFolderName = "log";
    public const string LTraceArchiveSuffix = ".gz";

    private const int LTraceArchiveKeep = 20;
    private const int LTraceArchiveDays = 14;

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

    private static readonly string lTraceFileName =
        $"Cadroue-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log";

    private static FileStream? lTraceWriterStream;
    private static string? lTraceWriterPath;
    private static int lTraceWriterStarted;
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
        lock (lTraceStateLock)
        {
            long lTraceSequence = lTraceWriterAccepted + 1;
            lTraceWriterQueue.Add(new LTraceWrite(
                lTraceSequence,
                LTracePathRead(),
                lTraceEntry,
                lTraceCommit,
                Math.Max(1, lTraceLoss)));
            lTraceWriterAccepted = lTraceSequence;
            lTraceWriterIdle.Reset();
        }
    }

    internal static int LTraceLossRead() => Interlocked.Exchange(ref lTraceWriterLoss, 0);

    public static void LTraceWriterPersist()
    {
        if (Volatile.Read(ref lTraceWriterStarted) == 0)
        {
            return;
        }

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
                    return;
                }
            }

            lTraceWriterIdle.Wait();
        }
    }

    public static string LTraceWriterRead() => LTraceWriterRead(out _);

    internal static string LTraceWriterRead(out long lTraceCommitted)
    {
        LTraceWriterPersist();
        lock (lTraceWriterLock)
        {
            try
            {
                string lTracePath = LTracePathRead();
                if (!File.Exists(lTracePath))
                {
                    lTraceCommitted = Volatile.Read(ref lTraceWriterCommitted);
                    return string.Empty;
                }

                using var lTraceStream = new FileStream(
                    lTracePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var lTraceReader = new StreamReader(lTraceStream, Encoding.UTF8);
                string lTraceText = lTraceReader.ReadToEnd();
                lTraceCommitted = Volatile.Read(ref lTraceWriterCommitted);
                return lTraceText;
            }
            catch (IOException)
            {
                lTraceCommitted = Volatile.Read(ref lTraceWriterCommitted);
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                lTraceCommitted = Volatile.Read(ref lTraceWriterCommitted);
                return string.Empty;
            }
        }
    }

    public static List<string> LTraceFilesRead()
    {
        try
        {
            string lTraceFolder = LTraceFolderRead();
            if (!Directory.Exists(lTraceFolder))
            {
                return new List<string>();
            }

            return Directory.GetFiles(lTraceFolder, "Cadroue-*")
                .Where(LTraceFileCheck)
                .OrderByDescending(lTraceFile => lTraceFile, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception lTraceException) when (lTraceException is IOException or UnauthorizedAccessException)
        {
            return new List<string>();
        }
    }

    public static string LTraceFileRead(string lTracePath)
    {
        if (string.Equals(lTracePath, LTracePathRead(), StringComparison.OrdinalIgnoreCase))
        {
            return LTraceWriterRead();
        }

        lock (lTraceWriterLock)
        {
            try
            {
                if (!File.Exists(lTracePath))
                {
                    return string.Empty;
                }

                using var lTraceStream = new FileStream(
                    lTracePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                Stream lTraceContent = lTracePath.EndsWith(LTraceArchiveSuffix, StringComparison.OrdinalIgnoreCase)
                    ? new GZipStream(lTraceStream, CompressionMode.Decompress)
                    : lTraceStream;
                using (lTraceContent)
                using (var lTraceReader = new StreamReader(lTraceContent, Encoding.UTF8))
                {
                    return lTraceReader.ReadToEnd();
                }
            }
            catch (Exception lTraceException)
                when (lTraceException is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                return string.Empty;
            }
        }
    }

    private static bool LTraceFileCheck(string lTracePath) =>
        lTracePath.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
        || lTracePath.EndsWith(".log" + LTraceArchiveSuffix, StringComparison.OrdinalIgnoreCase);

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
        LTraceWriterPersist();
        lock (lTraceWriterLock)
        {
            LTraceWriterClose();
            lTraceMove();
            LTraceArchiveUpdate();
            return true;
        }
    }

    private static void LTraceWriterStart()
    {
        if (Interlocked.CompareExchange(ref lTraceWriterStarted, 1, 0) != 0)
        {
            return;
        }

        var lTraceThread = new Thread(LTraceWriterRun)
        {
            IsBackground = true,
            Name = "Cadroue.Trace",
            Priority = ThreadPriority.BelowNormal
        };
        lTraceThread.Start();
    }

    private static void LTraceWriterRun()
    {
        LTraceArchiveRun();
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
                else
                {
                    Interlocked.Add(ref lTraceWriterLoss, lTraceBatch.Sum(lTraceWrite => lTraceWrite.LTraceWriteLoss));
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
        lock (lTraceWriterLock)
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
                lTraceStream.Flush(flushToDisk: true);
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

    private static void LTraceArchiveRun()
    {
        lock (lTraceWriterLock)
        {
            LTraceArchiveUpdate();
        }
    }

    private static void LTraceArchiveUpdate()
    {
        try
        {
            string lTraceFolder = LTraceFolderRead();
            if (!Directory.Exists(lTraceFolder))
            {
                return;
            }

            string lTraceCurrent = LTracePathRead();
            foreach (string lTraceStale in Directory.GetFiles(lTraceFolder, "Cadroue-*"))
            {
                if (!lTraceStale.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(lTraceStale, lTraceCurrent, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                LTraceArchiveSave(lTraceStale);
            }

            LTraceStaleRemove(lTraceFolder);
        }
        catch (Exception lTraceException) when (lTraceException is IOException or UnauthorizedAccessException)
        {
        }
    }

    internal static void LTraceArchiveSave(string lTracePath)
    {
        string lTraceTarget = lTracePath + LTraceArchiveSuffix;
        string lTraceTemporary = $"{lTraceTarget}.{Environment.ProcessId}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var lTraceSource = new FileStream(lTracePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var lTraceTargetStream = new FileStream(lTraceTemporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var lTraceGzip = new GZipStream(lTraceTargetStream, CompressionLevel.SmallestSize))
            {
                lTraceSource.CopyTo(lTraceGzip);
            }

            try
            {
                File.Move(lTraceTemporary, lTraceTarget);
            }
            catch (IOException) when (File.Exists(lTraceTarget))
            {
                File.Delete(lTraceTemporary);
            }

            File.Delete(lTracePath);
        }
        catch (Exception lTraceException) when (lTraceException is IOException or UnauthorizedAccessException)
        {
            try
            {
                if (File.Exists(lTraceTemporary))
                {
                    File.Delete(lTraceTemporary);
                }
            }
            catch (Exception lTraceCleanup) when (lTraceCleanup is IOException or UnauthorizedAccessException)
            {
            }
        }
    }

    private static void LTraceStaleRemove(string lTraceFolder)
    {
        List<string> lTraceArchives = Directory
            .GetFiles(lTraceFolder, "Cadroue-*" + LTraceArchiveSuffix)
            .OrderByDescending(lTraceFile => lTraceFile, StringComparer.OrdinalIgnoreCase)
            .ToList();

        DateTime lTraceCutoff = DateTime.UtcNow.AddDays(-LTraceArchiveDays);
        for (int lTraceIndex = 0; lTraceIndex < lTraceArchives.Count; lTraceIndex++)
        {
            string lTraceFile = lTraceArchives[lTraceIndex];
            bool lTraceExcess = lTraceIndex >= LTraceArchiveKeep;
            bool lTraceAged = File.GetLastWriteTimeUtc(lTraceFile) < lTraceCutoff;
            if (!lTraceExcess && !lTraceAged)
            {
                continue;
            }

            try
            {
                File.Delete(lTraceFile);
            }
            catch (Exception lTraceException) when (lTraceException is IOException or UnauthorizedAccessException)
            {
            }
        }
    }
}
