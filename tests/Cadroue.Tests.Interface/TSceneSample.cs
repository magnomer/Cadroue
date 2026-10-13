using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.Tests.Interface;

internal static class TSceneSample
{
    internal static LSceneDocument TSceneCreate(string name, int marker)
    {
        return new LSceneDocument
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

    private static LPresetDocument TPresetCreate(int marker)
    {
        return new LPresetDocument
        {
            LPresetName = $"preset-{marker}",
            LPresetDisplay = $"display-{marker}",
            LPresetContainer = $"container-{marker}",
            LPresetExtension = $".x{marker}",
            LPresetCollision = $"collision-{marker}",
            LPresetCollisionSuffix = $"-{marker}",
            LPresetLocation = $"location-{marker}",
            LPresetLocationFolder = $"folder-{marker}",
            LPresetVideo = new LPresetVideoEntry
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
            LPresetAudio = new LPresetAudioEntry
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

    private static LSceneTabEntry TSceneLayoutCreate(int marker)
    {
        return new LSceneTabEntry
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

    private static LSceneFunnelCondition TFunnelMatchCreate(string kind, int marker) =>
        new()
        {
            LSceneFunnelText = $"{kind}-{marker}",
            LSceneFunnelCase = true,
            LSceneFunnelJoin = false
        };

    private static LSceneInspectorEntry TInspectorCreate(int marker)
    {
        return new LSceneInspectorEntry
        {
            LSceneInspectorAudio = new LSidecarAudioEntry
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
            LSceneInspectorEdit = new LSidecarEditEntry
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
}
