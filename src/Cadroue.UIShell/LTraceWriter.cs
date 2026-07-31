using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Cadroue.Core;

namespace Cadroue.UIShell;

public static class LTraceWriter
{
    private const int LTraceWriterCapacity = 20000;
    private const int LTraceWriterIdle = 250;
    private const int LTraceWriterBatch = 256;

    public const string LTraceFolderName = "log";

    private static readonly BlockingCollection<string> lTraceWriterQueue =
        new(new ConcurrentQueue<string>(), LTraceWriterCapacity);

    private static readonly ManualResetEventSlim lTraceWriterIdle = new(true);
    private static readonly object lTraceWriterLock = new();

    private static readonly string lTraceFileName =
        $"Cadroue-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.log";

    private static StreamWriter? lTraceWriterStream;
    private static string? lTraceWriterPath;
    private static int lTraceWriterDrop;
    private static int lTraceWriterStarted;

    public static string LTraceFolderRead() => Path.Combine(LDepot.LDepotRootRead(), LTraceFolderName);

    public static string LTracePathRead() => Path.Combine(LTraceFolderRead(), lTraceFileName);

    public static void LTraceWriterRecord(string lTraceEntry)
    {
        if (string.IsNullOrEmpty(lTraceEntry))
        {
            return;
        }

        LTraceWriterStart();
        lTraceWriterIdle.Reset();
        if (!lTraceWriterQueue.TryAdd(lTraceEntry))
        {
            Interlocked.Increment(ref lTraceWriterDrop);
        }
    }

    public static void LTraceWriterPersist()
    {
        if (Volatile.Read(ref lTraceWriterStarted) == 0)
        {
            return;
        }

        lTraceWriterIdle.Wait(2000);
    }

    public static string LTraceWriterRead()
    {
        LTraceWriterPersist();
        lock (lTraceWriterLock)
        {
            try
            {
                string lTracePath = LTracePathRead();
                if (!File.Exists(lTracePath))
                {
                    return string.Empty;
                }

                using var lTraceStream = new FileStream(
                    lTracePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var lTraceReader = new StreamReader(lTraceStream, Encoding.UTF8);
                return lTraceReader.ReadToEnd();
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
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

            return Directory.GetFiles(lTraceFolder, "Cadroue-*.log")
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
                using var lTraceReader = new StreamReader(lTraceStream, Encoding.UTF8);
                return lTraceReader.ReadToEnd();
            }
            catch (Exception lTraceException) when (lTraceException is IOException or UnauthorizedAccessException)
            {
                return string.Empty;
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
        while (true)
        {
            try
            {
                if (!lTraceWriterQueue.TryTake(out string? lTraceEntry, LTraceWriterIdle))
                {
                    LTracePersistRun();
                    lTraceWriterIdle.Set();
                    continue;
                }

                lock (lTraceWriterLock)
                {
                    StreamWriter? lTraceStream = LTraceWriterOpen();
                    if (lTraceStream is null)
                    {
                        continue;
                    }

                    int lTraceBatch = 0;
                    do
                    {
                        lTraceStream.Write(lTraceEntry);
                        lTraceBatch++;
                    }
                    while (lTraceBatch < LTraceWriterBatch
                        && lTraceWriterQueue.TryTake(out lTraceEntry, 0));

                    int lTraceDropped = Interlocked.Exchange(ref lTraceWriterDrop, 0);
                    if (lTraceDropped > 0)
                    {
                        lTraceStream.Write(
                            $"{new string(' ', 30)}[{lTraceDropped} entries dropped: the trace queue was full]{Environment.NewLine}");
                    }
                }
            }
            catch (Exception)
            {
                Thread.Sleep(LTraceWriterIdle);
            }
        }
    }

    private static void LTracePersistRun()
    {
        lock (lTraceWriterLock)
        {
            try
            {
                lTraceWriterStream?.Flush();
            }
            catch (IOException)
            {
                LTraceWriterClose();
            }
        }
    }

    private static StreamWriter? LTraceWriterOpen()
    {
        string lTracePath = LTracePathRead();
        if (lTraceWriterStream is not null
            && string.Equals(lTraceWriterPath, lTracePath, StringComparison.OrdinalIgnoreCase))
        {
            return lTraceWriterStream;
        }

        LTraceWriterClose();
        try
        {
            Directory.CreateDirectory(LTraceFolderRead());
            var lTraceStream = new FileStream(
                lTracePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            lTraceWriterStream = new StreamWriter(lTraceStream, new UTF8Encoding(true))
            {
                AutoFlush = false
            };
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
            lTraceWriterStream?.Flush();
            lTraceWriterStream?.Dispose();
        }
        catch (IOException)
        {
        }

        lTraceWriterStream = null;
        lTraceWriterPath = null;
    }
}
