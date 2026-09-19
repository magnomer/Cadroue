using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TWindowDrop
{
    private static LWindow TWindowBuild(out LStrip strip)
    {
        strip = TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");
        return TInterface.TWindowCreate(strip, body => body());
    }

    private static LStripTab TListTabAdd(LStrip strip)
    {
        LStripTab tab = TInterface.TStripTabCreate("Split");
        TInterface.TStripWorkspaceAttach(tab, TInterface.TPresetInitialCreate("Split"), TInterface.TDocketCreate());
        TInterface.TStripAdd(strip, tab);
        return tab;
    }

    private static string TMediaFileCreate()
    {
        string path = Path.Combine(Path.GetTempPath(), $"cadroue-drop-{Guid.NewGuid():N}.mp4");
        File.WriteAllBytes(path, [0]);
        return path;
    }

    [Fact]
    public void Over_WithoutTab_RefusesButHandles()
    {
        LWindow window = TWindowBuild(out _);
        var data = new object();

        TInterface.TWindowOverHandle(window, TInterface.TWindowDragCreate(["C:/x.mp4"], false, "Grid", true, data));

        Assert.Equal(LWindowDropEffect.LWindowDropNone, TInterface.TWindowEffectRead(window));
        Assert.True(TInterface.TWindowHandledRead(window));
    }

    [Fact]
    public void Over_OnGroup_LeavesTheEventAlone()
    {
        LWindow window = TWindowBuild(out LStrip strip);
        TInterface.TWindowTabSet(window, TListTabAdd(strip), null);

        TInterface.TWindowOverHandle(
            window, TInterface.TWindowDragCreate(["C:/x.mp4"], true, "PGroup", true, new object()));

        Assert.Equal(LWindowDropEffect.LWindowDropNone, TInterface.TWindowEffectRead(window));
        Assert.False(TInterface.TWindowHandledRead(window));
    }

    [Fact]
    public void Drop_OnListTab_HandsMediaPathsToTheList()
    {
        LWindow window = TWindowBuild(out LStrip strip);
        TInterface.TWindowTabSet(window, TListTabAdd(strip), null);
        List<IReadOnlyList<string>> listDrops = [];
        List<string> viewerDrops = [];
        TInterface.TWindowDropAttach(window, listDrops.Add, viewerDrops.Add);

        TInterface.TWindowDropHandle(
            window,
            TInterface.TWindowDragCreate(["C:/clip.mp4", "C:/notes.txt"], false, "Grid", true, new object()));

        Assert.Equal(LWindowDropEffect.LWindowDropFile, TInterface.TWindowEffectRead(window));
        Assert.True(TInterface.TWindowHandledRead(window));
        Assert.Equal([new[] { "C:/clip.mp4", "C:/notes.txt" }], listDrops);
        Assert.Empty(viewerDrops);
    }

    [Fact]
    public void Drop_OnListTab_WithoutMediaOrCopy_Refuses()
    {
        LWindow window = TWindowBuild(out LStrip strip);
        TInterface.TWindowTabSet(window, TListTabAdd(strip), null);
        List<IReadOnlyList<string>> listDrops = [];
        TInterface.TWindowDropAttach(window, listDrops.Add, _ => { });

        TInterface.TWindowDropHandle(
            window, TInterface.TWindowDragCreate(["C:/notes.txt"], false, "Grid", true, new object()));
        Assert.Equal(LWindowDropEffect.LWindowDropNone, TInterface.TWindowEffectRead(window));

        TInterface.TWindowDropHandle(
            window, TInterface.TWindowDragCreate(["C:/clip.mp4"], false, "Grid", false, new object()));
        Assert.Equal(LWindowDropEffect.LWindowDropNone, TInterface.TWindowEffectRead(window));
        Assert.Empty(listDrops);
    }

    [Fact]
    public void Drop_OnViewerTab_OpensTheFirstExistingFile()
    {
        LWindow window = TWindowBuild(out LStrip strip);
        LStripTab tab = TInterface.TStripTabCreate("Split");
        TInterface.TStripWorkspaceAttach(tab, TInterface.TPresetInitialCreate("Split"), null);
        TInterface.TStripAdd(strip, tab);
        TInterface.TWindowTabSet(window, tab, TInterface.TViewerCreate());
        List<string> viewerDrops = [];
        TInterface.TWindowDropAttach(window, _ => { }, viewerDrops.Add);
        string media = TMediaFileCreate();
        try
        {
            TInterface.TWindowDropHandle(
                window,
                TInterface.TWindowDragCreate(["C:/missing.mp4", media], false, "Grid", true, new object()));

            Assert.Equal(LWindowDropEffect.LWindowDropFile, TInterface.TWindowEffectRead(window));
            Assert.Equal([media], viewerDrops);

            TInterface.TWindowDropHandle(
                window, TInterface.TWindowDragCreate(["C:/missing.mp4"], false, "Grid", true, new object()));
            Assert.Equal(LWindowDropEffect.LWindowDropNone, TInterface.TWindowEffectRead(window));
            Assert.Single(viewerDrops);
        }
        finally
        {
            File.Delete(media);
        }
    }

    [Fact]
    public void Width_FloorsAtNineHundredPlusReserved()
    {
        LWindow window = TWindowBuild(out _);

        Assert.Equal((900d, 1280d), TInterface.TWindowWidthResolve(window, null, 0, 1280));
        Assert.Equal((1180d, 1180d), TInterface.TWindowWidthResolve(window, 1000, 180, 700));
    }

    [Fact]
    public void TabSet_RaisesDetachThenAttach()
    {
        LWindow window = TWindowBuild(out LStrip strip);
        LStripTab first = TListTabAdd(strip);
        LStripTab second = TListTabAdd(strip);
        List<string> notices = [];
        TInterface.TWindowSelectAttach(
            window,
            (tab, _) => notices.Add("attach " + tab.LStripTabTitle),
            tab => notices.Add("detach " + tab.LStripTabTitle));

        TInterface.TWindowTabSet(window, first, null);
        TInterface.TWindowTabSet(window, second, null);
        TInterface.TWindowTabSet(window, null, null);

        Assert.Equal(["attach Split 1", "detach Split 1", "attach Split 2", "detach Split 2"], notices);
        Assert.Null(window.LWindowTab);
    }

    [Fact]
    public void SceneRead_ListsTabsInOrderWithSelection()
    {
        LWindow window = TWindowBuild(out LStrip strip);
        TListTabAdd(strip);
        LStripTab second = TListTabAdd(strip);
        TInterface.TStripSelect(strip, second);

        var scene = TInterface.TWindowSceneRead(window, "Night");

        Assert.Equal("Night", scene.LSceneName);
        Assert.Equal(["Split", "Split"], scene.LSceneLayoutKeys);
        Assert.Equal(1, scene.LSceneTabIndex);
        Assert.Equal(2, scene.LSceneTabExports.Count);
        Assert.Equal(2, scene.LSceneTabLayouts.Count);
    }
}
