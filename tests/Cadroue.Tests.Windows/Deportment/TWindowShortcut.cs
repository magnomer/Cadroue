using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TWindowShortcut
{
    private const int TWindowKeydown = 0x0100;
    private const int TWindowMotion = 0x0200;
    private const short TWindowKeyState = unchecked((short)0x8000);

    private static QWindow TWindowBuild() => TInterface.TWindowCreate(body => body());

    private static QFlow TWindowFlowAttach(QWindow window)
    {
        QStripTab tab = TInterface.TStripTabCreate("Split");
        QFlow flow = tab.QStripTabWorkspace.QWorkspaceFlow!;
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowSourceSet(flow, TInterfaceViewer.TViewerInfoCreate(
            TimeSpan.FromSeconds(60), 640, 360), "C:\\a.mp4");
        TInterface.TStripWorkspaceAttach(tab, TInterface.TPresetInitialCreate("Split"));
        TInterface.TWindowTabSet(window, tab, null);
        return flow;
    }

    private static QWindowPress TWindowPressBuild(
        QWindow window,
        bool handled,
        int message,
        bool modal,
        nint foreground,
        bool input,
        string key,
        short state,
        Func<nint, bool?>? viewer = null) =>
        TInterface.TWindowPressCreate(
            handled,
            message,
            65,
            modal,
            100,
            () => foreground,
            viewer ?? (_ => null),
            () => input,
            _ => key,
            _ => state);

    [Fact]
    public void Run_DispatchesTokensToTheAttachedSeams()
    {
        QWindow window = TWindowBuild();
        List<string> calls = [];
        TInterface.TWindowShortcutAttach(
            window,
            () => calls.Add("show"),
            () => { calls.Add("undo"); return true; },
            () => { calls.Add("redo"); return null; },
            () => { calls.Add("unload"); return false; },
            cohorts => { calls.Add("clear"); return true; });

        Assert.True(TInterface.TWindowShortcutRun(window, "Show"));
        Assert.True(TInterface.TWindowShortcutRun(window, "Undo"));
        Assert.False(TInterface.TWindowShortcutRun(window, "Redo"));
        Assert.False(TInterface.TWindowShortcutRun(window, "UnloadAll"));
        Assert.True(TInterface.TWindowShortcutRun(window, "Unload"));
        Assert.False(TInterface.TWindowShortcutRun(window, "ZoomIn"));
        Assert.Equal(["show", "undo", "redo", "unload", "clear"], calls);

        QFlow flow = TWindowFlowAttach(window);
        TInterface.TFlowSpoolAttach(flow, () => calls.Add("spool"));
        Assert.True(TInterface.TWindowShortcutRun(window, "ZoomIn"));
        Assert.False(TInterface.TWindowShortcutRun(window, "Nothing"));
        Assert.Equal(["show", "undo", "redo", "unload", "clear", "spool"], calls);
    }

    [Fact]
    public void PlayPause_NeedsAViewer_AndTogglesByState()
    {
        QWindow window = TWindowBuild();
        List<string> calls = [];
        TInterface.TWindowPlayAttach(window, () => calls.Add("play"), () => calls.Add("pause"));

        Assert.False(TInterface.TWindowShortcutRun(window, "PlayPause"));
        Assert.Empty(calls);

        TInterface.TWindowTabSet(window, null, TInterfaceViewer.TViewerCreate());
        Assert.True(TInterface.TWindowShortcutRun(window, "PlayPause"));
        Assert.Equal(["play"], calls);
    }

    [Fact]
    public void Handle_GatesOnMessageModalFocusAndInput()
    {
        QWindow window = TWindowBuild();

        Assert.True(TInterface.TWindowShortcutHandle(
            window, TWindowPressBuild(window, true, TWindowMotion, false, 100, false, "A", 0)));
        Assert.False(TInterface.TWindowShortcutHandle(
            window, TWindowPressBuild(window, false, TWindowMotion, false, 100, false, "A", 0)));
        Assert.False(TInterface.TWindowShortcutHandle(
            window, TWindowPressBuild(window, false, TWindowKeydown, true, 100, false, "A", 0)));
        Assert.False(TInterface.TWindowShortcutHandle(
            window, TWindowPressBuild(window, false, TWindowKeydown, false, 0, false, "A", 0)));
        Assert.False(TInterface.TWindowShortcutHandle(
            window, TWindowPressBuild(window, false, TWindowKeydown, false, 200, false, "A", 0)));
        Assert.False(TInterface.TWindowShortcutHandle(
            window, TWindowPressBuild(window, false, TWindowKeydown, false, 100, true, "A", 0)));
    }

    [Fact]
    public void Handle_ResolvesTheGestureAndRunsTheBoundToken()
    {
        QWindow window = TWindowBuild();
        List<string> calls = [];
        TInterface.TWindowShortcutAttach(
            window,
            () => calls.Add("show"),
            () => null,
            () => null,
            () => false,
            _ => null);
        QFlow flow = TWindowFlowAttach(window);
        TInterface.TFlowSpoolAttach(flow, () => calls.Add("spool"));

        Assert.True(TInterface.TWindowShortcutHandle(
            window, TWindowPressBuild(window, false, TWindowKeydown, false, 100, false, "C", 0)));
        Assert.False(TInterface.TWindowShortcutHandle(
            window, TWindowPressBuild(window, false, TWindowKeydown, false, 100, false, "", TWindowKeyState)));
        Assert.False(TInterface.TWindowShortcutHandle(
            window, TWindowPressBuild(window, false, TWindowKeydown, false, 100, false, "C", TWindowKeyState)));
        Assert.True(TInterface.TWindowShortcutHandle(
            window,
            TWindowPressBuild(
                window, false, TWindowKeydown, false, 300, false, "V", 0, foreground => foreground == 300)));
        Assert.Equal(["spool", "spool"], calls);
    }
}
