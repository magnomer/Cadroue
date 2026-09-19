using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TWindowShortcut
{
    private const int TWindowKeydown = 0x0100;
    private const int TWindowMotion = 0x0200;
    private const short TWindowKeyState = unchecked((short)0x8000);

    private static LWindow TWindowBuild()
    {
        LStrip strip = TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");
        return TInterface.TWindowCreate(strip, body => body());
    }

    private static LWindowPress TWindowPressBuild(
        LWindow window,
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
        LWindow window = TWindowBuild();
        List<string> calls = [];
        TInterface.TWindowShortcutAttach(
            window,
            () => calls.Add("show"),
            () => { calls.Add("undo"); return true; },
            () => { calls.Add("redo"); return null; },
            () => { calls.Add("unload"); return false; },
            cohorts => { calls.Add("clear"); return true; },
            code => { calls.Add("flow:" + code); return code.Length > 0; });

        Assert.True(TInterface.TWindowShortcutRun(window, "Show"));
        Assert.True(TInterface.TWindowShortcutRun(window, "Undo"));
        Assert.False(TInterface.TWindowShortcutRun(window, "Redo"));
        Assert.False(TInterface.TWindowShortcutRun(window, "UnloadAll"));
        Assert.True(TInterface.TWindowShortcutRun(window, "Unload"));
        Assert.True(TInterface.TWindowShortcutRun(window, "ZoomIn"));
        Assert.False(TInterface.TWindowShortcutRun(window, "Nothing"));
        Assert.Equal(["show", "undo", "redo", "unload", "clear", "flow:zoomIn", "flow:"], calls);
    }

    [Fact]
    public void PlayPause_NeedsAViewer_AndTogglesByState()
    {
        LWindow window = TWindowBuild();
        List<string> calls = [];
        TInterface.TWindowPlayAttach(window, () => calls.Add("play"), () => calls.Add("pause"));

        Assert.False(TInterface.TWindowShortcutRun(window, "PlayPause"));
        Assert.Empty(calls);

        TInterface.TWindowTabSet(window, null, TInterface.TViewerCreate());
        Assert.True(TInterface.TWindowShortcutRun(window, "PlayPause"));
        Assert.Equal(["play"], calls);
    }

    [Fact]
    public void Handle_GatesOnMessageModalFocusAndInput()
    {
        LWindow window = TWindowBuild();

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
        LWindow window = TWindowBuild();
        List<string> calls = [];
        TInterface.TWindowShortcutAttach(
            window,
            () => calls.Add("show"),
            () => null,
            () => null,
            () => false,
            _ => null,
            code => { calls.Add(code); return true; });

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
        Assert.Equal(["zoomIn", "zoomOut"], calls);
    }
}
