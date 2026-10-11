using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace Cadroue.Infrastructure;

public sealed record LTraceReadResult<LTracePayload>(
    bool LTraceReadSuccess,
    LTracePayload LTraceReadValue,
    string LTraceReadError);

public static partial class LTraceWriter
{
    public static string LTraceWriterRead() => LTraceWriterRead(out _).LTraceReadValue;

    internal static LTraceReadResult<string> LTraceWriterRead(out long lTraceCommitted)
    {
        LTraceWriterPersist();
        string lTracePath = LTracePathRead();
        LTraceReadResult<string> lTraceResult;
        lock (lTraceWriterLock)
        {
            try
            {
                using var lTraceStream = new FileStream(
                    lTracePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var lTraceReader = new StreamReader(lTraceStream, Encoding.UTF8);
                lTraceResult = new LTraceReadResult<string>(true, lTraceReader.ReadToEnd(), string.Empty);
            }
            catch (Exception lTraceException)
                when (lTraceException is FileNotFoundException or DirectoryNotFoundException)
            {
                lTraceResult = new LTraceReadResult<string>(true, string.Empty, string.Empty);
            }
            catch (Exception lTraceException) when (lTraceException is IOException or UnauthorizedAccessException)
            {
                lTraceResult = new LTraceReadResult<string>(false, string.Empty, lTraceException.Message);
            }

            lTraceCommitted = Volatile.Read(ref lTraceWriterCommitted);
        }

        return LTraceReadRecord(lTraceResult, $"Log could not be read: {lTracePath}");
    }

    public static LTraceReadResult<List<string>> LTraceFilesRead()
    {
        string lTraceFolder = LTraceFolderRead();
        try
        {
            List<string> lTraceFiles = Directory.GetFiles(lTraceFolder, "Cadroue-*")
                .Where(LTraceFileCheck)
                .OrderByDescending(lTraceFile => lTraceFile, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new LTraceReadResult<List<string>>(true, lTraceFiles, string.Empty);
        }
        catch (DirectoryNotFoundException)
        {
            return new LTraceReadResult<List<string>>(true, new List<string>(), string.Empty);
        }
        catch (Exception lTraceException) when (lTraceException is IOException or UnauthorizedAccessException)
        {
            return LTraceReadRecord(
                new LTraceReadResult<List<string>>(false, new List<string>(), lTraceException.Message),
                $"Log sessions could not be listed: {lTraceFolder}");
        }
    }

    public static LTraceReadResult<string> LTraceFileRead(string lTracePath)
    {
        if (string.Equals(lTracePath, LTracePathRead(), StringComparison.OrdinalIgnoreCase))
        {
            return LTraceWriterRead(out _);
        }

        LTraceReadResult<string> lTraceResult;
        lock (lTraceWriterLock)
        {
            try
            {
                using var lTraceStream = new FileStream(
                    lTracePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                Stream lTraceContent = lTracePath.EndsWith(LTraceArchiveSuffix, StringComparison.OrdinalIgnoreCase)
                    ? new GZipStream(lTraceStream, CompressionMode.Decompress)
                    : lTraceStream;
                using (lTraceContent)
                using (var lTraceReader = new StreamReader(lTraceContent, Encoding.UTF8))
                {
                    lTraceResult = new LTraceReadResult<string>(true, lTraceReader.ReadToEnd(), string.Empty);
                }
            }
            catch (Exception lTraceException)
                when (lTraceException is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                lTraceResult = new LTraceReadResult<string>(false, string.Empty, lTraceException.Message);
            }
        }

        return LTraceReadRecord(lTraceResult, $"Log could not be read: {lTracePath}");
    }

    private static LTraceReadResult<LTracePayload> LTraceReadRecord<LTracePayload>(
        LTraceReadResult<LTracePayload> lTraceResult,
        string lTraceSubject)
    {
        if (!lTraceResult.LTraceReadSuccess)
        {
            LTrace.LTraceRecord(LTraceKind.LTraceError, $"{lTraceSubject}: {lTraceResult.LTraceReadError}");
        }

        return lTraceResult;
    }

    private static bool LTraceFileCheck(string lTracePath) =>
        lTracePath.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
        || lTracePath.EndsWith(".log" + LTraceArchiveSuffix, StringComparison.OrdinalIgnoreCase);
}
