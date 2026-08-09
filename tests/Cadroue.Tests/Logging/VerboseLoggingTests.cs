using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class VerboseLoggingTests
{
    [Fact]
    public void VerboseCategories_AreSuppressedWhenVerboseIsOff()
    {
        using var logging = new TLogging();

        logging.Ui("hidden ui");
        logging.Work("hidden work");
        logging.Ffmpeg("hidden ffmpeg");

        Assert.Empty(logging.Entries);
    }

    [Fact]
    public void EnablingVerbose_MakesProductionVerboseCategoriesObservable()
    {
        using var logging = new TLogging();
        logging.VerboseSet(true);
        logging.Reset();

        logging.Ui("visible ui");
        logging.Work("visible work");
        logging.Ffmpeg("visible ffmpeg");

        Assert.Collection(
            logging.Entries,
            entry => Assert.Equal(("UI", "visible ui"), (entry.Kind, entry.Summary)),
            entry => Assert.Equal(("Work", "visible work"), (entry.Kind, entry.Summary)),
            entry => Assert.Equal(("Ffmpeg", "visible ffmpeg"), (entry.Kind, entry.Summary)));
    }

    [Fact]
    public void DisablingVerboseAgain_RestoresFiltering()
    {
        using var logging = new TLogging();
        logging.VerboseSet(true);
        logging.Ui("visible before disable");
        logging.VerboseSet(false);
        logging.Reset();

        logging.Ui("hidden ui");
        logging.Work("hidden work");
        logging.Ffmpeg("hidden ffmpeg");

        Assert.Empty(logging.Entries);
    }
}
