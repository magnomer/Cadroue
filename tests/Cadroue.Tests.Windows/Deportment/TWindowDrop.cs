using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TWindowDrop
{
    private static QWindow TWindowBuild(out QStrip strip)
    {
        QWindow window = TInterface.TWindowCreate(body => body());
        strip = TInterface.TWindowStripRead(window);
        return window;
    }

    private static QStripTab TListTabAdd(QStrip strip)
    {
        QStripTab tab = TInterface.TStripTabCreate("Split");
        TInterface.TStripWorkspaceAttach(tab, TInterface.TPresetInitialCreate("Split"));
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
        QWindow window = TWindowBuild(out _);
        var data = new object();

        TInterface.TWindowOverHandle(window, TInterface.TWindowDragCreate(["C:/x.mp4"], false, "Grid", true, data));

        Assert.Equal(QWindowDropEffect.QWindowDropNone, TInterface.TWindowEffectRead(window));
        Assert.True(TInterface.TWindowHandledRead(window));
    }

    [Fact]
    public void Over_OnGroup_LeavesTheEventAlone()
    {
        QWindow window = TWindowBuild(out QStrip strip);
        TInterface.TWindowTabSet(window, TListTabAdd(strip), null);

        TInterface.TWindowOverHandle(
            window, TInterface.TWindowDragCreate(["C:/x.mp4"], true, "PGroup", true, new object()));

        Assert.Equal(QWindowDropEffect.QWindowDropNone, TInterface.TWindowEffectRead(window));
        Assert.False(TInterface.TWindowHandledRead(window));
    }

    [Fact]
    public void Drop_OnListTab_HandsMediaPathsToTheList()
    {
        QWindow window = TWindowBuild(out QStrip strip);
        TInterface.TWindowTabSet(window, TListTabAdd(strip), null);
        List<IReadOnlyList<string>> listDrops = [];
        List<string> viewerDrops = [];
        TInterface.TWindowDropAttach(window, listDrops.Add, viewerDrops.Add);

        TInterface.TWindowDropHandle(
            window,
            TInterface.TWindowDragCreate(["C:/clip.mp4", "C:/notes.txt"], false, "Grid", true, new object()));

        Assert.Equal(QWindowDropEffect.QWindowDropFile, TInterface.TWindowEffectRead(window));
        Assert.True(TInterface.TWindowHandledRead(window));
        Assert.Equal([new[] { "C:/clip.mp4", "C:/notes.txt" }], listDrops);
        Assert.Empty(viewerDrops);
    }

    [Fact]
    public void Drop_OnListTab_WithoutMediaOrCopy_Refuses()
    {
        QWindow window = TWindowBuild(out QStrip strip);
        TInterface.TWindowTabSet(window, TListTabAdd(strip), null);
        List<IReadOnlyList<string>> listDrops = [];
        TInterface.TWindowDropAttach(window, listDrops.Add, _ => { });

        TInterface.TWindowDropHandle(
            window, TInterface.TWindowDragCreate(["C:/notes.txt"], false, "Grid", true, new object()));
        Assert.Equal(QWindowDropEffect.QWindowDropNone, TInterface.TWindowEffectRead(window));

        TInterface.TWindowDropHandle(
            window, TInterface.TWindowDragCreate(["C:/clip.mp4"], false, "Grid", false, new object()));
        Assert.Equal(QWindowDropEffect.QWindowDropNone, TInterface.TWindowEffectRead(window));
        Assert.Empty(listDrops);
    }

    [Fact]
    public void Drop_OnViewerTab_OpensTheFirstExistingFile()
    {
        QWindow window = TWindowBuild(out QStrip strip);
        TInterface.TWindowTabSet(window, null, TInterfaceViewer.TViewerCreate());
        List<string> viewerDrops = [];
        TInterface.TWindowDropAttach(window, _ => { }, viewerDrops.Add);
        string media = TMediaFileCreate();
        try
        {
            TInterface.TWindowDropHandle(
                window,
                TInterface.TWindowDragCreate(["C:/missing.mp4", media], false, "Grid", true, new object()));

            Assert.Equal(QWindowDropEffect.QWindowDropFile, TInterface.TWindowEffectRead(window));
            Assert.Equal([media], viewerDrops);

            TInterface.TWindowDropHandle(
                window, TInterface.TWindowDragCreate(["C:/missing.mp4"], false, "Grid", true, new object()));
            Assert.Equal(QWindowDropEffect.QWindowDropNone, TInterface.TWindowEffectRead(window));
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
        QWindow window = TWindowBuild(out _);

        Assert.Equal((900d, 1280d), TInterface.TWindowWidthResolve(window, null, 0, 1280));
        Assert.Equal((1180d, 1180d), TInterface.TWindowWidthResolve(window, 1000, 180, 700));
    }

    [Fact]
    public void TabSet_RaisesDetachThenAttach()
    {
        QWindow window = TWindowBuild(out QStrip strip);
        QStripTab first = TListTabAdd(strip);
        QStripTab second = TListTabAdd(strip);
        List<string> notices = [];
        TInterface.TWindowSelectAttach(
            window,
            (tab, _) => notices.Add("attach Split " + (ReferenceEquals(tab, first) ? 1 : 2)),
            tab => notices.Add("detach Split " + (ReferenceEquals(tab, first) ? 1 : 2)));

        TInterface.TWindowTabSet(window, first, null);
        TInterface.TWindowTabSet(window, second, null);
        TInterface.TWindowTabSet(window, null, null);

        Assert.Equal(["attach Split 1", "detach Split 1", "attach Split 2", "detach Split 2"], notices);
        Assert.Null(window.QWindowTab);
    }

    [Fact]
    public void SceneRead_ListsTabsInOrderWithSelection()
    {
        QWindow window = TWindowBuild(out QStrip strip);
        TListTabAdd(strip);
        QStripTab second = TListTabAdd(strip);
        TInterface.TStripSelect(strip, second);

        var scene = TInterface.TWindowSceneRead(window, "Night");

        Assert.Equal("Night", scene.LSceneName);
        Assert.Equal(["Split", "Split"], scene.LSceneLayoutKeys);
        Assert.Equal(1, scene.LSceneTabIndex);
        Assert.Equal(2, scene.LSceneTabExports.Count);
        Assert.Equal(2, scene.LSceneTabLayouts.Count);
    }
}
