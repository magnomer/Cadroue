using System.Collections.Generic;
using System.Linq;

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
