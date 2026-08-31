using System.Collections;
using System.Reflection;

using Cadroue.Core;
using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests;

[CollectionDefinition("Scene", DisableParallelization = true)]
public sealed class LSceneCollection { }

internal sealed class TSceneValue
{
    internal TSceneValue(LSceneRecord scene)
    {
        TSceneData = scene;
    }

    internal LSceneRecord TSceneData { get; }
}

internal sealed class TScene : IDisposable
{
    private const string TSceneStoreName = "LScenePresets.json";
    private readonly string tSceneRoot;

    internal TScene()
    {
        tSceneRoot = Path.Combine(Path.GetTempPath(), "cadroue-scene-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tSceneRoot);
        LScene.LSceneRootSet(tSceneRoot);
    }

    internal TSceneValue TSceneRecordCreate(string name, int marker = 1) => new(TSceneCreate(name, marker));

    internal void TSceneSave(TSceneValue scene) => LScene.LSceneSave(scene.TSceneData);

    internal TSceneValue? TSceneRead(string name)
    {
        LSceneRecord? scene = LScene.LSceneRead(name);
        return scene is null ? null : new TSceneValue(scene);
    }

    internal bool TSceneDelete(string name) => LScene.LSceneDelete(name);

    internal IReadOnlyList<string> TSceneNames => LScene.LSceneNames;

    internal void TSceneDiskLoad() => LScene.LSceneRootSet(tSceneRoot);

    internal bool TScenePersistCheck(TSceneValue left, TSceneValue right) =>
        TSceneValueCheck(left.TSceneData, right.TSceneData);

    internal TSceneValue TSceneExternalMatch(TSceneValue scene)
    {
        string path = Path.Combine(tSceneRoot, "external.cadroue-scene");
        LScene.LSceneFileSave(scene.TSceneData, path);
        return new TSceneValue(
            LScene.LSceneFileLoad(path)
            ?? throw new InvalidDataException("Production returned no scene for a scene file it saved."));
    }

    internal static LSceneRecord TSceneRawCreate() => new();

    internal static LSceneTabRecord TSceneTabCreate() => new();

    internal static LSceneInspectorRecord TSceneInspectorCreate() => new();

    internal static LSidecarEditRecord TSceneEditCreate() => new();

    internal static LSidecarAudioRecord TSceneAudioCreate() => new();

    internal static LSceneRecord TSceneNormalize(LSceneRecord? raw) => LScene.LSceneNormalize(raw);

    internal static int TSceneVersionCurrent => LScene.LSceneVersionCurrent;

    internal static IReadOnlyList<LSceneRecord> TSceneCatalogueNormalize(List<LSceneRecord>? raw) =>
        LScene.LSceneCatalogueNormalize(raw);

    internal void TSceneMalformSave()
    {
        File.WriteAllText(Path.Combine(tSceneRoot, TSceneStoreName), "{ definitely-not-a-scene");
    }

    internal bool TSceneMatch(TSceneValue left, TSceneValue right) =>
        LScene.LSceneMatch(left.TSceneData, right.TSceneData);

    internal void TSceneFieldChange(TSceneValue scene)
    {
        scene.TSceneData.LSceneTabLayouts[0].LSceneAutoRelay =
            !scene.TSceneData.LSceneTabLayouts[0].LSceneAutoRelay;
    }

    internal void TSceneNameChange(TSceneValue scene, string name)
    {
        scene.TSceneData.LSceneName = name;
    }

    internal void TSceneTabChange(TSceneValue scene) => scene.TSceneData.LSceneTabIndex = 0;

    internal void TSceneWidthChange(TSceneValue scene) =>
        scene.TSceneData.LSceneTabLayouts[0].LScenePanelWidths.Add(999);

    internal void TSceneScaleCreate(TSceneValue scene, double scale)
    {
        foreach (LSceneTabRecord layout in scene.TSceneData.LSceneTabLayouts)
        {
            for (int index = 0; index < layout.LScenePanelWidths.Count; index++)
            {
                layout.LScenePanelWidths[index] *= scale;
            }
        }
    }

    internal void TSceneVersionChange(TSceneValue scene) =>
        scene.TSceneData.LSceneVersion = 0;

    internal void TSceneReverseCreate(TSceneValue scene) =>
        scene.TSceneData.LSceneTabNames.Reverse();

    internal void TSceneWidthCreate(TSceneValue scene)
    {
        foreach (LSceneTabRecord layout in scene.TSceneData.LSceneTabLayouts)
        {
            layout.LScenePanelWidths.Reverse();
        }
    }

    internal void TSceneCurrentSave(TSceneValue scene)
    {
        LScene.LSceneStateSave(scene.TSceneData);
        LScene.LSceneCurrentLoad();
    }

    internal string TSceneCurrentName => LScene.LSceneCurrent.LSceneName;

    internal string TSceneActiveName => LScene.LSceneActiveName;

    public void Dispose()
    {
        LScene.LSceneRootSet(null);
        try
        {
            Directory.Delete(tSceneRoot, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static LSceneRecord TSceneCreate(string name, int marker)
    {
        return new LSceneRecord
        {
            LSceneVersion = LScene.LSceneVersionCurrent,
            LSceneName = name,
            LSceneLayoutKeys = new() { $"layout-{marker}", $"layout-{marker + 1}" },
            LSceneTabNames = new() { $"tab-{marker}", $"tab-{marker + 1}" },
            LSceneTabExports = new() { TPresetCreate(marker), TPresetCreate(marker + 1) },
            LSceneTabLayouts = new() { TSceneLayoutCreate(marker), TSceneLayoutCreate(marker + 1) },
            LSceneTabRelays = new() { marker + 10, marker + 20 },
            LSceneTabIndex = 1
        };
    }

    private static LPresetRecord TPresetCreate(int marker)
    {
        return new LPresetRecord
        {
            LPresetName = $"preset-{marker}",
            LPresetDisplay = $"display-{marker}",
            LPresetContainer = $"container-{marker}",
            LPresetExtension = $".x{marker}",
            LPresetCollision = $"collision-{marker}",
            LPresetCollisionSuffix = $"-{marker}",
            LPresetLocation = $"location-{marker}",
            LPresetLocationFolder = $"folder-{marker}",
            LPresetVideo = new LPresetVideoRecord
            {
                LPresetStream = $"video-stream-{marker}",
                LPresetMode = $"video-mode-{marker}",
                LPresetEncoder = $"video-encoder-{marker}",
                LPresetRateControl = $"video-rate-{marker}",
                LPresetQuality = $"video-quality-{marker}",
                LPresetSpeedPreset = $"video-speed-{marker}",
                LPresetSize = $"video-size-{marker}",
                LPresetSizeReactive = true,
                LPresetFps = $"fps-{marker}",
                LPresetPixelLayout = $"pixels-{marker}",
                LPresetExtras = new() { [$"video-extra-{marker}"] = $"value-{marker}" }
            },
            LPresetAudio = new LPresetAudioRecord
            {
                LPresetStream = $"audio-stream-{marker}",
                LPresetMode = $"audio-mode-{marker}",
                LPresetEncoder = $"audio-encoder-{marker}",
                LPresetRateControl = $"audio-rate-{marker}",
                LPresetQuality = $"audio-quality-{marker}",
                LPresetSpeed = $"audio-speed-{marker}",
                LPresetExtras = new() { [$"audio-extra-{marker}"] = $"value-{marker}" },
                LPresetSampleRate = $"sample-{marker}",
                LPresetChannels = $"channels-{marker}"
            }
        };
    }

    private static LSceneTabRecord TSceneLayoutCreate(int marker)
    {
        return new LSceneTabRecord
        {
            LScenePanelWidths = new() { marker + 0.25, marker + 0.75 },
            LSceneExportHidden = true,
            LScenePanelsCollapsed = new() { marker, marker + 1 },
            LSceneFunnelRules = new() { TFunnelRuleCreate(marker) },
            LSceneInspector = TInspectorCreate(marker),
            LSceneGroupAuto = true,
            LSceneGroupStrict = false,
            LSceneGroupMode = LSeriesNameMode.LSeriesNameFirst,
            LSceneAutoRelay = marker % 2 == 0
        };
    }

    private static LSceneFunnelRule TFunnelRuleCreate(int marker)
    {
        return new LSceneFunnelRule
        {
            LSceneFunnelContains = TFunnelMatchCreate("contains", marker),
            LSceneFunnelPrefix = TFunnelMatchCreate("start", marker),
            LSceneFunnelEnd = TFunnelMatchCreate("end", marker),
            LSceneFunnelExtension = TFunnelMatchCreate("extension", marker),
            LSceneFunnelType = marker,
            LSceneFunnelRegex = $"regex-{marker}",
            LSceneFunnelWhole = true,
            LSceneFunnelTarget = marker + 2
        };
    }

    private static LSceneFunnelMatch TFunnelMatchCreate(string kind, int marker) =>
        new()
        {
            LSceneFunnelText = $"{kind}-{marker}",
            LSceneFunnelCase = true,
            LSceneFunnelJoin = false
        };

    private static LSceneInspectorRecord TInspectorCreate(int marker)
    {
        return new LSceneInspectorRecord
        {
            LSceneInspectorAudio = new LSidecarAudioRecord
            {
                LSidecarSkip = true,
                LSidecarSteps = new()
                {
                    new LSidecarAudioStep
                    {
                        LSidecarKind = $"audio-kind-{marker}",
                        LSidecarActive = true,
                        LSidecarGain = marker + 0.1,
                        LSidecarMode = $"audio-mode-{marker}",
                        LSidecarTarget = marker + 0.2,
                        LSidecarPeak = marker + 0.3,
                        LSidecarRange = marker + 0.4,
                        LSidecarTwoPass = true,
                        LSidecarFrame = marker + 0.5,
                        LSidecarGauss = marker + 0.6,
                        LSidecarMaxGain = marker + 0.7,
                        LSidecarCompress = marker + 0.8,
                        LSidecarReduction = marker + 0.9,
                        LSidecarNoiseFloor = marker + 1.1,
                        LSidecarTrackNoise = true,
                        LSidecarFrequency = marker + 1.2,
                        LSidecarStages = marker + 3,
                        LSidecarPoles = marker + 4,
                        LSidecarResonance = marker + 1.3,
                        LSidecarNoiseType = $"noise-{marker}",
                        LSidecarGainSmooth = marker + 1.4,
                        LSidecarAdaptivity = marker + 1.5,
                        LSidecarResidualFloor = marker + 1.6,
                        LSidecarEqualizerBands = new()
                        {
                            new LSidecarEqualizerBand
                            {
                                LSidecarBandFrequency = marker + 100,
                                LSidecarBandGain = marker + 2.5
                            }
                        }
                    }
                }
            },
            LSceneInspectorEdit = new LSidecarEditRecord
            {
                LSidecarCropLeft = marker + 1,
                LSidecarCropTop = marker + 2,
                LSidecarCropRight = marker + 3,
                LSidecarCropBottom = marker + 4,
                LSidecarRotation = 90,
                LSidecarFlipHorizontal = true,
                LSidecarFlipVertical = true,
                LSidecarCropActive = true,
                LSidecarRatioFixed = true,
                LSidecarRatioLenient = true,
                LSidecarRatioWidth = marker + 16,
                LSidecarRatioHeight = marker + 9,
                LSidecarSkip = true,
                LSidecarSteps = new()
                {
                    new LSidecarVideoStep
                    {
                        LSidecarKind = $"video-kind-{marker}",
                        LSidecarActive = true,
                        LSidecarValue = marker + 3.5
                    }
                }
            },
            LSceneInspectorCrop = true,
            LSceneInspectorSkip = true
        };
    }

    private static bool TSceneValueCheck(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.GetType() != right.GetType())
        {
            return false;
        }

        Type type = left.GetType();
        if (type.IsValueType || left is string)
        {
            return left.Equals(right);
        }

        if (left is IEnumerable leftItems && right is IEnumerable rightItems)
        {
            IEnumerator leftIterator = leftItems.GetEnumerator();
            IEnumerator rightIterator = rightItems.GetEnumerator();
            while (true)
            {
                bool leftNext = leftIterator.MoveNext();
                bool rightNext = rightIterator.MoveNext();
                if (leftNext != rightNext)
                {
                    return false;
                }

                if (!leftNext)
                {
                    return true;
                }

                if (!TSceneValueCheck(leftIterator.Current, rightIterator.Current))
                {
                    return false;
                }
            }
        }

        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .All(property => TSceneValueCheck(property.GetValue(left), property.GetValue(right)));
    }
}
