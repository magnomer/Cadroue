using System.Globalization;

using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class LogFormattingTests
{
    [Fact]
    public void FirstAndLaterEntries_HaveProductionDeltaAndChronologicalTime()
    {
        using var logging = new TLogging();

        logging.Info("first");
        Thread.Sleep(20);
        logging.Info("second");

        Assert.Equal("Δ-", logging.Entries[0].Delta);
        Assert.Matches(@"^Δ\d+\.\d{3}$", logging.Entries[1].Delta);

        TimeOnly first = TimeOnly.ParseExact(logging.Entries[0].Time, "HH:mm:ss.fff", CultureInfo.InvariantCulture);
        TimeOnly second = TimeOnly.ParseExact(logging.Entries[1].Time, "HH:mm:ss.fff", CultureInfo.InvariantCulture);
        Assert.True(second >= first);
        Assert.True(double.Parse(logging.Entries[1].Delta[1..], CultureInfo.InvariantCulture) >= 0);
    }

    [Fact]
    public void LoggingOneCategory_DoesNotMutateAnotherCategory()
    {
        using var logging = new TLogging();

        logging.Warning("warning text", "warning detail");
        logging.Info("information text", "information detail");

        Assert.Equal(("Warning", "warning text", "warning detail"),
            (logging.Entries[0].Kind, logging.Entries[0].Summary, logging.Entries[0].Detail));
        Assert.Equal(("Info", "information text", "information detail"),
            (logging.Entries[1].Kind, logging.Entries[1].Summary, logging.Entries[1].Detail));
    }

    [Fact]
    public void InteractionEntry_RoundTripsThroughTextFormat()
    {
        using var logging = new TLogging();
        logging.Interaction("Button activated: Render", "Control: RenderButton");

        TLoggingEntry actual = Assert.Single(logging.PersistedEntriesRead());

        Assert.Equal("Interaction", actual.Kind);
        Assert.Equal("Button activated: Render", actual.Summary);
        Assert.Equal("Control: RenderButton", actual.Detail);
    }
}
