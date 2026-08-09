using Xunit;

namespace Cadroue.Tests;

[Collection("Logging")]
public sealed class LoggingVisibilityTests
{
    [Fact]
    public void NormalSeverityEntries_AreEmittedWithTheirProductionKinds()
    {
        using var logging = new TLogging();

        logging.Info("ordinary information");
        logging.Warning("ordinary warning");
        logging.Error("ordinary error");

        Assert.Collection(
            logging.Entries,
            entry =>
            {
                Assert.Equal("Info", entry.Kind);
                Assert.Equal("ordinary information", entry.Summary);
            },
            entry =>
            {
                Assert.Equal("Warning", entry.Kind);
                Assert.Equal("ordinary warning", entry.Summary);
            },
            entry =>
            {
                Assert.Equal("Error", entry.Kind);
                Assert.Equal("ordinary error", entry.Summary);
                Assert.Contains("test error detail", entry.Detail);
            });

        Assert.True(logging.IsolatedLogExists());
        Assert.Contains(Path.Combine("Cadroue.Tests", "logging"), logging.IsolatedRoot);
    }
}
