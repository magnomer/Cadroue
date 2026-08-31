using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class TTraceVerbose
{
    [Fact]
    public void VerboseCategories_AreSuppressedWhenVerboseIsOff()
    {
        using var logging = new TTrace();

        logging.TTraceUiRecord("hidden ui");
        logging.TTraceWorkRecord("hidden work");
        logging.TTraceFfmpegRecord("hidden ffmpeg");

        Assert.Empty(logging.TTraceEntries);
    }

    [Fact]
    public void EnablingVerbose_MakesProductionVerboseCategoriesObservable()
    {
        using var logging = new TTrace();
        logging.TTraceVerboseSet(true);
        logging.TTraceReset();

        logging.TTraceUiRecord("visible ui");
        logging.TTraceWorkRecord("visible work");
        logging.TTraceFfmpegRecord("visible ffmpeg");

        Assert.Collection(
            logging.TTraceEntries,
            entry => Assert.Equal(("UI", "visible ui"), (entry.TTraceKind, entry.TTraceSummary)),
            entry => Assert.Equal(("Work", "visible work"), (entry.TTraceKind, entry.TTraceSummary)),
            entry => Assert.Equal(("Ffmpeg", "visible ffmpeg"), (entry.TTraceKind, entry.TTraceSummary)));
    }

    [Fact]
    public void DisablingVerboseAgain_RestoresFiltering()
    {
        using var logging = new TTrace();
        logging.TTraceVerboseSet(true);
        logging.TTraceUiRecord("visible before disable");
        logging.TTraceVerboseSet(false);
        logging.TTraceReset();

        logging.TTraceUiRecord("hidden ui");
        logging.TTraceWorkRecord("hidden work");
        logging.TTraceFfmpegRecord("hidden ffmpeg");

        Assert.Empty(logging.TTraceEntries);
    }
}
