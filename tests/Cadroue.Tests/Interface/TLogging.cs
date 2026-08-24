using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("Logging", DisableParallelization = true)]
public sealed class TLoggingCollection
{
}

internal sealed record TLoggingEntry(
    string Time,
    string Delta,
    string Kind,
    string Summary,
    string? Detail,
    double? Span);

internal sealed class TLogging : IDisposable
{
    private readonly List<TLoggingEntry> tEntries = [];
    private readonly string tPreviousDepotRoot;
    private readonly bool tPreviousVerbose;
    private readonly bool tPreviousLoading;
    private readonly string tRoot;
    private bool tDisposed;

    internal TLogging()
    {
        tPreviousDepotRoot = LDepot.LDepotRootRead();
        tPreviousVerbose = LTrace.LTraceVerbose;
        tPreviousLoading = LTrace.LTraceLoading;
        tRoot = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "logging", Guid.NewGuid().ToString("N"));

        LTraceWriter.LTraceWriterPersist();
        LDepot.LDepotRootSet(tRoot);
        LTrace.LTraceAppend += TEntryCapture;
        LTrace.LTraceLoadingSet(false);
        LTrace.LTraceVerbose = false;
        LTraceLog.LTraceClear();
        tEntries.Clear();
    }

    internal IReadOnlyList<TLoggingEntry> Entries
    {
        get
        {
            LTraceWriter.LTraceWriterPersist();
            return tEntries.ToArray();
        }
    }

    internal string IsolatedRoot => tRoot;

    internal IReadOnlyList<TLoggingEntry> PersistedEntriesRead()
    {
        LTraceWriter.LTraceWriterPersist();
        return LTraceEntry.LTraceEntryParse(LTraceLog.LTraceTextRead())
            .Select(TEntryCreate)
            .ToArray();
    }

    internal void Info(string summary, string? detail = null) =>
        LTraceLog.LTraceInfoRecord(summary, detail);

    internal void Loading(string summary, string? detail = null) =>
        LTraceLog.LTraceLoadingRecord(summary, detail);

    internal void Warning(string summary, string? detail = null) =>
        LTraceLog.LTraceWarningRecord(summary, detail);

    internal void Error(string summary) =>
        LTraceLog.LTraceErrorRecord(summary, new InvalidOperationException("test error detail"));

    internal void Interaction(string summary, string? detail = null) =>
        LTraceLog.LTraceInteractionRecord(summary, detail);

    internal void Ui(string summary, string? detail = null) =>
        LTrace.LTraceRecord(LTraceKind.LTraceUi, summary, detail);

    internal void Work(string summary, string? detail = null) =>
        LTrace.LTraceRecord(LTraceKind.LTraceWork, summary, detail);

    internal void Ffmpeg(string summary, string? detail = null) =>
        LTrace.LTraceRecord(LTraceKind.LTraceFfmpeg, summary, detail);

    internal void VerboseSet(bool active) => LTrace.LTraceVerbose = active;

    internal void LoadingSet(bool active) => LTrace.LTraceLoadingSet(active);

    internal void Draw(string surface, string trigger, double milliseconds, int glyphCount = 0) =>
        LTrace.LTraceDrawAdd(surface, trigger, milliseconds, glyphCount);

    internal void TimelineDraw(
        string surface,
        TimeSpan cursor,
        string? sourcePath,
        string trigger,
        double milliseconds,
        int glyphCount = 0) =>
        LTrace.LTraceTimelineAdd(surface, cursor, sourcePath, trigger, milliseconds, glyphCount);

    internal void DrawFlush() => LTrace.LTraceDrawTick();

    internal void Reset()
    {
        LTraceWriter.LTraceWriterPersist();
        LTrace.LTraceReset();
        tEntries.Clear();
    }

    internal bool IsolatedLogExists()
    {
        LTraceWriter.LTraceWriterPersist();
        return File.Exists(LTraceLog.LTracePathFind());
    }

    public void Dispose()
    {
        if (tDisposed)
        {
            return;
        }

        tDisposed = true;
        LTrace.LTraceAppend -= TEntryCapture;
        LTrace.LTraceReset();
        LTrace.LTraceLoadingSet(tPreviousLoading);
        LTrace.LTraceVerbose = tPreviousVerbose;
        LTraceWriter.LTraceWriterPersist();
        LDepot.LDepotRootSet(tPreviousDepotRoot);

        try
        {
            if (Directory.Exists(tRoot))
            {
                Directory.Delete(tRoot, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void TEntryCapture(LTraceEntry entry)
    {
        tEntries.Add(TEntryCreate(entry));
    }

    private static TLoggingEntry TEntryCreate(LTraceEntry entry) =>
        new(
            entry.LTraceEntryTime,
            entry.LTraceEntryDelta,
            LTraceEntry.LTraceKindRead(entry.LTraceEntryKind),
            entry.LTraceEntrySummary,
            entry.LTraceEntryDetail,
            entry.LTraceEntrySpan);
}
