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
        scenes.ChangeMatchIgnoredName(right, "different-identity");

        Assert.True(scenes.Match(left, right));
    }

    [Fact]
    public void SceneVersionDifference_DoesNotAffectMatch()
    {
        using var scenes = new TScene();
        TSceneValue stored = scenes.Create("stored", marker: 3);
        TSceneValue live = scenes.Create("live", marker: 3);
        scenes.ChangeMatchIgnoredVersion(live);

        Assert.True(scenes.Match(stored, live));
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
    public void SelectedTabDifference_DoesNotMatch()
    {
        using var scenes = new TScene();
        TSceneValue stored = scenes.Create("stored", marker: 10);
        TSceneValue live = scenes.Create("live", marker: 10);
        scenes.ChangeTabIndex(live);

        Assert.False(scenes.Match(stored, live));
    }

    [Fact]
    public void PanelWidthDifference_DoesNotMatch()
    {
        using var scenes = new TScene();
        TSceneValue stored = scenes.Create("stored", marker: 11);
        TSceneValue live = scenes.Create("live", marker: 11);
        scenes.ChangePanelWidths(live);

        Assert.False(scenes.Match(stored, live));
    }

    [Fact]
    public void PersistedCollectionOrder_AffectsMatch()
    {
        using var scenes = new TScene();
        TSceneValue baseline = scenes.Create("baseline", marker: 12);
        TSceneValue tabOrderChanged = scenes.Create("tab-order", marker: 12);
        TSceneValue widthOrderChanged = scenes.Create("width-order", marker: 12);

        scenes.ReverseMeaningfulCollection(tabOrderChanged);
        scenes.ReversePanelWidths(widthOrderChanged);

        Assert.False(scenes.Match(baseline, tabOrderChanged));
        Assert.False(scenes.Match(baseline, widthOrderChanged));
    }
}
