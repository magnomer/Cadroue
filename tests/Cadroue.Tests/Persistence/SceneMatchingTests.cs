using Xunit;

namespace Cadroue.Tests;

[Collection("Scene")]
public sealed class SceneMatchingTests
{
    [Fact]
    public void SemanticallyIdenticalScenes_Match()
    {
        using var scenes = new TScene();
        TSceneValue left = scenes.Create("left", marker: 6);
        TSceneValue right = scenes.Create("right", marker: 6);
        scenes.ChangeMatchIgnoredState(right, "different-identity");

        Assert.True(scenes.Match(left, right));
    }

    [Fact]
    public void MeaningfulPersistedFieldChange_DoesNotMatch()
    {
        using var scenes = new TScene();
        TSceneValue left = scenes.Create("left", marker: 9);
        TSceneValue right = scenes.Create("right", marker: 9);
        scenes.ChangeMeaningfulField(right);

        Assert.False(scenes.Match(left, right));
    }

    [Fact]
    public void CollectionOrder_MattersOnlyForMeaningfulCollections()
    {
        using var scenes = new TScene();
        TSceneValue baseline = scenes.Create("baseline", marker: 12);
        TSceneValue meaningfulOrderChanged = scenes.Create("meaningful", marker: 12);
        TSceneValue ignoredOrderChanged = scenes.Create("ignored", marker: 12);

        scenes.ReverseMeaningfulCollection(meaningfulOrderChanged);
        scenes.ReverseIgnoredCollection(ignoredOrderChanged);

        Assert.False(scenes.Match(baseline, meaningfulOrderChanged));
        Assert.True(scenes.Match(baseline, ignoredOrderChanged));
    }
}
