using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("Logging", DisableParallelization = true)]
public sealed class TTraceCollection
{
}

internal sealed record TTraceEntry(
    string TTraceTime,
    string TTraceDelta,
    string TTraceKind,
    string TTraceSummary,
    string? TTraceDetail,
    double? TTraceSpan);

internal sealed class TTrace : IDisposable
{
    private readonly List<TTraceEntry> tTraceEntries = [];
    private readonly string tTraceDepotRoot;
    private readonly bool tTraceVerbose;
    private readonly bool tTraceLoading;
    private readonly string tTraceRoot;
    private bool tTraceDisposed;

    internal TTrace()
    {
        tTraceDepotRoot = LDepot.LDepotRootRead();
        tTraceVerbose = LTrace.LTraceVerbose;
        tTraceLoading = LTrace.LTraceLoading;
        tTraceRoot = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "logging", Guid.NewGuid().ToString("N"));

        LTraceWriter.LTraceWriterPersist();
        LDepot.LDepotRootSet(tTraceRoot);
        LTrace.LTraceAppend += TTraceEntryRead;
        LTrace.LTraceLoadingSet(false);
        LTrace.LTraceVerbose = false;
        LTraceLog.LTraceClear();
        tTraceEntries.Clear();
    }

    internal IReadOnlyList<TTraceEntry> TTraceEntries
    {
        get
        {
            LTraceWriter.LTraceWriterPersist();
            return tTraceEntries.ToArray();
        }
    }

    internal string TTraceIsolatedRoot => tTraceRoot;

    internal IReadOnlyList<TTraceEntry> TTracePersistRead()
    {
        LTraceWriter.LTraceWriterPersist();
        return LTraceEntry.LTraceEntryParse(LTraceLog.LTraceTextRead())
            .Select(TEntryCreate)
            .ToArray();
    }

    internal void TTraceInfoRecord(string summary, string? detail = null) =>
        LTraceLog.LTraceInfoRecord(summary, detail);

    internal void TTraceLoadingRecord(string summary, string? detail = null) =>
        LTraceLog.LTraceLoadingRecord(summary, detail);

    internal void TTraceWarningRecord(string summary, string? detail = null) =>
        LTraceLog.LTraceWarningRecord(summary, detail);

    internal void TTraceErrorRecord(string summary) =>
        LTraceLog.LTraceErrorRecord(summary, new InvalidOperationException("test error detail"));

    internal void TTraceInteractionRecord(string summary, string? detail = null) =>
        LTraceLog.LTraceInteractionRecord(summary, detail);

    internal void TTraceUiRecord(string summary, string? detail = null) =>
        LTrace.LTraceRecord(LTraceKind.LTraceUi, summary, detail);

    internal void TTraceWorkRecord(string summary, string? detail = null) =>
        LTrace.LTraceRecord(LTraceKind.LTraceWork, summary, detail);

    internal void TTraceFfmpegRecord(string summary, string? detail = null) =>
        LTrace.LTraceRecord(LTraceKind.LTraceFfmpeg, summary, detail);

    internal void TTraceVerboseSet(bool active) => LTrace.LTraceVerbose = active;

    internal void TTraceLoadingSet(bool active) => LTrace.LTraceLoadingSet(active);

    internal void TTraceDraw(string surface, string trigger, double milliseconds, int glyphCount = 0) =>
        LTrace.LTraceDrawAdd(surface, trigger, milliseconds, glyphCount);

    internal void TTimelineDraw(
        string surface,
        TimeSpan cursor,
        string? sourcePath,
        string trigger,
        double milliseconds,
        int glyphCount = 0) =>
        LTrace.LTraceTimelineAdd(surface, cursor, sourcePath, trigger, milliseconds, glyphCount);

    internal void TTraceDrawCommit() => LTrace.LTraceDrawTick();

    internal void TTraceReset()
    {
        LTraceWriter.LTraceWriterPersist();
        LTrace.LTraceReset();
        tTraceEntries.Clear();
    }

    internal bool TTraceLogExist()
    {
        LTraceWriter.LTraceWriterPersist();
        return File.Exists(LTraceLog.LTracePathFind());
    }

    public void Dispose()
    {
        if (tTraceDisposed)
        {
            return;
        }

        tTraceDisposed = true;
        LTrace.LTraceAppend -= TTraceEntryRead;
        LTrace.LTraceReset();
        LTrace.LTraceLoadingSet(tTraceLoading);
        LTrace.LTraceVerbose = tTraceVerbose;
        LTraceWriter.LTraceWriterPersist();
        LDepot.LDepotRootSet(tTraceDepotRoot);

        try
        {
            if (Directory.Exists(tTraceRoot))
            {
                Directory.Delete(tTraceRoot, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void TTraceEntryRead(LTraceEntry entry)
    {
        tTraceEntries.Add(TEntryCreate(entry));
    }

    private static TTraceEntry TEntryCreate(LTraceEntry entry) =>
        new(
            entry.LTraceEntryTime,
            entry.LTraceEntryDelta,
            LTraceEntry.LTraceKindRead(entry.LTraceEntryKind),
            entry.LTraceEntrySummary,
            entry.LTraceEntryDetail,
            entry.LTraceEntrySpan);
}
