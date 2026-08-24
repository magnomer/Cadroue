using System.IO.Compression;
using System.Text;

using Cadroue.Infrastructure;

namespace Cadroue.Tests;

internal sealed record TLogCommitResult(bool Observed, string? Text);
internal sealed record TLogMoveResult(bool Moved, string Root, bool SourceExists, string Text);
internal sealed record TLogArchiveResult(bool ArchiveExists, string Text, int TemporaryCount);
internal sealed record TLogLossResult(string Text, string[] Summaries);

internal static class TLogIntegrity
{
    internal static TLogCommitResult CommitObserve()
    {
        string previousRoot = LDepot.LDepotRootRead();
        string root = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "log-commit", Guid.NewGuid().ToString("N"));
        string? observedText = null;
        using var observed = new ManualResetEventSlim();

        void Capture(LTraceEntry entry)
        {
            using var stream = new FileStream(
                LTraceWriter.LTracePathRead(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            observedText = reader.ReadToEnd();
            observed.Set();
        }

        try
        {
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(root);
            LTrace.LTraceAppend += Capture;
            LTraceLog.LTraceInfoRecord("durable notification");
            return new TLogCommitResult(observed.Wait(TimeSpan.FromSeconds(5)), observedText);
        }
        finally
        {
            LTrace.LTraceAppend -= Capture;
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(previousRoot);
            Directory.Delete(root, recursive: true);
        }
    }

    internal static TLogMoveResult WorkspaceMove()
    {
        string previousRoot = LDepot.LDepotRootRead();
        string parent = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "log-move", Guid.NewGuid().ToString("N"));
        string source = Path.Combine(parent, "source");
        string target = Path.Combine(parent, "target");

        try
        {
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(source);
            LTraceLog.LTraceInfoRecord("before workspace move");
            LTraceWriter.LTraceWriterPersist();
            bool moved = LDepot.LDepotFolderMove(source, target);
            LTraceLog.LTraceInfoRecord("after workspace move");
            string text = LTraceWriter.LTraceWriterRead();
            return new TLogMoveResult(moved, LDepot.LDepotRootRead(), Directory.Exists(source), text);
        }
        finally
        {
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(previousRoot);
            if (Directory.Exists(parent))
            {
                Directory.Delete(parent, recursive: true);
            }
        }
    }

    internal static TLogArchiveResult ArchiveConcurrent()
    {
        string folder = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "log-archive", Guid.NewGuid().ToString("N"));
        string source = Path.Combine(folder, "Cadroue-20000101-000000-1.log");
        string archive = source + LTraceWriter.LTraceArchiveSuffix;
        Directory.CreateDirectory(folder);
        File.WriteAllText(source, "archive payload", Encoding.UTF8);

        try
        {
            Parallel.Invoke(
                () => LTraceWriter.LTraceArchiveSave(source),
                () => LTraceWriter.LTraceArchiveSave(source));

            string text = string.Empty;
            if (File.Exists(archive))
            {
                using var stream = File.OpenRead(archive);
                using var gzip = new GZipStream(stream, CompressionMode.Decompress);
                using var reader = new StreamReader(gzip, Encoding.UTF8);
                text = reader.ReadToEnd();
            }

            return new TLogArchiveResult(
                File.Exists(archive),
                text,
                Directory.GetFiles(folder, "*.tmp").Length);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    internal static TLogLossResult StorageRecover()
    {
        string previousRoot = LDepot.LDepotRootRead();
        string parent = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "log-loss", Guid.NewGuid().ToString("N"));
        string blockedRoot = Path.Combine(parent, "blocked");
        string recoveredRoot = Path.Combine(parent, "recovered");
        var summaries = new List<string>();

        void Capture(LTraceEntry entry) => summaries.Add(entry.LTraceEntrySummary);

        Directory.CreateDirectory(parent);
        File.WriteAllText(blockedRoot, "not a directory", Encoding.UTF8);
        try
        {
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(blockedRoot);
            LTrace.LTraceAppend += Capture;
            LTraceLog.LTraceInfoRecord("entry that cannot be saved");
            LTraceWriter.LTraceWriterPersist();

            LDepot.LDepotRootSet(recoveredRoot);
            LTraceLog.LTraceInfoRecord("entry after recovery");
            string text = LTraceWriter.LTraceWriterRead();
            return new TLogLossResult(text, summaries.ToArray());
        }
        finally
        {
            LTrace.LTraceAppend -= Capture;
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(previousRoot);
            if (Directory.Exists(parent))
            {
                Directory.Delete(parent, recursive: true);
            }
        }
    }
}
