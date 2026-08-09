using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class LoadingLoggingTests
{
    [Fact]
    public void LoadingMode_ClassifiesInfoAndUiAsLoading()
    {
        using var logging = new TLogging();
        logging.LoadingSet(true);

        logging.Info("load information");
        logging.Ui("load ui");
        logging.Warning("load warning");

        Assert.Collection(
            logging.Entries,
            entry => Assert.Equal(("Loading", "load information"), (entry.Kind, entry.Summary)),
            entry => Assert.Equal(("Loading", "load ui"), (entry.Kind, entry.Summary)),
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

        Assert.Equal("Loading", logging.Entries[0].Kind);
        Assert.Equal("Info", logging.Entries[1].Kind);
        Assert.Equal("after loading", logging.Entries[1].Summary);
    }
}
