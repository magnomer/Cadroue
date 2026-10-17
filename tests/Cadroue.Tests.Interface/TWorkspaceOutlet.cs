using Cadroue.ShellEngine;
using Xunit;

namespace Cadroue.Tests.Interface;

public sealed class TWorkspaceOutlet
{
    private readonly LWorkspaceOutlet tWorkspaceOutlet = new(TInterface.TInterfaceEngine);

    [Fact]
    public void WorkspaceRemovalRead_IntentRemovedInOtherCase_CancelsTheLoadOnly()
    {
        Assert.Equal((true, false), tWorkspaceOutlet.LWorkspaceRemovalRead(["C:\\A.MP4"], "c:\\a.mp4", "c:\\b.mp4"));
    }

    [Fact]
    public void WorkspaceRemovalRead_SourceRemoved_ClosesTheMediaOnly()
    {
        Assert.Equal((false, true), tWorkspaceOutlet.LWorkspaceRemovalRead(["b"], "a", "b"));
    }

    [Fact]
    public void WorkspaceRemovalRead_NoViewerPaths_DecidesNothing()
    {
        Assert.Equal((false, false), tWorkspaceOutlet.LWorkspaceRemovalRead(["a"], null, null));
    }

    [Fact]
    public void WorkspaceClearRead_NothingProtected_ClosesAndClearsOnlyLoadedMedia()
    {
        Assert.Equal((true, false, true), tWorkspaceOutlet.LWorkspaceClearRead([], "a", null, true));
        Assert.Equal((true, false, false), tWorkspaceOutlet.LWorkspaceClearRead([], null, null, false));
    }

    [Fact]
    public void WorkspaceClearRead_SourceProtectedIntentNot_CancelsTheLoad()
    {
        Assert.Equal((false, true, true), tWorkspaceOutlet.LWorkspaceClearRead(["A"], "a", "b", true));
    }

    [Fact]
    public void WorkspaceClearRead_AllProtected_KeepsTheMedia()
    {
        Assert.Equal((false, false, false), tWorkspaceOutlet.LWorkspaceClearRead(["a", "b"], "a", "b", true));
    }

    [Fact]
    public void WorkspacePathResolve_RelativeBlankOrInvalid_GivesFullEmptyOrTheSamePath()
    {
        Assert.Equal(Path.GetFullPath("clip.mp4"), tWorkspaceOutlet.LWorkspacePathResolve("clip.mp4"));
        Assert.Equal(string.Empty, tWorkspaceOutlet.LWorkspacePathResolve("  "));
        Assert.Equal("a\0b", tWorkspaceOutlet.LWorkspacePathResolve("a\0b"));
    }
}
