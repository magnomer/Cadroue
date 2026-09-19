using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TConsoleScene
{
    [Fact]
    public void Reload_IsConsumedOnce()
    {
        LConsoleScene scene = TInterface.TConsoleSceneCreate();
        TInterface.TConsoleReloadSet(scene, "Night");

        Assert.Equal("Night", scene.LConsoleReloadName);
        Assert.Equal("Night", TInterface.TConsoleReloadRead(scene));
        Assert.Null(TInterface.TConsoleReloadRead(scene));
    }

    [Fact]
    public void Row_KeepsTheReloadName_WhenTheSenderCarriesNone()
    {
        LConsoleScene scene = TInterface.TConsoleSceneCreate();
        TInterface.TConsoleRowHandle(scene, "Night");
        TInterface.TConsoleRowHandle(scene, null);

        Assert.Equal("Night", scene.LConsoleReloadName);
    }

    [Fact]
    public void Scene_MatchesIgnoringCase()
    {
        LConsoleScene scene = TInterface.TConsoleSceneCreate();
        Assert.False(TInterface.TConsoleSceneCheck(scene, string.Empty) && scene.LConsoleSceneName.Length > 0);

        TInterface.TConsoleSceneSet(scene, "Daily");
        Assert.True(TInterface.TConsoleSceneCheck(scene, "daily"));
        Assert.False(TInterface.TConsoleSceneCheck(scene, "Weekly"));
    }

    [Fact]
    public void Dirty_IsFalse_WithoutANameOrAWindow()
    {
        LConsoleScene scene = TInterface.TConsoleSceneCreate();
        Assert.False(TInterface.TConsoleDirtyCheck(scene));

        TInterface.TConsoleSceneSet(scene, "Unknown scene");
        Assert.False(TInterface.TConsoleDirtyCheck(scene));
    }

    [Fact]
    public void Delete_IsNotHandled_WhenTheSourceIsNoSceneButton()
    {
        LConsoleScene scene = TInterface.TConsoleSceneCreate();
        Assert.False(TInterface.TConsoleDeleteHandle(scene, null));
    }

    [Fact]
    public void Press_ClearsFocus_OnlyOutsideTheOpenCombo()
    {
        LConsoleScene scene = TInterface.TConsoleSceneCreate();
        int cleared = 0;
        TInterface.TConsoleFocusAttach(scene, () => cleared++);

        TInterface.TConsolePressHandle(scene, true, true, false);
        TInterface.TConsolePressHandle(scene, false, false, false);
        TInterface.TConsolePressHandle(scene, false, true, true);
        Assert.Equal(0, cleared);

        TInterface.TConsolePressHandle(scene, false, true, false);
        Assert.Equal(1, cleared);
    }

    [Fact]
    public void ImportName_FallsBackToTheFileStem()
    {
        Assert.Equal("Night", TInterface.TConsoleStemResolve("  Night ", "C:/scenes/other.json"));
        Assert.Equal("other", TInterface.TConsoleStemResolve("   ", "C:/scenes/other.json"));
        Assert.NotEmpty(TInterface.TConsoleStemResolve(string.Empty, "C:/scenes/.json"));
    }

    [Fact]
    public void NameCreate_NumbersDuplicatesIgnoringCase()
    {
        string[] names = ["Night", "night 2"];
        Assert.Equal("Day", TInterface.TConsoleNameCreate("Day", names));
        Assert.Equal("NIGHT 3", TInterface.TConsoleNameCreate("NIGHT", names));
    }
}
