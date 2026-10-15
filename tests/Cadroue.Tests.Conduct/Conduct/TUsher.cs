using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TUsher
{
    private readonly TUsherPort tUsherPort = new();
    private readonly CUsher tUsherArea;

    public TUsher()
    {
        tUsherArea = new CAtelier(tUsherPort, new TLineagePort(), new TWorkspacePort(), new TPlacementPort())
            .CAtelierUsher;
    }

    [Fact]
    public void UsherFileExist_PresentPath_AsksThePortOnce()
    {
        Assert.True(tUsherArea.CUsherFileExist("present"));
        Assert.Equal(["file:present"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherFolderExist_MissingFolder_ReturnsFalse()
    {
        Assert.False(tUsherArea.CUsherFolderExist("missing"));
        Assert.Equal(["folder:missing"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherNameRead_Path_ReturnsThePortName()
    {
        Assert.Equal("d/a.b.name", tUsherArea.CUsherNameRead("d/a.b"));
        Assert.Equal(["name:d/a.b"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherStemRead_Path_ReturnsThePortStem()
    {
        Assert.Equal("d/a.b.stem", tUsherArea.CUsherStemRead("d/a.b"));
        Assert.Equal(["stem:d/a.b"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherProgramOpen_Name_AnswersTheResolvedPathAndTheFailure()
    {
        (string path, string? failure) = tUsherArea.CUsherProgramOpen("n");
        Assert.Equal("n.program", path);
        Assert.Equal("n.program.failed", failure);
        Assert.Equal(["program:n", "link:n.program"], tUsherPort.TUsherCalls);
        Assert.Equal([null], tUsherPort.TUsherFailures);
    }

    [Fact]
    public void UsherProgramOpen_FailureText_HandsTheCallerTextToThePort()
    {
        tUsherArea.CUsherProgramOpen("{n}", "Notice could not be opened: {0}: {1}");
        Assert.Equal(["Notice could not be opened: {0}: {1}"], tUsherPort.TUsherFailures);
    }
    [Fact]
    public void UsherPathOpen_WithoutFallback_PassesNullAndReturnsTheFailure()
    {
        Assert.Equal("a|.failed", tUsherArea.CUsherPathOpen("a"));
        Assert.Equal(["reveal:a|"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherPathOpen_FailureText_HandsTheCallerTextToThePort()
    {
        Assert.Equal("{a}|.failed", tUsherArea.CUsherPathOpen("{a}", failure: "Could not open '{0}': {1}"));
        Assert.Equal(["Could not open '{0}': {1}"], tUsherPort.TUsherFailures);
    }

    [Fact]
    public void UsherPathOpen_WithoutFailureText_HandsNullToThePort()
    {
        Assert.Equal("a|.failed", tUsherArea.CUsherPathOpen("a"));
        Assert.Equal([null], tUsherPort.TUsherFailures);
    }
    [Fact]
    public void UsherFolderOpen_Folder_ReturnsTheFailure()
    {
        Assert.Equal("f.failed", tUsherArea.CUsherFolderOpen("f"));
        Assert.Equal(["explorer:f"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherLinkOpen_Target_ReturnsTheFailure()
    {
        Assert.Equal("t.failed", tUsherArea.CUsherLinkOpen("t"));
        Assert.Equal(["link:t"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherLinkOpen_FailureText_HandsTheCallerTextToThePort()
    {
        Assert.Equal("{t}.failed", tUsherArea.CUsherLinkOpen("{t}", "Link could not be opened: {0}: {1}"));
        Assert.Equal(["Link could not be opened: {0}: {1}"], tUsherPort.TUsherFailures);
    }

    [Fact]
    public void UsherLinkOpen_WithoutFailureText_HandsNullToThePort()
    {
        Assert.Equal("t.failed", tUsherArea.CUsherLinkOpen("t"));
        Assert.Equal([null], tUsherPort.TUsherFailures);
    }
}
