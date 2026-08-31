using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class TTraceLoading
{
    [Fact]
    public void LoadingMode_RecordsOnlyIntentionalLoadingEntriesAndFailures()
    {
        using var logging = new TTrace();
        logging.TTraceLoadingSet(true);
        logging.TTraceVerboseSet(true);

        logging.TTraceInfoRecord("load information");
        logging.TTraceUiRecord("load ui");
        logging.TTraceWorkRecord("load work");
        logging.TTraceFfmpegRecord("load ffmpeg");
        logging.TTraceLoadingRecord("scene loaded", "load detail");
        logging.TTraceWarningRecord("load warning");

        Assert.Collection(
            logging.TTraceEntries,
            entry => Assert.Equal(("Loading", "scene loaded", "load detail"), (entry.TTraceKind, entry.TTraceSummary, entry.TTraceDetail)),
            entry => Assert.Equal(("Warning", "load warning"), (entry.TTraceKind, entry.TTraceSummary)));
    }

    [Fact]
    public void LeavingLoadingMode_RestoresNormalClassification()
    {
        using var logging = new TTrace();
        logging.TTraceLoadingSet(true);
        logging.TTraceInfoRecord("during loading");
        logging.TTraceLoadingSet(false);
        logging.TTraceInfoRecord("after loading");

        Assert.Single(logging.TTraceEntries);
        Assert.Equal("Info", logging.TTraceEntries[0].TTraceKind);
        Assert.Equal("after loading", logging.TTraceEntries[0].TTraceSummary);
    }
}
