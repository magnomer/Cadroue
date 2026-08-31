using System.IO.Compression;
using System.Text;

using Cadroue.Infrastructure;

namespace Cadroue.Tests;

internal sealed record TLogCommitResult(bool TLogObserved, string? TLogText);
internal sealed record TLogCallbackResult(bool TLogObserved, bool TLogPersisted);
internal sealed record TLogMoveResult(bool TLogMoved, string TLogRoot, bool TLogSourceFlag, string TLogText);
internal sealed record TLogPersistResult(bool TLogPersisted, TimeSpan TLogElapsed);
internal sealed record TLogArchiveResult(bool TLogArchiveFlag, string TLogText, int TLogTemporaryCount);
internal sealed record TLogLossResult(string TLogText, string[] TLogSummaries);
internal sealed record TLogReadResult(
    bool TLogEmptySuccess,
    string TLogEmptyText,
    bool TLogCorruptSuccess,
    string TLogCorruptError,
    bool TLogMissingSuccess,
    string TLogMissingError);

internal static class TLogIntegrity
{
    internal static TLogReadResult TLogReadResolve()
    {
        string folder = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "log-read", Guid.NewGuid().ToString("N"));
        string emptyPath = Path.Combine(folder, "Cadroue-empty.log");
        string corruptPath = Path.Combine(folder, "Cadroue-corrupt.log.gz");
        string missingPath = Path.Combine(folder, "Cadroue-missing.log");
        Directory.CreateDirectory(folder);
        File.WriteAllText(emptyPath, string.Empty);
        File.WriteAllText(corruptPath, "not gzip content");

        try
        {
            LTraceReadResult<string> empty = LTraceWriter.LTraceFileRead(emptyPath);
            LTraceReadResult<string> corrupt = LTraceWriter.LTraceFileRead(corruptPath);
            LTraceReadResult<string> missing = LTraceWriter.LTraceFileRead(missingPath);
            return new TLogReadResult(
                empty.LTraceReadSuccess,
                empty.LTraceReadValue,
                corrupt.LTraceReadSuccess,
                corrupt.LTraceReadError,
                missing.LTraceReadSuccess,
                missing.LTraceReadError);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    internal static TLogCommitResult TLogCommitRead()
    {
        string previousRoot = LDepot.LDepotRootRead();
        string root = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "log-commit", Guid.NewGuid().ToString("N"));
        string? observedText = null;
        using var observed = new ManualResetEventSlim();

        void TLogCaptureRead(LTraceEntry entry)
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
            LTrace.LTraceAppend += TLogCaptureRead;
            LTraceLog.LTraceInfoRecord("durable notification");
            return new TLogCommitResult(observed.Wait(TimeSpan.FromSeconds(5)), observedText);
        }
        finally
        {
            LTrace.LTraceAppend -= TLogCaptureRead;
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(previousRoot);
            Directory.Delete(root, recursive: true);
        }
    }

    internal static TLogCallbackResult TLogCallbackPersist()
    {
        string previousRoot = LDepot.LDepotRootRead();
        string root = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "log-callback", Guid.NewGuid().ToString("N"));
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void TLogCaptureRead(LTraceEntry entry) =>
            completion.TrySetResult(LTraceWriter.LTraceWriterPersist(250));

        try
        {
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(root);
            LTrace.LTraceAppend += TLogCaptureRead;
            LTraceLog.LTraceInfoRecord("callback persistence");
            bool observed = completion.Task.Wait(TimeSpan.FromSeconds(5));
            return new TLogCallbackResult(observed, observed && completion.Task.Result);
        }
        finally
        {
            LTrace.LTraceAppend -= TLogCaptureRead;
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(previousRoot);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    internal static TLogMoveResult TWorkspaceMove()
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

    internal static TLogMoveResult TLogConcurrentMove()
    {
        string previousRoot = LDepot.LDepotRootRead();
        string parent = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "log-move-race", Guid.NewGuid().ToString("N"));
        string source = Path.Combine(parent, "source");
        string target = Path.Combine(parent, "target");
        using var moveEntered = new ManualResetEventSlim();
        using var moveContinue = new ManualResetEventSlim();

        try
        {
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(source);
            LTraceLog.LTraceInfoRecord("before concurrent workspace move");
            LTraceWriter.LTraceWriterPersist();

            Task<bool> move = Task.Run(() => LTraceWriter.LTraceRootMove(() =>
            {
                moveEntered.Set();
                if (!moveContinue.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException("Concurrent workspace move test did not continue.");
                }

                LDepot.LDepotMove(source, target);
                LDepot.LDepotRootSet(target);
            }));

            if (!moveEntered.Wait(TimeSpan.FromSeconds(5)))
            {
                throw new TimeoutException("Concurrent workspace move test did not start.");
            }

            LTraceLog.LTraceInfoRecord("during concurrent workspace move");
            moveContinue.Set();
            bool moved = move.GetAwaiter().GetResult();
            LTraceLog.LTraceInfoRecord("after concurrent workspace move");
            string text = LTraceWriter.LTraceWriterRead();
            return new TLogMoveResult(moved, LDepot.LDepotRootRead(), Directory.Exists(source), text);
        }
        finally
        {
            moveContinue.Set();
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(previousRoot);
            if (Directory.Exists(parent))
            {
                Directory.Delete(parent, recursive: true);
            }
        }
    }

    internal static TLogPersistResult TLogTimeoutPersist()
    {
        string previousRoot = LDepot.LDepotRootRead();
        string parent = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "log-timeout", Guid.NewGuid().ToString("N"));
        string blockedRoot = Path.Combine(parent, "blocked");
        Directory.CreateDirectory(parent);
        File.WriteAllText(blockedRoot, "not a directory", Encoding.UTF8);

        try
        {
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(blockedRoot);
            LTraceLog.LTraceInfoRecord("entry awaiting unavailable storage");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool persisted = LTraceWriter.LTraceWriterPersist(25);
            stopwatch.Stop();
            return new TLogPersistResult(persisted, stopwatch.Elapsed);
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

    internal static TLogArchiveResult TLogArchiveRun()
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

    internal static TLogLossResult TLogStorageRestore()
    {
        string previousRoot = LDepot.LDepotRootRead();
        string parent = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "log-loss", Guid.NewGuid().ToString("N"));
        string blockedRoot = Path.Combine(parent, "blocked");
        string recoveredRoot = Path.Combine(parent, "recovered");
        var summaries = new List<string>();

        void TLogCaptureRead(LTraceEntry entry) => summaries.Add(entry.LTraceEntrySummary);

        Directory.CreateDirectory(parent);
        File.WriteAllText(blockedRoot, "not a directory", Encoding.UTF8);
        try
        {
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(blockedRoot);
            LTrace.LTraceAppend += TLogCaptureRead;
            LTraceLog.LTraceInfoRecord("entry that cannot be saved");
            LTraceWriter.LTraceWriterPersist();

            LDepot.LDepotRootSet(recoveredRoot);
            LTraceLog.LTraceInfoRecord("entry after recovery");
            string text = LTraceWriter.LTraceWriterRead();
            return new TLogLossResult(text, summaries.ToArray());
        }
        finally
        {
            LTrace.LTraceAppend -= TLogCaptureRead;
            LTraceWriter.LTraceWriterPersist();
            LDepot.LDepotRootSet(previousRoot);
            if (Directory.Exists(parent))
            {
                Directory.Delete(parent, recursive: true);
            }
        }
    }
}
