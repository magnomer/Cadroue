using System.Collections.Generic;

using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class SceneNormalizeTests
{
    [Fact]
    public void NormalizingNull_YieldsCurrentVersionRecord()
    {
        LSceneRecord scene = TScene.Normalize(null);

        Assert.Equal(TScene.VersionCurrent, scene.LSceneVersion);
        Assert.True(scene.LSceneDefaultTabs);
        Assert.Empty(scene.LSceneLayoutKeys);
        Assert.Equal(0, scene.LSceneTabIndex);
    }

    [Fact]
    public void NormalizingExplicitEmptyScene_PreservesZeroTabs()
    {
        LSceneRecord scene = TScene.RawCreate();

        TScene.Normalize(scene);

        Assert.False(scene.LSceneDefaultTabs);
        Assert.Empty(scene.LSceneLayoutKeys);
    }

    [Fact]
    public void NormalizingNullCollections_ReplacesThemWithEmptyLists()
    {
        LSceneRecord scene = TScene.RawCreate();
        scene.LSceneLayoutKeys = null!;
        scene.LSceneTabNames = null!;
        scene.LSceneTabExports = null!;
        scene.LSceneTabLayouts = null!;
        scene.LSceneTabRelays = null!;

        TScene.Normalize(scene);

        Assert.NotNull(scene.LSceneLayoutKeys);
        Assert.NotNull(scene.LSceneTabNames);
        Assert.NotNull(scene.LSceneTabExports);
        Assert.NotNull(scene.LSceneTabLayouts);
        Assert.NotNull(scene.LSceneTabRelays);
    }

    [Fact]
    public void NormalizingMisalignedLists_AlignsThemToLayoutKeyCount()
    {
        LSceneRecord scene = TScene.RawCreate();
        scene.LSceneLayoutKeys = new() { "a", "b", "c" };
        scene.LSceneTabNames = new() { "only-one" };
        scene.LSceneTabLayouts = new()
        {
            TScene.RawTabCreate(),
            TScene.RawTabCreate(),
            TScene.RawTabCreate(),
            TScene.RawTabCreate()
        };
        scene.LSceneTabRelays = new() { 5 };

        TScene.Normalize(scene);

        Assert.Equal(3, scene.LSceneTabNames.Count);
        Assert.Equal(3, scene.LSceneTabExports.Count);
        Assert.Equal(3, scene.LSceneTabLayouts.Count);
        Assert.Equal(3, scene.LSceneTabRelays.Count);
        Assert.Equal(-1, scene.LSceneTabRelays[1]);
    }

    [Fact]
    public void NormalizingOutOfRangeIndex_ClampsIntoTabRange()
    {
        LSceneRecord scene = TScene.RawCreate();
        scene.LSceneLayoutKeys = new() { "a", "b" };
        scene.LSceneTabIndex = 99;

        TScene.Normalize(scene);

        Assert.Equal(1, scene.LSceneTabIndex);
    }

    [Fact]
    public void NormalizingNullInspectorSteps_ReplacesThemWithEmptyLists()
    {
        LSidecarEditRecord edit = TScene.RawEditCreate();
        edit.LSidecarSteps = null!;
        LSidecarAudioRecord audio = TScene.RawAudioCreate();
        audio.LSidecarSteps = null!;
        LSceneInspectorRecord inspector = TScene.RawInspectorCreate();
        inspector.LSceneInspectorEdit = edit;
        inspector.LSceneInspectorAudio = audio;
        LSceneTabRecord tab = TScene.RawTabCreate();
        tab.LSceneInspector = inspector;
        LSceneRecord scene = TScene.RawCreate();
        scene.LSceneLayoutKeys = new() { "a" };
        scene.LSceneTabLayouts = new() { tab };

        TScene.Normalize(scene);

        LSceneInspectorRecord applied = Assert.IsType<LSceneInspectorRecord>(scene.LSceneTabLayouts[0].LSceneInspector);
        Assert.NotNull(applied.LSceneInspectorEdit!.LSidecarSteps);
        Assert.NotNull(applied.LSceneInspectorAudio!.LSidecarSteps);
    }

    [Fact]
    public void NormalizingCatalogue_DropsBlankAndDuplicateNames()
    {
        var catalogue = new List<LSceneRecord>
        {
            NamedRaw("Keep"),
            NamedRaw("  "),
            NamedRaw("keep"),
            NamedRaw("Other")
        };

        IReadOnlyList<LSceneRecord> normalized = TScene.CatalogueNormalize(catalogue);

        Assert.Equal(2, normalized.Count);
        Assert.Equal("Keep", normalized[0].LSceneName);
        Assert.Equal("Other", normalized[1].LSceneName);
    }

    [Fact]
    public void NormalizingCatalogue_TrimsSurroundingWhitespaceInNames()
    {
        var catalogue = new List<LSceneRecord> { NamedRaw("  Spaced  ") };

        IReadOnlyList<LSceneRecord> normalized = TScene.CatalogueNormalize(catalogue);

        Assert.Equal("Spaced", Assert.Single(normalized).LSceneName);
    }

    private static LSceneRecord NamedRaw(string name)
    {
        LSceneRecord scene = TScene.RawCreate();
        scene.LSceneName = name;
        return scene;
    }
}
