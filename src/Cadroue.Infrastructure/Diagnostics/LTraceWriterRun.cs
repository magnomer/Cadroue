using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Cadroue.Infrastructure;

public static partial class LTraceWriter
{
    private const int LTraceWriterIdle = 250;
    private const int LTraceWriterBatch = 256;
    private const int LTraceWriterRetry = 8;

    private static FileStream? lTraceWriterStream;
    private static string? lTraceWriterPath;

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
        Volatile.Write(ref lTraceWriterThread, Environment.CurrentManagedThreadId);
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
}
