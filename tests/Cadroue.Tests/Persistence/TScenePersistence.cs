using Xunit;

namespace Cadroue.Tests;

[Collection("Scene")]
public sealed class TScenePersistence
{
    [Fact]
    public void SavingThenReadingOneScene_ReproducesEveryPersistedField()
    {
        using var scenes = new TScene();
        TSceneValue expected = scenes.TSceneRecordCreate("complete-scene");

        scenes.TSceneSave(expected);
        scenes.TSceneDiskLoad();

        TSceneValue actual = Assert.IsType<TSceneValue>(scenes.TSceneRead("complete-scene"));
        Assert.True(scenes.TScenePersistCheck(expected, actual));
    }

    [Fact]
    public void SavingModifiedScene_ReplacesStaleStoredValues()
    {
        using var scenes = new TScene();
        TSceneValue original = scenes.TSceneRecordCreate("replace-me", marker: 1);
        TSceneValue modified = scenes.TSceneRecordCreate("replace-me", marker: 20);
        scenes.TSceneSave(original);

        scenes.TSceneSave(modified);
        scenes.TSceneDiskLoad();

        TSceneValue actual = Assert.IsType<TSceneValue>(scenes.TSceneRead("replace-me"));
        Assert.True(scenes.TScenePersistCheck(modified, actual));
        Assert.False(scenes.TScenePersistCheck(original, actual));
        Assert.Single(scenes.TSceneNames);
    }

    [Fact]
    public void SeparatelyNamedScenes_RemainIsolated()
    {
        using var scenes = new TScene();
        TSceneValue first = scenes.TSceneRecordCreate("first", marker: 1);
        TSceneValue second = scenes.TSceneRecordCreate("second", marker: 30);

        scenes.TSceneSave(first);
        scenes.TSceneSave(second);
        scenes.TSceneDiskLoad();

        Assert.True(scenes.TScenePersistCheck(first, Assert.IsType<TSceneValue>(scenes.TSceneRead("first"))));
        Assert.True(scenes.TScenePersistCheck(second, Assert.IsType<TSceneValue>(scenes.TSceneRead("second"))));
    }

    [Fact]
    public void DeletingOneScene_DoesNotDeleteAnother()
    {
        using var scenes = new TScene();
        TSceneValue retained = scenes.TSceneRecordCreate("retained", marker: 4);
        scenes.TSceneSave(scenes.TSceneRecordCreate("deleted", marker: 2));
        scenes.TSceneSave(retained);

        Assert.True(scenes.TSceneDelete("deleted"));
        scenes.TSceneDiskLoad();

        Assert.Null(scenes.TSceneRead("deleted"));
        Assert.True(scenes.TScenePersistCheck(
            retained,
            Assert.IsType<TSceneValue>(scenes.TSceneRead("retained"))));
    }

    [Fact]
    public void ReadingMissingScene_ReturnsProductionAbsence()
    {
        using var scenes = new TScene();

        Assert.Null(scenes.TSceneRead("missing"));
    }

    [Fact]
    public void ExternalSceneFileRoundTrip_PreservesPersistedState()
    {
        using var scenes = new TScene();
        TSceneValue expected = scenes.TSceneRecordCreate("external", marker: 8);

        TSceneValue actual = scenes.TSceneExternalMatch(expected);

        Assert.True(scenes.TScenePersistCheck(expected, actual));
    }

    [Fact]
    public void MalformedSceneStorage_FailsWithoutFabricatingScene()
    {
        using var scenes = new TScene();
        scenes.TSceneSave(scenes.TSceneRecordCreate("formerly-valid"));
        scenes.TSceneMalformSave();

        scenes.TSceneDiskLoad();

        Assert.Empty(scenes.TSceneNames);
        Assert.Null(scenes.TSceneRead("formerly-valid"));
    }

    [Fact]
    public void CurrentAndActiveIdentity_RemainCorrectAcrossSaveAndRead()
    {
        using var scenes = new TScene();
        TSceneValue current = scenes.TSceneRecordCreate("current", marker: 3);
        scenes.TSceneCurrentSave(current);

        scenes.TSceneSave(scenes.TSceneRecordCreate("stored", marker: 7));
        Assert.NotNull(scenes.TSceneRead("stored"));

        Assert.Equal("current", scenes.TSceneCurrentName);
        Assert.Equal("current", scenes.TSceneActiveName);
    }
}
