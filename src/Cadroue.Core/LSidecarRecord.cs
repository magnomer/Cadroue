using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Cadroue.Core;

public sealed class LSidecarEditRecord
{
    public int LSidecarCropLeft { get; set; }
    public int LSidecarCropTop { get; set; }
    public int LSidecarCropRight { get; set; }
    public int LSidecarCropBottom { get; set; }
    public int LSidecarRotation { get; set; }
    public bool LSidecarFlipHorizontal { get; set; }
    public bool LSidecarFlipVertical { get; set; }
    public bool LSidecarCropActive { get; set; }
    public bool LSidecarRatioFixed { get; set; }
    public bool LSidecarRatioLenient { get; set; }
    public int LSidecarRatioWidth { get; set; }
    public int LSidecarRatioHeight { get; set; }
    public bool LSidecarSkip { get; set; }
    public List<LSidecarVideoStep> LSidecarSteps { get; set; } = new();

    public bool LSidecarEditActive =>
        LSidecarCropActive
        || LSidecarSkip
        || LSidecarCropLeft > 0 || LSidecarCropTop > 0 || LSidecarCropRight > 0 || LSidecarCropBottom > 0
        || LSidecarRotation != 0 || LSidecarFlipHorizontal || LSidecarFlipVertical
        || LSidecarRatioFixed
        || LSidecarSteps.Any(lStep => lStep.LSidecarActive);
}

public sealed class LSidecarVideoStep
{
    public string LSidecarKind { get; set; } = string.Empty;
    public bool LSidecarActive { get; set; }
    public double LSidecarValue { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? LSidecarGammaRed { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? LSidecarGammaGreen { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? LSidecarGammaBlue { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? LSidecarGammaHighlight { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LWhitebalanceMethod? LSidecarWhitebalanceMethod { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? LSidecarWhitebalanceSaturation { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? LSidecarWhitebalanceRed { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? LSidecarWhitebalanceGreen { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? LSidecarWhitebalanceBlue { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LSidecarSampleRed { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LSidecarSampleGreen { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LSidecarSampleBlue { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<LSidecarCurveChannel>? LSidecarCurveChannels { get; set; }
}

public sealed class LSidecarCurveChannel
{
    public string LSidecarCurveName { get; set; } = string.Empty;
    public List<LSidecarCurvePoint> LSidecarCurvePoints { get; set; } = new();
}

public sealed class LSidecarCurvePoint
{
    public double LSidecarCurveInput { get; set; }
    public double LSidecarCurveOutput { get; set; }
}

public sealed class LSidecarAudioStep
{
    public string LSidecarKind { get; set; } = string.Empty;
    public bool LSidecarActive { get; set; }
    public double LSidecarGain { get; set; }
    public string LSidecarMode { get; set; } = "Loudness";
    public double LSidecarTarget { get; set; } = -16;
    public double LSidecarPeak { get; set; } = -1.5;
    public double LSidecarRange { get; set; } = 11;
    public bool LSidecarTwoPass { get; set; }
    public double LSidecarFrame { get; set; } = 300;
    public double LSidecarGauss { get; set; } = 21;
    public double LSidecarMaxGain { get; set; } = 10;
    public double LSidecarCompress { get; set; } = 6;
    public double LSidecarReduction { get; set; } = 12;
    public double LSidecarNoiseFloor { get; set; } = -50;
    public bool LSidecarTrackNoise { get; set; }
    public double LSidecarFrequency { get; set; }
    public int LSidecarStages { get; set; } = 1;
    public int LSidecarPoles { get; set; } = 2;
    public double LSidecarResonance { get; set; } = 0.707;
    public string LSidecarNoiseType { get; set; } = "White";
    public double LSidecarGainSmooth { get; set; }
    public double LSidecarAdaptivity { get; set; } = 0.5;
    public double LSidecarResidualFloor { get; set; } = -38;
    public List<LSidecarEqualizerBand> LSidecarEqualizerBands { get; set; } = new();
}

public sealed class LSidecarEqualizerBand
{
    public double LSidecarBandFrequency { get; set; } = 1000;
    public double LSidecarBandGain { get; set; }
}

public sealed class LSidecarAudioRecord
{
    public bool LSidecarSkip { get; set; }
    public List<LSidecarAudioStep> LSidecarSteps { get; set; } = new();

    public bool LSidecarAudioActive => LSidecarSkip || LSidecarSteps.Any(lStep => lStep.LSidecarActive);
}

public sealed class LSidecarFixStep
{
    public string LSidecarKind { get; set; } = string.Empty;
    public bool LSidecarRepair { get; set; }
    public bool LSidecarDiagnosis { get; set; }
    public bool LSidecarPersistent { get; set; }
}

public sealed class LSidecarFixRecord
{
    public List<LSidecarFixStep> LSidecarSteps { get; set; } = new();
    public bool LSidecarSalvageActive { get; set; }
    public string LSidecarSalvageMode { get; set; } = "Rejoin";
    public string LSidecarSalvageBasis { get; set; } = "Source";
    public bool LSidecarSalvagePersistent { get; set; }

    public bool LSidecarFixActive =>
        LSidecarSalvageActive || LSidecarSteps.Any(lStep => lStep.LSidecarRepair || lStep.LSidecarDiagnosis);
}

public sealed class LSidecarSectionRecord
{
    public long LSidecarStartMilliseconds { get; set; }
    public long LSidecarEndMilliseconds { get; set; }
    public int LSidecarColorIndex { get; set; }
    public string LSidecarName { get; set; } = string.Empty;
    public string LSidecarPrefix { get; set; } = string.Empty;
    public string LSidecarSuffix { get; set; } = string.Empty;
    public bool LSidecarHidden { get; set; }
    public bool LSidecarDetected { get; set; }
}

public sealed class LSidecarDetectorRecord
{
    public int LSidecarDetectorKind { get; set; }
    public bool LSidecarDetectorEnabled { get; set; }
    public double LSidecarDetectorThreshold { get; set; }
    public double LSidecarDetectorMinimum { get; set; }
    public double LSidecarDetectorWindow { get; set; } = LDetector.LDetectorWindowRead(LDetectorKind.LDetectorKindLuminance).LDetectorBoundDefault;
    public int LSidecarDetectorType { get; set; }
    public double LSidecarDetectorHue { get; set; }
    public double LSidecarDetectorSaturation { get; set; }
    public double LSidecarDetectorBrightness { get; set; } = LDetectorBlank.LDetectorBlankValue;
    public double LSidecarDetectorTolerance { get; set; } = LDetector.LDetectorToleranceRead().LDetectorBoundDefault;
    public double LSidecarDetectorCoverage { get; set; } = LDetector.LDetectorCoverageRead().LDetectorBoundDefault;
    public string LSidecarDetectorPreset { get; set; } = "Normal";
}

public sealed class LSidecarSplitRecord
{
    public bool LSidecarSplitPersistent { get; set; }
    public List<LSidecarDetectorRecord> LSidecarSplitDetectors { get; set; } = new();

    public bool LSidecarSplitActive =>
        LSidecarSplitPersistent || LSidecarSplitDetectors.Any(lDetector => lDetector.LSidecarDetectorEnabled);
}

public sealed class LSidecarWaveformRecord
{
    public int LSidecarBucketMilliseconds { get; set; }
    public long LSidecarDurationMilliseconds { get; set; }
    public string LSidecarPeaks { get; set; } = string.Empty;
    public string LSidecarRms { get; set; } = string.Empty;
}

public sealed class LSidecarSourceRecord
{
    public string LSidecarFileName { get; set; } = string.Empty;
    public string LSidecarRelativePath { get; set; } = string.Empty;
    public string LSidecarAbsolutePath { get; set; } = string.Empty;
    public long LSidecarLength { get; set; }
    public long LSidecarWriteTicks { get; set; }
    public long LSidecarDurationMilliseconds { get; set; }
    public string LSidecarPartialHash { get; set; } = string.Empty;
}
