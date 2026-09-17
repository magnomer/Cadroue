using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TConsoleScene
{
    [Fact]
    public void Reload_IsConsumedOnce()
    {
        LConsole console = TInterface.TConsoleCreate();
        TInterface.TConsoleReloadSet(console, "Night");

        Assert.Equal("Night", console.LConsoleReloadName);
        Assert.Equal("Night", TInterface.TConsoleReloadRead(console));
        Assert.Null(TInterface.TConsoleReloadRead(console));
    }

    [Fact]
    public void Scene_MatchesIgnoringCase()
    {
        LConsole console = TInterface.TConsoleCreate();
        Assert.False(TInterface.TConsoleSceneCheck(console, string.Empty) && console.LConsoleSceneName.Length > 0);

        TInterface.TConsoleSceneSet(console, "Daily");
        Assert.True(TInterface.TConsoleSceneCheck(console, "daily"));
        Assert.False(TInterface.TConsoleSceneCheck(console, "Weekly"));
    }

    [Fact]
    public void Progress_ClampsAndReportsBackward()
    {
        LConsole console = TInterface.TConsoleCreate();

        Assert.True(TInterface.TConsoleProgressSet(console, 0.5));
        Assert.False(console.LConsoleBackward);
        Assert.False(TInterface.TConsoleProgressSet(console, 0.5));
        Assert.True(TInterface.TConsoleProgressSet(console, 2));
        Assert.Equal(1, console.LConsoleProgress);
        Assert.True(TInterface.TConsoleProgressSet(console, 0.25));
        Assert.True(console.LConsoleBackward);
    }

    [Fact]
    public void Latches_FireOnce()
    {
        LConsole console = TInterface.TConsoleCreate();

        Assert.True(TInterface.TConsoleCaretSet(console));
        Assert.False(TInterface.TConsoleCaretSet(console));
        Assert.True(TInterface.TConsoleSpinSet(console, true));
        Assert.False(TInterface.TConsoleSpinSet(console, true));
        Assert.True(TInterface.TConsoleSpinSet(console, false));
    }
}
