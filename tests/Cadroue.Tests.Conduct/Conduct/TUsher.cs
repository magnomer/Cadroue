using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TUsher
{
    private readonly TUsherPort tUsherPort = new();
    private readonly CUsher tUsherArea;

    public TUsher()
    {
        tUsherArea = new CAtelier(tUsherPort).CAtelierUsher;
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
        Assert.Equal("a.name", tUsherArea.CUsherNameRead("a"));
        Assert.Equal(["name:a"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherStemRead_Path_ReturnsThePortStem()
    {
        Assert.Equal("a.stem", tUsherArea.CUsherStemRead("a"));
        Assert.Equal(["stem:a"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherExtensionRead_Path_ReturnsThePortExtension()
    {
        Assert.Equal("a.extension", tUsherArea.CUsherExtensionRead("a"));
        Assert.Equal(["extension:a"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherProgramResolve_Name_ReturnsThePortPath()
    {
        Assert.Equal("n.program", tUsherArea.CUsherProgramResolve("n"));
        Assert.Equal(["program:n"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherPathResolve_Path_ReturnsThePortPath()
    {
        Assert.Equal("a.path", tUsherArea.CUsherPathResolve("a"));
        Assert.Equal(["path:a"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherPathOpen_WithoutFallback_PassesNullAndReturnsTheFailure()
    {
        Assert.Equal("a|Label|.failed", tUsherArea.CUsherPathOpen("a", "Label"));
        Assert.Equal(["reveal:a|Label|"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherFolderOpen_Folder_ReturnsTheFailure()
    {
        Assert.Equal("f|Label.failed", tUsherArea.CUsherFolderOpen("f", "Label"));
        Assert.Equal(["explorer:f|Label"], tUsherPort.TUsherCalls);
    }

    [Fact]
    public void UsherLinkOpen_Target_ReturnsTheFailure()
    {
        Assert.Equal("t|Label.failed", tUsherArea.CUsherLinkOpen("t", "Label"));
        Assert.Equal(["link:t|Label"], tUsherPort.TUsherCalls);
    }
}
