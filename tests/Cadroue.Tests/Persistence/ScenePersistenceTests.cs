using Xunit;

namespace Cadroue.Tests;

[Collection("Scene")]
public sealed class ScenePersistenceTests
{
    [Fact]
    public void SavingThenReadingOneScene_ReproducesEveryPersistedField()
    {
        using var scenes = new TScene();
        TSceneValue expected = scenes.Create("complete-scene");

        scenes.Save(expected);
        scenes.Reload();

        TSceneValue actual = Assert.IsType<TSceneValue>(scenes.Read("complete-scene"));
        Assert.True(scenes.PersistedFieldsEqual(expected, actual));
    }

    [Fact]
    public void SavingModifiedScene_ReplacesStaleStoredValues()
    {
        using var scenes = new TScene();
        TSceneValue original = scenes.Create("replace-me", marker: 1);
        TSceneValue modified = scenes.Create("replace-me", marker: 20);
        scenes.Save(original);

        scenes.Save(modified);
        scenes.Reload();

        TSceneValue actual = Assert.IsType<TSceneValue>(scenes.Read("replace-me"));
        Assert.True(scenes.PersistedFieldsEqual(modified, actual));
        Assert.False(scenes.PersistedFieldsEqual(original, actual));
        Assert.Single(scenes.Names);
    }

    [Fact]
    public void SeparatelyNamedScenes_RemainIsolated()
    {
        using var scenes = new TScene();
        TSceneValue first = scenes.Create("first", marker: 1);
        TSceneValue second = scenes.Create("second", marker: 30);

        scenes.Save(first);
        scenes.Save(second);
        scenes.Reload();

        Assert.True(scenes.PersistedFieldsEqual(first, Assert.IsType<TSceneValue>(scenes.Read("first"))));
        Assert.True(scenes.PersistedFieldsEqual(second, Assert.IsType<TSceneValue>(scenes.Read("second"))));
    }

    [Fact]
    public void DeletingOneScene_DoesNotDeleteAnother()
    {
        using var scenes = new TScene();
        TSceneValue retained = scenes.Create("retained", marker: 4);
        scenes.Save(scenes.Create("deleted", marker: 2));
        scenes.Save(retained);

        Assert.True(scenes.Delete("deleted"));
        scenes.Reload();

        Assert.Null(scenes.Read("deleted"));
        Assert.True(scenes.PersistedFieldsEqual(
            retained,
            Assert.IsType<TSceneValue>(scenes.Read("retained"))));
    }

    [Fact]
    public void ReadingMissingScene_ReturnsProductionAbsence()
    {
        using var scenes = new TScene();

        Assert.Null(scenes.Read("missing"));
    }

    [Fact]
    public void ExternalSceneFileRoundTrip_PreservesPersistedState()
    {
        using var scenes = new TScene();
        TSceneValue expected = scenes.Create("external", marker: 8);

        TSceneValue actual = scenes.ExternalRoundTrip(expected);

        Assert.True(scenes.PersistedFieldsEqual(expected, actual));
    }

    [Fact]
    public void MalformedSceneStorage_FailsWithoutFabricatingScene()
    {
        using var scenes = new TScene();
        scenes.Save(scenes.Create("formerly-valid"));
        scenes.MalformStorage();

        scenes.Reload();

        Assert.Empty(scenes.Names);
        Assert.Null(scenes.Read("formerly-valid"));
    }

    [Fact]
    public void CurrentAndActiveIdentity_RemainCorrectAcrossSaveAndRead()
    {
        using var scenes = new TScene();
        TSceneValue current = scenes.Create("current", marker: 3);
        scenes.SaveAsCurrent(current);

        scenes.Save(scenes.Create("stored", marker: 7));
        Assert.NotNull(scenes.Read("stored"));

        Assert.Equal("current", scenes.CurrentName);
        Assert.Equal("current", scenes.ActiveName);
    }
}
