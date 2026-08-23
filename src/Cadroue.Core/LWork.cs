namespace Cadroue.Core;

public sealed record LWorkMedia(
    int LWorkMediaWidth,
    int LWorkMediaHeight,
    double LWorkMediaFramerate,
    long LWorkMediaMilliseconds,
    bool LWorkMediaVideo)
{
    public TimeSpan LWorkMediaDuration => TimeSpan.FromMilliseconds(LWorkMediaMilliseconds);

    public double? LWorkKeyframeInterval { get; init; }

    public string LWorkMediaCodec { get; init; } = "";

    public string LWorkAudioCodec { get; init; } = "";

    public int LWorkMediaBitrate { get; init; }

    public int LWorkMediaSamplerate { get; init; }

    public string LWorkMediaPixel { get; init; } = "";

    public string LWorkMediaRange { get; init; } = "";
}

public sealed record LWorkCrop(
    int LWorkCropLeft,
    int LWorkCropTop,
    int LWorkCropRight,
    int LWorkCropBottom,
    int LWorkCropRotation,
    bool LWorkFlipHorizontal,
    bool LWorkFlipVertical)
{
    public static LWorkCrop LWorkCropCreate() => new(0, 0, 0, 0, 0, false, false);

    public bool LWorkEdgeActive =>
        LWorkCropLeft > 0 || LWorkCropTop > 0 || LWorkCropRight > 0 || LWorkCropBottom > 0;

    public bool LWorkCropActive =>
        LWorkEdgeActive
        || LWorkCropRotation != 0
        || LWorkFlipHorizontal
        || LWorkFlipVertical;
}

public enum LColorKind
{
    LColorKindWhitebalance,
    LColorKindExposure,
    LColorKindBrightness,
    LColorKindContrast,
    LColorKindGamma,
    LColorKindSaturation,
    LColorKindCurve
}

public enum LWhitebalanceMethod
{
    LWhitebalanceMethodAverage,
    LWhitebalanceMethodMinmax,
    LWhitebalanceMethodMedian,
    LWhitebalanceMethodManual
}

public sealed record LWorkGammaSettings(
    double LWorkGammaGlobal,
    double LWorkGammaRed,
    double LWorkGammaGreen,
    double LWorkGammaBlue,
    double LWorkGammaHighlight);

public sealed record LWorkWhitebalanceSettings(
    LWhitebalanceMethod LWorkWhitebalanceMethod,
    double LWorkWhitebalanceSaturation)
{
    public double LWorkWhitebalanceRed { get; init; } = 1;
    public double LWorkWhitebalanceGreen { get; init; } = 1;
    public double LWorkWhitebalanceBlue { get; init; } = 1;
    public int LWorkSampleRed { get; init; }
    public int LWorkSampleGreen { get; init; }
    public int LWorkSampleBlue { get; init; }

    public IReadOnlyList<string> LWorkWhitebalanceFormat()
    {
        static string LWorkNumberFormat(double lValue) =>
            lValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        var lFilters = new List<string>();
        if (LWorkWhitebalanceMethod == LWhitebalanceMethod.LWhitebalanceMethodManual)
        {
            lFilters.Add(
                "colorchannelmixer=rr=" + LWorkNumberFormat(LWorkWhitebalanceRed)
                + ":gg=" + LWorkNumberFormat(LWorkWhitebalanceGreen)
                + ":bb=" + LWorkNumberFormat(LWorkWhitebalanceBlue));
            double lSaturation = LWorkWhitebalanceSaturation / 100d;
            if (lSaturation != 1)
            {
                lFilters.Add("eq=saturation=" + LWorkNumberFormat(lSaturation));
            }

            return lFilters;
        }

        string lAnalyze = LWorkWhitebalanceMethod switch
        {
            LWhitebalanceMethod.LWhitebalanceMethodAverage => "average",
            LWhitebalanceMethod.LWhitebalanceMethodMinmax => "minmax",
            _ => "median"
        };
        lFilters.Add(
            "colorcorrect=analyze=" + lAnalyze
            + ":saturation=" + LWorkNumberFormat(LWorkWhitebalanceSaturation / 100d));
        return lFilters;
    }
}

public sealed record LWorkCurvePoint(double LWorkCurveInput, double LWorkCurveOutput);

public sealed record LWorkCurveSettings(
    IReadOnlyList<LWorkCurvePoint> LWorkCurveMaster,
    IReadOnlyList<LWorkCurvePoint> LWorkCurveRed,
    IReadOnlyList<LWorkCurvePoint> LWorkCurveGreen,
    IReadOnlyList<LWorkCurvePoint> LWorkCurveBlue)
{
    public static bool LWorkIdentityCheck(IReadOnlyList<LWorkCurvePoint> lPoints) =>
        lPoints.Count == 2
        && lPoints[0].LWorkCurveInput == 0 && lPoints[0].LWorkCurveOutput == 0
        && lPoints[1].LWorkCurveInput == 1 && lPoints[1].LWorkCurveOutput == 1;

    public string LWorkCurveFormat()
    {
        static string LWorkNumberFormat(double lValue) =>
            lValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

        static string LWorkChannelFormat(IReadOnlyList<LWorkCurvePoint> lPoints) =>
            string.Join(" ", lPoints.Select(lPoint =>
                LWorkNumberFormat(lPoint.LWorkCurveInput)
                + "/" + LWorkNumberFormat(lPoint.LWorkCurveOutput)));

        var lParts = new List<string>();
        if (!LWorkIdentityCheck(LWorkCurveMaster))
        {
            lParts.Add("master='" + LWorkChannelFormat(LWorkCurveMaster) + "'");
        }

        if (!LWorkIdentityCheck(LWorkCurveRed))
        {
            lParts.Add("red='" + LWorkChannelFormat(LWorkCurveRed) + "'");
        }

        if (!LWorkIdentityCheck(LWorkCurveGreen))
        {
            lParts.Add("green='" + LWorkChannelFormat(LWorkCurveGreen) + "'");
        }

        if (!LWorkIdentityCheck(LWorkCurveBlue))
        {
            lParts.Add("blue='" + LWorkChannelFormat(LWorkCurveBlue) + "'");
        }

        if (lParts.Count == 0)
        {
            return "";
        }

        lParts.Add("interp=pchip");
        return "curves=" + string.Join(":", lParts);
    }
}

public sealed record LWorkVideoStep(
    LColorKind LWorkStepKind,
    bool LWorkStepActive,
    double LWorkStepValue)
{
    private static readonly IReadOnlyList<LWorkCurvePoint> LWorkCurveIdentity =
        new[] { new LWorkCurvePoint(0, 0), new LWorkCurvePoint(1, 1) };

    public LWorkGammaSettings? LWorkStepGamma { get; init; }

    public LWorkWhitebalanceSettings? LWorkStepWhitebalance { get; init; }

    public LWorkCurveSettings? LWorkStepCurve { get; init; }

    public static LWorkVideoStep LWorkBrightnessCreate(bool lStepActive, double lStepValue) =>
        new(LColorKind.LColorKindBrightness, lStepActive, lStepValue);

    public static LWorkVideoStep LWorkContrastCreate(bool lStepActive, double lStepValue) =>
        new(LColorKind.LColorKindContrast, lStepActive, Math.Clamp(lStepValue, 0, 200));

    public static LWorkVideoStep LWorkSaturationCreate(bool lStepActive, double lStepValue) =>
        new(LColorKind.LColorKindSaturation, lStepActive, Math.Clamp(lStepValue, 0, 200));

    public static LWorkVideoStep LWorkExposureCreate(bool lStepActive, double lStepValue) =>
        new(LColorKind.LColorKindExposure, lStepActive, Math.Clamp(lStepValue, -3, 3));

    public static LWorkVideoStep LWorkGammaCreate(
        bool lStepActive,
        double lStepGlobal,
        double lStepRed = 0,
        double lStepGreen = 0,
        double lStepBlue = 0,
        double lStepHighlightProtection = 0)
    {
        double lGlobal = Math.Clamp(lStepGlobal, -100, 100);
        return new(LColorKind.LColorKindGamma, lStepActive, lGlobal)
        {
            LWorkStepGamma = new LWorkGammaSettings(
                lGlobal,
                Math.Clamp(lStepRed, -100, 100),
                Math.Clamp(lStepGreen, -100, 100),
                Math.Clamp(lStepBlue, -100, 100),
                Math.Clamp(lStepHighlightProtection, 0, 100))
        };
    }

    public static LWorkVideoStep LWorkWhitebalanceCreate(
        bool lStepActive,
        LWhitebalanceMethod lStepMethod = LWhitebalanceMethod.LWhitebalanceMethodMedian,
        double lStepSaturation = 100,
        double lStepRed = 1,
        double lStepGreen = 1,
        double lStepBlue = 1,
        int lStepSampleRed = 0,
        int lStepSampleGreen = 0,
        int lStepSampleBlue = 0)
    {
        LWhitebalanceMethod lMethod = Enum.IsDefined(lStepMethod)
            ? lStepMethod
            : LWhitebalanceMethod.LWhitebalanceMethodMedian;
        double lSaturation = Math.Clamp(lStepSaturation, 0, 300);
        bool lManual = lMethod == LWhitebalanceMethod.LWhitebalanceMethodManual;
        return new(LColorKind.LColorKindWhitebalance, lStepActive, lSaturation)
        {
            LWorkStepWhitebalance = new LWorkWhitebalanceSettings(lMethod, lSaturation)
            {
                LWorkWhitebalanceRed = lManual ? LWorkCoefficientNormalize(lStepRed) : 1,
                LWorkWhitebalanceGreen = lManual ? LWorkCoefficientNormalize(lStepGreen) : 1,
                LWorkWhitebalanceBlue = lManual ? LWorkCoefficientNormalize(lStepBlue) : 1,
                LWorkSampleRed = lManual ? Math.Clamp(lStepSampleRed, 0, 255) : 0,
                LWorkSampleGreen = lManual ? Math.Clamp(lStepSampleGreen, 0, 255) : 0,
                LWorkSampleBlue = lManual ? Math.Clamp(lStepSampleBlue, 0, 255) : 0
            }
        };
    }

    private static double LWorkCoefficientNormalize(double lValue) =>
        double.IsFinite(lValue) ? Math.Clamp(lValue, 0, 2) : 1;

    public static LWorkVideoStep LWorkCurveCreate(
        bool lStepActive,
        IReadOnlyList<LWorkCurvePoint>? lStepMaster = null,
        IReadOnlyList<LWorkCurvePoint>? lStepRed = null,
        IReadOnlyList<LWorkCurvePoint>? lStepGreen = null,
        IReadOnlyList<LWorkCurvePoint>? lStepBlue = null) =>
        new(LColorKind.LColorKindCurve, lStepActive, 0)
        {
            LWorkStepCurve = new LWorkCurveSettings(
                LWorkCurveNormalize(lStepMaster),
                LWorkCurveNormalize(lStepRed),
                LWorkCurveNormalize(lStepGreen),
                LWorkCurveNormalize(lStepBlue))
        };

    private static IReadOnlyList<LWorkCurvePoint> LWorkCurveNormalize(
        IReadOnlyList<LWorkCurvePoint>? lPoints)
    {
        if (lPoints is null || lPoints.Count == 0)
        {
            return LWorkCurveIdentity;
        }

        var lResult = new List<LWorkCurvePoint>();
        foreach (LWorkCurvePoint lPoint in lPoints
            .Where(lEntry => double.IsFinite(lEntry.LWorkCurveInput)
                && double.IsFinite(lEntry.LWorkCurveOutput))
            .Select(lEntry => new LWorkCurvePoint(
                Math.Clamp(lEntry.LWorkCurveInput, 0, 1),
                Math.Clamp(lEntry.LWorkCurveOutput, 0, 1)))
            .OrderBy(lEntry => lEntry.LWorkCurveInput))
        {
            if (lResult.Count > 0 && lResult[^1].LWorkCurveInput == lPoint.LWorkCurveInput)
            {
                continue;
            }

            lResult.Add(lPoint);
        }

        return lResult.Count == 0 ? LWorkCurveIdentity : lResult;
    }

    public LWorkCurveSettings LWorkCurveRead() =>
        LWorkStepKind == LColorKind.LColorKindCurve && LWorkStepCurve is { } lCurve
            ? lCurve
            : new LWorkCurveSettings(
                LWorkCurveIdentity, LWorkCurveIdentity, LWorkCurveIdentity, LWorkCurveIdentity);

    public string LWorkCurveFormat() => LWorkCurveRead().LWorkCurveFormat();

    public LWorkGammaSettings LWorkGammaRead() =>
        LWorkStepKind == LColorKind.LColorKindGamma && LWorkStepGamma is { } lGamma
            ? lGamma
            : new LWorkGammaSettings(
                Math.Clamp(LWorkStepValue, -100, 100), 0, 0, 0, 0);

    public LWorkWhitebalanceSettings LWorkWhitebalanceRead()
    {
        LWorkWhitebalanceSettings lWhitebalance =
            LWorkStepKind == LColorKind.LColorKindWhitebalance && LWorkStepWhitebalance is { } lSettings
                ? lSettings
                : new LWorkWhitebalanceSettings(
                    LWhitebalanceMethod.LWhitebalanceMethodMedian,
                    LWorkStepKind == LColorKind.LColorKindWhitebalance ? LWorkStepValue : 100);
        LWhitebalanceMethod lMethod = Enum.IsDefined(lWhitebalance.LWorkWhitebalanceMethod)
            ? lWhitebalance.LWorkWhitebalanceMethod
            : LWhitebalanceMethod.LWhitebalanceMethodMedian;
        bool lManual = lMethod == LWhitebalanceMethod.LWhitebalanceMethodManual;
        return new LWorkWhitebalanceSettings(
            lMethod,
            Math.Clamp(lWhitebalance.LWorkWhitebalanceSaturation, 0, 300))
        {
            LWorkWhitebalanceRed = lManual
                ? LWorkCoefficientNormalize(lWhitebalance.LWorkWhitebalanceRed) : 1,
            LWorkWhitebalanceGreen = lManual
                ? LWorkCoefficientNormalize(lWhitebalance.LWorkWhitebalanceGreen) : 1,
            LWorkWhitebalanceBlue = lManual
                ? LWorkCoefficientNormalize(lWhitebalance.LWorkWhitebalanceBlue) : 1,
            LWorkSampleRed = lManual
                ? Math.Clamp(lWhitebalance.LWorkSampleRed, 0, 255) : 0,
            LWorkSampleGreen = lManual
                ? Math.Clamp(lWhitebalance.LWorkSampleGreen, 0, 255) : 0,
            LWorkSampleBlue = lManual
                ? Math.Clamp(lWhitebalance.LWorkSampleBlue, 0, 255) : 0
        };
    }

    public string LWorkDiagnosticRead()
    {
        string lSummary = $"{LWorkStepKind} {LWorkStepValue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}";
        if (LWorkStepKind == LColorKind.LColorKindWhitebalance)
        {
            LWorkWhitebalanceSettings lWhitebalance = LWorkWhitebalanceRead();
            string lMethod = lWhitebalance.LWorkWhitebalanceMethod switch
            {
                LWhitebalanceMethod.LWhitebalanceMethodAverage => "Average",
                LWhitebalanceMethod.LWhitebalanceMethodMinmax => "Minmax",
                LWhitebalanceMethod.LWhitebalanceMethodManual => "Manual",
                _ => "Median"
            };
            string lSaturation = lWhitebalance.LWorkWhitebalanceSaturation.ToString(
                "0.###", System.Globalization.CultureInfo.InvariantCulture);
            if (lWhitebalance.LWorkWhitebalanceMethod == LWhitebalanceMethod.LWhitebalanceMethodManual)
            {
                string lGains = string.Join("/", new[]
                {
                    lWhitebalance.LWorkWhitebalanceRed,
                    lWhitebalance.LWorkWhitebalanceGreen,
                    lWhitebalance.LWorkWhitebalanceBlue
                }.Select(lGain => lGain.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)));
                string lSample = string.Join("/", new[]
                {
                    lWhitebalance.LWorkSampleRed,
                    lWhitebalance.LWorkSampleGreen,
                    lWhitebalance.LWorkSampleBlue
                });
                return $"{lSummary} (method {lMethod}, saturation {lSaturation}, gain {lGains}, sample {lSample})";
            }

            return $"{lSummary} (method {lMethod}, saturation {lSaturation})";
        }

        if (LWorkStepKind == LColorKind.LColorKindCurve)
        {
            LWorkCurveSettings lCurve = LWorkCurveRead();
            var lChannels = new List<string>();
            if (!LWorkCurveSettings.LWorkIdentityCheck(lCurve.LWorkCurveMaster))
            {
                lChannels.Add($"master {lCurve.LWorkCurveMaster.Count}");
            }

            if (!LWorkCurveSettings.LWorkIdentityCheck(lCurve.LWorkCurveRed))
            {
                lChannels.Add($"red {lCurve.LWorkCurveRed.Count}");
            }

            if (!LWorkCurveSettings.LWorkIdentityCheck(lCurve.LWorkCurveGreen))
            {
                lChannels.Add($"green {lCurve.LWorkCurveGreen.Count}");
            }

            if (!LWorkCurveSettings.LWorkIdentityCheck(lCurve.LWorkCurveBlue))
            {
                lChannels.Add($"blue {lCurve.LWorkCurveBlue.Count}");
            }

            return lChannels.Count == 0
                ? "LColorKindCurve"
                : $"LColorKindCurve ({string.Join(", ", lChannels)})";
        }

        if (LWorkStepKind != LColorKind.LColorKindGamma)
        {
            return lSummary;
        }

        LWorkGammaSettings lGamma = LWorkGammaRead();
        var lDetails = new List<string>();
        if (lGamma.LWorkGammaRed != 0)
        {
            lDetails.Add($"red {lGamma.LWorkGammaRed.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        if (lGamma.LWorkGammaGreen != 0)
        {
            lDetails.Add($"green {lGamma.LWorkGammaGreen.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        if (lGamma.LWorkGammaBlue != 0)
        {
            lDetails.Add($"blue {lGamma.LWorkGammaBlue.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        if (lGamma.LWorkGammaHighlight != 0)
        {
            lDetails.Add($"highlight {lGamma.LWorkGammaHighlight.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        return lDetails.Count == 0 ? lSummary : $"{lSummary} ({string.Join(", ", lDetails)})";
    }

    public double LWorkFfmpegValue => LWorkStepKind switch
    {
        LColorKind.LColorKindBrightness => Math.Clamp(LWorkStepValue * 0.005d, -1, 1),
        LColorKind.LColorKindGamma => LWorkGammaResolve(LWorkStepValue),
        LColorKind.LColorKindExposure => LWorkStepValue,
        _ => LWorkStepValue / 100d
    };

    public static double LWorkGammaResolve(double lStepValue) =>
        Math.Pow(10d, Math.Clamp(lStepValue, -100, 100) / 100d);
}

public sealed record LWorkVideo(IReadOnlyList<LWorkVideoStep> LWorkVideoSteps)
{
    public static LWorkVideo LWorkVideoCreate() => new(Array.Empty<LWorkVideoStep>());

    public bool LWorkVideoActive => LWorkVideoSteps.Any(lStep =>
        lStep.LWorkStepActive
        && (lStep.LWorkStepKind != LColorKind.LColorKindCurve
            || lStep.LWorkCurveFormat().Length > 0));
}
