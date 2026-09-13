using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Cadroue.Infrastructure;

public static partial class LTraceWriter
{
    public const string LTraceArchiveSuffix = ".gz";

    private const int LTraceArchiveKeep = 20;
    private const int LTraceArchiveDays = 14;

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
            using (var lTraceTargetStream = new FileStream(
                lTraceTemporary,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
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
