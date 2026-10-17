using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Cadroue.Infrastructure;

public sealed record LTraceReadResult<LTracePayload>(
    bool LTraceReadSuccess,
    LTracePayload LTraceReadValue,
    string LTraceReadError);

public static class LTraceReader
{
    public static string LTraceTextRead() => LTraceTextRead(out _).LTraceReadValue;

    internal static LTraceReadResult<string> LTraceTextRead(out long lTraceCommitted)
    {
        LTraceWriter.LTraceWriterPersist();
        lock (LTraceFolder.LTraceFolderLock)
        {
            try
            {
                string lTracePath = LTraceFolder.LTracePathRead();
                using var lTraceStream = new FileStream(
                    lTracePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var lTraceReader = new StreamReader(lTraceStream, Encoding.UTF8);
                string lTraceText = lTraceReader.ReadToEnd();
                lTraceCommitted = LTraceWriter.LTraceWriterCommitted;
                return new LTraceReadResult<string>(true, lTraceText, string.Empty);
            }
            catch (Exception lTraceException)
                when (lTraceException is FileNotFoundException or DirectoryNotFoundException)
            {
                lTraceCommitted = LTraceWriter.LTraceWriterCommitted;
                return new LTraceReadResult<string>(true, string.Empty, string.Empty);
            }
            catch (Exception lTraceException) when (lTraceException is IOException or UnauthorizedAccessException)
            {
                lTraceCommitted = LTraceWriter.LTraceWriterCommitted;
                return new LTraceReadResult<string>(false, string.Empty, lTraceException.Message);
            }
        }
    }

    public static LTraceReadResult<List<string>> LTraceFilesRead()
    {
        try
        {
            string lTraceFolder = LTraceFolder.LTraceFolderRead();
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
            return new LTraceReadResult<List<string>>(false, new List<string>(), lTraceException.Message);
        }
    }

    public static LTraceReadResult<string> LTraceFileRead(string lTracePath)
    {
        if (string.Equals(lTracePath, LTraceFolder.LTracePathRead(), StringComparison.OrdinalIgnoreCase))
        {
            return LTraceTextRead(out _);
        }

        lock (LTraceFolder.LTraceFolderLock)
        {
            try
            {
                using var lTraceStream = new FileStream(
                    lTracePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                Stream lTraceContent = lTracePath.EndsWith(
                    LTraceArchive.LTraceArchiveSuffix,
                    StringComparison.OrdinalIgnoreCase)
                    ? new GZipStream(lTraceStream, CompressionMode.Decompress)
                    : lTraceStream;
                using (lTraceContent)
                using (var lTraceReader = new StreamReader(lTraceContent, Encoding.UTF8))
                {
                    return new LTraceReadResult<string>(true, lTraceReader.ReadToEnd(), string.Empty);
                }
            }
            catch (Exception lTraceException)
                when (lTraceException is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                return new LTraceReadResult<string>(false, string.Empty, lTraceException.Message);
            }
        }
    }

    private static bool LTraceFileCheck(string lTracePath) =>
        lTracePath.EndsWith(".log", StringComparison.OrdinalIgnoreCase)
        || lTracePath.EndsWith(".log" + LTraceArchive.LTraceArchiveSuffix, StringComparison.OrdinalIgnoreCase);
}
