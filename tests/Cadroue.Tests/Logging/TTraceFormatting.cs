using System.Globalization;

using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class TTraceFormatting
{
    [Fact]
    public void FirstAndLaterEntries_HaveProductionDeltaAndChronologicalTime()
    {
        using var logging = new TTrace();

        logging.TTraceInfoRecord("first");
        Thread.Sleep(20);
        logging.TTraceInfoRecord("second");

        Assert.Equal("Δ-", logging.TTraceEntries[0].TTraceDelta);
        Assert.Matches(@"^Δ\d+\.\d{3}$", logging.TTraceEntries[1].TTraceDelta);

        TimeOnly first = TimeOnly.ParseExact(logging.TTraceEntries[0].TTraceTime, "HH:mm:ss.fff", CultureInfo.InvariantCulture);
        TimeOnly second = TimeOnly.ParseExact(logging.TTraceEntries[1].TTraceTime, "HH:mm:ss.fff", CultureInfo.InvariantCulture);
        Assert.True(second >= first);
        Assert.True(double.Parse(logging.TTraceEntries[1].TTraceDelta[1..], CultureInfo.InvariantCulture) >= 0);
    }

    [Fact]
    public void LoggingOneCategory_DoesNotMutateAnotherCategory()
    {
        using var logging = new TTrace();

        logging.TTraceWarningRecord("warning text", "warning detail");
        logging.TTraceInfoRecord("information text", "information detail");

        Assert.Equal(("Warning", "warning text", "warning detail"),
            (logging.TTraceEntries[0].TTraceKind, logging.TTraceEntries[0].TTraceSummary, logging.TTraceEntries[0].TTraceDetail));
        Assert.Equal(("Info", "information text", "information detail"),
            (logging.TTraceEntries[1].TTraceKind, logging.TTraceEntries[1].TTraceSummary, logging.TTraceEntries[1].TTraceDetail));
    }

    [Fact]
    public void InteractionEntry_RoundTripsThroughTextFormat()
    {
        using var logging = new TTrace();
        logging.TTraceInteractionRecord("Button activated: Render", "Control: RenderButton");

        TTraceEntry actual = Assert.Single(logging.TTracePersistRead());

        Assert.Equal("Interaction", actual.TTraceKind);
        Assert.Equal("Button activated: Render", actual.TTraceSummary);
        Assert.Equal("Control: RenderButton", actual.TTraceDetail);
    }
}
