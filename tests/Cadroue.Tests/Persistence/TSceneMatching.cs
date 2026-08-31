using Xunit;

namespace Cadroue.Tests;

[Collection("Scene")]
public sealed class TSceneMatching
{
    [Fact]
    public void SemanticallyIdenticalScenes_Match()
    {
        using var scenes = new TScene();
        TSceneValue left = scenes.TSceneRecordCreate("left", marker: 6);
        TSceneValue right = scenes.TSceneRecordCreate("right", marker: 6);
        scenes.TSceneNameChange(right, "different-identity");

        Assert.True(scenes.TSceneMatch(left, right));
    }

    [Fact]
    public void SceneVersionDifference_DoesNotAffectMatch()
    {
        using var scenes = new TScene();
        TSceneValue stored = scenes.TSceneRecordCreate("stored", marker: 3);
        TSceneValue live = scenes.TSceneRecordCreate("live", marker: 3);
        scenes.TSceneVersionChange(live);

        Assert.True(scenes.TSceneMatch(stored, live));
    }

    [Fact]
    public void MeaningfulPersistedFieldChange_DoesNotMatch()
    {
        using var scenes = new TScene();
        TSceneValue left = scenes.TSceneRecordCreate("left", marker: 9);
        TSceneValue right = scenes.TSceneRecordCreate("right", marker: 9);
        scenes.TSceneFieldChange(right);

        Assert.False(scenes.TSceneMatch(left, right));
    }

    [Fact]
    public void SelectedTabDifference_DoesNotAffectMatch()
    {
        using var scenes = new TScene();
        TSceneValue stored = scenes.TSceneRecordCreate("stored", marker: 10);
        TSceneValue live = scenes.TSceneRecordCreate("live", marker: 10);
        scenes.TSceneTabChange(live);

        Assert.True(scenes.TSceneMatch(stored, live));
    }

    [Fact]
    public void PanelWidthDifference_DoesNotMatch()
    {
        using var scenes = new TScene();
        TSceneValue stored = scenes.TSceneRecordCreate("stored", marker: 11);
        TSceneValue live = scenes.TSceneRecordCreate("live", marker: 11);
        scenes.TSceneWidthChange(live);

        Assert.False(scenes.TSceneMatch(stored, live));
    }

    [Fact]
    public void ProportionallyEquivalentPanelWidths_Match()
    {
        using var scenes = new TScene();
        TSceneValue stored = scenes.TSceneRecordCreate("stored", marker: 11);
        TSceneValue restored = scenes.TSceneRecordCreate("restored", marker: 11);
        scenes.TSceneScaleCreate(restored, 0.9248028709630333);

        Assert.True(scenes.TSceneMatch(stored, restored));
    }

    [Fact]
    public void PersistedCollectionOrder_AffectsMatch()
    {
        using var scenes = new TScene();
        TSceneValue baseline = scenes.TSceneRecordCreate("baseline", marker: 12);
        TSceneValue tabOrderChanged = scenes.TSceneRecordCreate("tab-order", marker: 12);
        TSceneValue widthOrderChanged = scenes.TSceneRecordCreate("width-order", marker: 12);

        scenes.TSceneReverseCreate(tabOrderChanged);
        scenes.TSceneWidthCreate(widthOrderChanged);

        Assert.False(scenes.TSceneMatch(baseline, tabOrderChanged));
        Assert.False(scenes.TSceneMatch(baseline, widthOrderChanged));
    }
}
