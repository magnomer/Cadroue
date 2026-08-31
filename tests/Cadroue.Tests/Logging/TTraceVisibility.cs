using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class TTraceVisibility
{
    [Fact]
    public void NormalSeverityEntries_AreEmittedWithTheirProductionKinds()
    {
        using var logging = new TTrace();

        logging.TTraceInfoRecord("ordinary information");
        logging.TTraceWarningRecord("ordinary warning");
        logging.TTraceErrorRecord("ordinary error");
        logging.TTraceInteractionRecord("button activated", "Button: Render");

        Assert.Collection(
            logging.TTraceEntries,
            entry =>
            {
                Assert.Equal("Info", entry.TTraceKind);
                Assert.Equal("ordinary information", entry.TTraceSummary);
            },
            entry =>
            {
                Assert.Equal("Warning", entry.TTraceKind);
                Assert.Equal("ordinary warning", entry.TTraceSummary);
            },
            entry =>
            {
                Assert.Equal("Error", entry.TTraceKind);
                Assert.Equal("ordinary error", entry.TTraceSummary);
                Assert.Contains("test error detail", entry.TTraceDetail);
            },
            entry =>
            {
                Assert.Equal("Interaction", entry.TTraceKind);
                Assert.Equal("button activated", entry.TTraceSummary);
                Assert.Equal("Button: Render", entry.TTraceDetail);
            });

        Assert.True(logging.TTraceLogExist());
        Assert.Contains(Path.Combine("Cadroue.Tests", "logging"), logging.TTraceIsolatedRoot);
    }
}
