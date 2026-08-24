using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class LoadingLoggingTests
{
    [Fact]
    public void LoadingMode_RecordsOnlyIntentionalLoadingEntriesAndFailures()
    {
        using var logging = new TLogging();
        logging.LoadingSet(true);
        logging.VerboseSet(true);

        logging.Info("load information");
        logging.Ui("load ui");
        logging.Work("load work");
        logging.Ffmpeg("load ffmpeg");
        logging.Loading("scene loaded", "load detail");
        logging.Warning("load warning");

        Assert.Collection(
            logging.Entries,
            entry => Assert.Equal(("Loading", "scene loaded", "load detail"), (entry.Kind, entry.Summary, entry.Detail)),
            entry => Assert.Equal(("Warning", "load warning"), (entry.Kind, entry.Summary)));
    }

    [Fact]
    public void LeavingLoadingMode_RestoresNormalClassification()
    {
        using var logging = new TLogging();
        logging.LoadingSet(true);
        logging.Info("during loading");
        logging.LoadingSet(false);
        logging.Info("after loading");

        Assert.Single(logging.Entries);
        Assert.Equal("Info", logging.Entries[0].Kind);
        Assert.Equal("after loading", logging.Entries[0].Summary);
    }
}
