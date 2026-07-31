using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

public sealed record LEncodeStage(
    string LEncodeStageArguments,
    string LEncodeStageLabel,
    string LEncodeStageOutputPath,
    bool LEncodeStageTemporary,
    bool LEncodeStageMeasure = false);

internal enum LEncodeChainMode
{
    LEncodeChainPlain,
    LEncodeChainAnalyze,
    LEncodeChainApply
}

public static class LEncode
{
    private const string LEncodeBitrateOption = "-b:v";

    public const string LEncodeMeasureToken = "@@MEASURED@@";

    public const double LEncodeStatsPeriod = 0.5;

    public static IReadOnlyList<LEncodeStage> LEncodeStagesBuild(LWorkItem lWorkItem)
    {
        if (lWorkItem.LWorkKind == LWorkKind.LWorkKindAudio)
        {
            return LEncodeStepsBuild(lWorkItem);
        }

        return new[]
        {
            new LEncodeStage(LEncodeArgumentBuild(lWorkItem), "Encoding", lWorkItem.LWorkOutputPath, false)
        };
    }

    public static string LEncodeArgumentBuild(LWorkItem lWorkItem)
    {
        LWorkOutput lOutput = lWorkItem.LWorkOutput;
        var lArguments = new StringBuilder();

        LEncodeHeaderAppend(lArguments);

        if (lWorkItem.LWorkKind == LWorkKind.LWorkKindMerge)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -f concat -safe 0 -i {LEncodeFormat(LEncodeMergeSave(lWorkItem))}");
        }
        else
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -ss {LEncodeTimeFormat(lWorkItem.LWorkOrigin)}");
            if (lWorkItem.LWorkEnd > lWorkItem.LWorkOrigin)
            {
                lArguments.Append(CultureInfo.InvariantCulture, $" -to {LEncodeTimeFormat(lWorkItem.LWorkEnd)}");
            }
            lArguments.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        }

        LEncodeVideoAppend(lArguments, lWorkItem, lOutput);
        LEncodeAudioAppend(lArguments, lOutput);

        lArguments.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lWorkItem.LWorkOutputPath)}");
        return lArguments.ToString();
    }

    private static string LEncodeMergeSave(LWorkItem lWorkItem)
    {
        string lMergeListPath = Path.Combine(LDepot.LDepotMergeRead(), $"{lWorkItem.LWorkId:N}.txt");
        var lMergeList = new StringBuilder();
        foreach (string lMergeSource in lWorkItem.LWorkMergeSources)
        {
            string lMergeEscaped = lMergeSource.Replace("\\", "/", StringComparison.Ordinal).Replace("'", "'\\''", StringComparison.Ordinal);
            lMergeList.Append(CultureInfo.InvariantCulture, $"file '{lMergeEscaped}'\n");
        }

        File.WriteAllText(lMergeListPath, lMergeList.ToString());
        return lMergeListPath;
    }

    private static IReadOnlyList<LEncodeStage> LEncodeStepsBuild(LWorkItem lWorkItem)
    {
        LWorkOutput lOutput = lWorkItem.LWorkOutput;
        string lAudioFolder = LDepot.LDepotAudioRead();
        string lRawWav = Path.Combine(lAudioFolder, $"{lWorkItem.LWorkId:N}.raw.wav");
        string lProcessedWav = Path.Combine(lAudioFolder, $"{lWorkItem.LWorkId:N}.proc.wav");

        var lStages = new List<LEncodeStage>();

        var lExtract = new StringBuilder();
        LEncodeHeaderAppend(lExtract);
        lExtract.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lExtract.Append(" -vn -c:a pcm_s16le");
        lExtract.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lRawWav)}");
        lStages.Add(new LEncodeStage(lExtract.ToString(), "Extracting audio", lRawWav, true));

        string lAudioInputWav = lRawWav;
        int lTwoPassIndex = LEncodePassRead(lWorkItem.LWorkAudio);

        if (lWorkItem.LWorkAudio.LWorkAudioActive)
        {
            if (lTwoPassIndex >= 0)
            {
                string? lAnalyzeChain = LEncodeChainBuild(
                    lWorkItem.LWorkAudio, LEncodeChainMode.LEncodeChainAnalyze, lTwoPassIndex);
                var lAnalyze = new StringBuilder();
                LEncodeHeaderAppend(lAnalyze);
                lAnalyze.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lRawWav)}");
                lAnalyze.Append(CultureInfo.InvariantCulture, $" -af {LEncodeFormat(lAnalyzeChain!)}");
                lAnalyze.Append(" -f null -");
                lStages.Add(new LEncodeStage(lAnalyze.ToString(), "Analyzing audio", string.Empty, false, true));
            }

            string? lChain = LEncodeChainBuild(
                lWorkItem.LWorkAudio,
                lTwoPassIndex >= 0 ? LEncodeChainMode.LEncodeChainApply : LEncodeChainMode.LEncodeChainPlain,
                lTwoPassIndex);
            if (lChain is not null)
            {
                var lProcess = new StringBuilder();
                LEncodeHeaderAppend(lProcess);
                lProcess.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lRawWav)}");
                lProcess.Append(CultureInfo.InvariantCulture, $" -af {LEncodeFormat(lChain)}");
                lProcess.Append(" -c:a pcm_s16le");
                lProcess.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lProcessedWav)}");
                lStages.Add(new LEncodeStage(lProcess.ToString(), "Processing audio", lProcessedWav, true));
                lAudioInputWav = lProcessedWav;
            }
        }

        var lMux = new StringBuilder();
        LEncodeHeaderAppend(lMux);
        lMux.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lWorkItem.LWorkSourcePath)}");
        lMux.Append(CultureInfo.InvariantCulture, $" -i {LEncodeFormat(lAudioInputWav)}");

        bool lVideoExcluded = string.Equals(lOutput.LWorkOutputVideoStream, "Exclude", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lOutput.LWorkOutputVideoMode, "Exclude", StringComparison.OrdinalIgnoreCase);
        if (!lVideoExcluded)
        {
            lMux.Append(" -map 0:v:0?");
        }
        lMux.Append(" -map 1:a:0");

        if (!lVideoExcluded)
        {
            if (string.Equals(lOutput.LWorkOutputVideoMode, "Copy", StringComparison.OrdinalIgnoreCase)
                && !LEncodeVideoCheck(lWorkItem, lOutput))
            {
                lMux.Append(" -c:v copy");
            }
            else
            {
                LEncodeEncoderAppend(lMux, lWorkItem, lOutput);
            }
        }

        LEncodeMuxAppend(lMux, lOutput);
        lMux.Append(CultureInfo.InvariantCulture, $" {LEncodeFormat(lWorkItem.LWorkOutputPath)}");
        lStages.Add(new LEncodeStage(lMux.ToString(), "Encoding output", lWorkItem.LWorkOutputPath, false));

        return lStages;
    }

    private static int LEncodePassRead(LWorkAudio lWorkAudio)
    {
        int lFound = -1;
        for (int lIndex = 0; lIndex < lWorkAudio.LWorkAudioSteps.Count; lIndex++)
        {
            if (!lWorkAudio.LWorkAudioSteps[lIndex].LWorkStepLoudness)
            {
                continue;
            }

            if (lFound >= 0)
            {
                return -1;
            }

            lFound = lIndex;
        }

        return lFound;
    }

    private static string? LEncodeChainBuild(LWorkAudio lWorkAudio, LEncodeChainMode lChainMode, int lTwoPassIndex)
    {
        var lFilters = new List<string>();
        for (int lIndex = 0; lIndex < lWorkAudio.LWorkAudioSteps.Count; lIndex++)
        {
            LWorkAudioStep lStep = lWorkAudio.LWorkAudioSteps[lIndex];
            if (!lStep.LWorkAudioStepActive)
            {
                continue;
            }

            switch (lStep.LWorkAudioStepKind)
            {
                case LWorkAudioKind.LWorkAudioKindVolume:
                    lFilters.Add(string.Create(
                        CultureInfo.InvariantCulture,
                        $"volume={lStep.LWorkAudioStepGain.ToString("0.###", CultureInfo.InvariantCulture)}dB"));
                    break;
                case LWorkAudioKind.LWorkAudioKindNoiseReduction:
                    string lNoiseType = lStep.LWorkAudioStepNoiseType switch
                    {
                        LWorkAudioNoiseType.LWorkAudioNoiseVinyl => "vinyl",
                        LWorkAudioNoiseType.LWorkAudioNoiseShellac => "shellac",
                        _ => "white"
                    };
                    string lDenoise = string.Create(
                        CultureInfo.InvariantCulture,
                        $"afftdn=nr={lStep.LWorkAudioStepReduction.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"nf={lStep.LWorkAudioStepNoiseFloor.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"rf={lStep.LWorkAudioStepResidualFloor.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"ad={lStep.LWorkAudioStepAdaptivity.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"gs={lStep.LWorkAudioStepGainSmooth.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"nt={lNoiseType}");
                    if (lStep.LWorkAudioStepTrackNoise)
                    {
                        lDenoise += ":tn=1";
                    }

                    lFilters.Add(lDenoise);
                    break;
                case LWorkAudioKind.LWorkAudioKindHighPass:
                    LEncodePassAppend(lFilters, lStep, "highpass");
                    break;
                case LWorkAudioKind.LWorkAudioKindLowPass:
                    LEncodePassAppend(lFilters, lStep, "lowpass");
                    break;
                case LWorkAudioKind.LWorkAudioKindNormalize:
                    if (lStep.LWorkAudioStepMode == LWorkAudioNormalizeMode.LWorkAudioNormalizeDynamic)
                    {
                        lFilters.Add("dynaudnorm=f=500:g=15:p=0.95");
                        break;
                    }

                    string lLoudnorm = string.Create(
                        CultureInfo.InvariantCulture,
                        $"loudnorm=I={lStep.LWorkAudioStepTarget.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"TP={lStep.LWorkAudioStepPeak.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"LRA={lStep.LWorkAudioStepRange.ToString("0.###", CultureInfo.InvariantCulture)}");

                    if (lIndex == lTwoPassIndex)
                    {
                        lLoudnorm += lChainMode switch
                        {
                            LEncodeChainMode.LEncodeChainAnalyze => ":print_format=json",
                            LEncodeChainMode.LEncodeChainApply => LEncodeMeasureToken,
                            _ => string.Empty
                        };
                    }

                    lFilters.Add(lLoudnorm);
                    break;
            }
        }

        return lFilters.Count > 0 ? string.Join(',', lFilters) : null;
    }

    private static void LEncodePassAppend(List<string> lFilters, LWorkAudioStep lStep, string lFilterName)
    {
        int lStages = Math.Max(1, lStep.LWorkAudioStepStages);
        int lPoles = lStep.LWorkAudioStepPoles == 1 ? 1 : 2;
        string lFragment = string.Create(
            CultureInfo.InvariantCulture,
            $"{lFilterName}=f={lStep.LWorkAudioStepFrequency.ToString("0.###", CultureInfo.InvariantCulture)}:" +
            $"poles={lPoles}:width_type=q:width={lStep.LWorkAudioStepResonance.ToString("0.###", CultureInfo.InvariantCulture)}");

        for (int lStage = 0; lStage < lStages; lStage++)
        {
            lFilters.Add(lFragment);
        }
    }

    public static string LEncodeLoudnormRead(string lStderr)
    {
        int lStart = lStderr.LastIndexOf('{');
        int lEnd = lStderr.LastIndexOf('}');
        if (lStart < 0 || lEnd <= lStart)
        {
            return string.Empty;
        }

        try
        {
            Dictionary<string, string>? lValues =
                JsonSerializer.Deserialize<Dictionary<string, string>>(lStderr.Substring(lStart, lEnd - lStart + 1));
            if (lValues is null
                || !lValues.TryGetValue("input_i", out string? lInputI)
                || !lValues.TryGetValue("input_tp", out string? lInputTp)
                || !lValues.TryGetValue("input_lra", out string? lInputLra)
                || !lValues.TryGetValue("input_thresh", out string? lInputThresh)
                || !lValues.TryGetValue("target_offset", out string? lTargetOffset))
            {
                return string.Empty;
            }

            return $":measured_I={lInputI}:measured_TP={lInputTp}:measured_LRA={lInputLra}:" +
                $"measured_thresh={lInputThresh}:offset={lTargetOffset}:linear=true";
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    private static void LEncodeHeaderAppend(StringBuilder lArguments)
    {
        lArguments.Append("-hide_banner -nostdin -y");
        lArguments.Append(" -progress pipe:1 -nostats");
        lArguments.Append(CultureInfo.InvariantCulture,
            $" -stats_period {LEncodeStatsPeriod.ToString("0.###", CultureInfo.InvariantCulture)}");
    }

    private static void LEncodeVideoAppend(StringBuilder lArguments, LWorkItem lWorkItem, LWorkOutput lOutput)
    {
        if (string.Equals(lOutput.LWorkOutputVideoStream, "Exclude", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lOutput.LWorkOutputVideoMode, "Exclude", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -vn");
            return;
        }

        if (string.Equals(lOutput.LWorkOutputVideoMode, "Copy", StringComparison.OrdinalIgnoreCase)
            && !LEncodeVideoCheck(lWorkItem, lOutput))
        {
            lArguments.Append(" -c:v copy");
            return;
        }

        LEncodeEncoderAppend(lArguments, lWorkItem, lOutput);
    }

    private static void LEncodeEncoderAppend(StringBuilder lArguments, LWorkItem lWorkItem, LWorkOutput lOutput)
    {
        string lEncoderName = LCapability.LCapabilityNameRead(lOutput.LWorkOutputVideoEncoder);
        if (string.IsNullOrWhiteSpace(lEncoderName))
        {
            LEncodeFilterAppend(lArguments, lWorkItem, lOutput);
            return;
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" -c:v {lEncoderName}");

        LCapabilityCodec lCodec = LCapability.LCapabilityRead(lEncoderName);
        LCapabilityMode lMode = lCodec.LCapabilityModeFind(lOutput.LWorkOutputRateControl);
        LEncodeQualityAppend(lArguments, lEncoderName, lMode, lOutput.LWorkOutputQuality);

        if (lCodec.CapabilitySpeed is LCapabilitySpeed lSpeed && !string.IsNullOrWhiteSpace(lOutput.LWorkOutputSpeedPreset))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" {lSpeed.CapabilitySpeedOption} {lOutput.LWorkOutputSpeedPreset}");
        }

        foreach (var lExtra in lOutput.LWorkOutputVideoExtras)
        {
            if (string.IsNullOrWhiteSpace(lExtra.Value) || string.Equals(lExtra.Value, "none", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            lArguments.Append(CultureInfo.InvariantCulture, $" {lExtra.Key} {lExtra.Value}");
        }

        LEncodeFilterAppend(lArguments, lWorkItem, lOutput);
    }

    private static void LEncodeQualityAppend(
        StringBuilder lArguments,
        string lEncoderName,
        LCapabilityMode lMode,
        string lQualityValue)
    {
        if (lMode.CapabilityModeQuality is not LCapabilityQuality lQuality)
        {
            LEncodeLosslessAppend(lArguments, lEncoderName);
            return;
        }

        if (string.IsNullOrWhiteSpace(lQualityValue))
        {
            lQualityValue = lQuality.CapabilityQualityDefault;
        }

        lArguments.Append(CultureInfo.InvariantCulture, $" {lQuality.CapabilityQualityOption} {lQualityValue}");

        bool lNeedsZeroBitrate = lEncoderName is "libaom-av1" or "libvpx" or "libvpx-vp9";
        if (lNeedsZeroBitrate && !string.Equals(lQuality.CapabilityQualityOption, LEncodeBitrateOption, StringComparison.Ordinal))
        {
            lArguments.Append(" -b:v 0");
        }
    }

    private static void LEncodeLosslessAppend(StringBuilder lArguments, string lEncoderName)
    {
        switch (lEncoderName)
        {
            case "libx264":
                lArguments.Append(" -crf 0");
                break;
            case "libx265":
                lArguments.Append(" -x265-params lossless=1");
                break;
            case "libvpx-vp9":
                lArguments.Append(" -lossless 1");
                break;
            case "libwebp":
            case "libwebp_anim":
                lArguments.Append(" -lossless 1");
                break;
            case "h264_nvenc":
            case "hevc_nvenc":
            case "av1_nvenc":
                lArguments.Append(" -tune lossless");
                break;
            case "libaom-av1":
                lArguments.Append(" -aom-params lossless=1");
                break;
            case "ffv1":
                break;
        }
    }

    private static void LEncodeFilterAppend(StringBuilder lArguments, LWorkItem lWorkItem, LWorkOutput lOutput)
    {
        var lFilters = new List<string>();
        LWorkCrop lCrop = lWorkItem.LWorkCrop;

        string? lRotate = lCrop.LWorkCropRotation switch
        {
            90 => "transpose=1",
            180 => "transpose=1,transpose=1",
            270 => "transpose=2",
            _ => null
        };

        if (lRotate is not null)
        {
            lFilters.Add(lRotate);
        }

        if (lCrop.LWorkCropFlipHorizontal)
        {
            lFilters.Add("hflip");
        }

        if (lCrop.LWorkCropFlipVertical)
        {
            lFilters.Add("vflip");
        }

        if (lCrop.LWorkEdgeActive)
        {
            lFilters.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"crop=in_w-{lCrop.LWorkCropLeft}-{lCrop.LWorkCropRight}:in_h-{lCrop.LWorkCropTop}-{lCrop.LWorkCropBottom}:{lCrop.LWorkCropLeft}:{lCrop.LWorkCropTop}"));
        }

        LEncodeFiltersAppend(lFilters, lWorkItem.LWorkVideo);

        string? lSize = LEncodeSizeRead(lOutput.LWorkOutputVideoSize);
        if (lSize is not null)
        {
            lFilters.Add(LEncodeScaleResolve(lSize, lOutput.LWorkSizeReactive));
            lFilters.Add("setsar=1");
        }

        if (lFilters.Count > 0)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -vf {LEncodeFormat(string.Join(',', lFilters))}");
        }

        if (!LEncodeSourceCheck(lOutput.LWorkOutputVideoFps)
            && double.TryParse(lOutput.LWorkOutputVideoFps, NumberStyles.Float, CultureInfo.InvariantCulture, out double lFps))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -r {lFps.ToString(CultureInfo.InvariantCulture)}");
        }

        if (!string.IsNullOrWhiteSpace(lOutput.LWorkOutputPixelFormat)
            && !string.Equals(lOutput.LWorkOutputPixelFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -pix_fmt {lOutput.LWorkOutputPixelFormat}");
        }
    }

    private static void LEncodeFiltersAppend(List<string> lFilters, LWorkVideo lWorkVideo)
    {
        var lEqParts = new List<string>();
        foreach (LWorkVideoStep lStep in lWorkVideo.LWorkVideoSteps)
        {
            if (!lStep.LWorkVideoStepActive)
            {
                continue;
            }

            switch (lStep.LWorkVideoStepKind)
            {
                case LWorkVideoKind.LWorkVideoKindBrightness:
                    lEqParts.Add(
                        $"brightness={lStep.LWorkFfmpegValue.ToString("0.###", CultureInfo.InvariantCulture)}");
                    break;
                case LWorkVideoKind.LWorkVideoKindContrast:
                    lEqParts.Add(
                        $"contrast={lStep.LWorkFfmpegValue.ToString("0.###", CultureInfo.InvariantCulture)}");
                    break;
            }
        }

        if (lEqParts.Count > 0)
        {
            lFilters.Add("eq=" + string.Join(':', lEqParts));
        }
    }

    private static bool LEncodeVideoCheck(LWorkItem lWorkItem, LWorkOutput lOutput) =>
        lWorkItem.LWorkCrop.LWorkCropActive
        || lWorkItem.LWorkVideo.LWorkVideoActive
        || LEncodeSizeRead(lOutput.LWorkOutputVideoSize) is not null;

    private static void LEncodeAudioAppend(StringBuilder lArguments, LWorkOutput lOutput)
    {
        if (string.Equals(lOutput.LWorkOutputAudioStream, "Exclude", StringComparison.OrdinalIgnoreCase)
            || string.Equals(lOutput.LWorkOutputAudioMode, "Exclude", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -an");
            return;
        }

        if (string.Equals(lOutput.LWorkOutputAudioStream, "Include all audio tracks", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -map 0:v:0 -map 0:a");
        }

        if (string.Equals(lOutput.LWorkOutputAudioMode, "Copy", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(" -c:a copy");
            return;
        }

        LEncodeSettingsAppend(lArguments, lOutput, LEncodeTrackRead(lOutput.LWorkOutputAudioEncoder));
    }

    private static void LEncodeMuxAppend(StringBuilder lArguments, LWorkOutput lOutput)
    {
        string lAudioName = LEncodeTrackRead(lOutput.LWorkOutputAudioEncoder);
        if (string.IsNullOrWhiteSpace(lAudioName))
        {
            lAudioName = "aac";
        }

        LEncodeSettingsAppend(lArguments, lOutput, lAudioName);
    }

    private static void LEncodeSettingsAppend(StringBuilder lArguments, LWorkOutput lOutput, string lAudioName)
    {
        if (!string.IsNullOrWhiteSpace(lAudioName))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -c:a {lAudioName}");
        }

        if (!string.IsNullOrWhiteSpace(lOutput.LWorkOutputAudioBitrate)
            && !string.Equals(lOutput.LWorkOutputAudioBitrate, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -b:a {lOutput.LWorkOutputAudioBitrate}");
        }

        if (!LEncodeSourceCheck(lOutput.LWorkOutputAudioSampleRate)
            && int.TryParse(lOutput.LWorkOutputAudioSampleRate, out int lSampleRate))
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -ar {lSampleRate}");
        }

        int? lChannels = LEncodeChannelRead(lOutput.LWorkOutputAudioChannels);
        if (lChannels is int lChannelCount)
        {
            lArguments.Append(CultureInfo.InvariantCulture, $" -ac {lChannelCount}");
        }
    }

    private static string LEncodeTrackRead(string lAudioEncoder) => lAudioEncoder switch
    {
        "AAC" => "aac",
        "FLAC" => "flac",
        _ => LCapability.LCapabilityNameRead(lAudioEncoder)
    };

    private static int? LEncodeChannelRead(string lChannels) => lChannels switch
    {
        "Mono" => 1,
        "Stereo" => 2,
        "5.1" => 6,
        _ => null
    };

    private static string LEncodeScaleResolve(string lSize, bool lReactive)
    {
        string[] lParts = lSize.Split('x');
        int lWidth = int.Parse(lParts[0], CultureInfo.InvariantCulture);
        int lHeight = int.Parse(lParts[1], CultureInfo.InvariantCulture);

        int lShortEdge = Math.Min(lWidth, lHeight);
        int lLongEdge = Math.Max(lWidth, lHeight);
        if (!lReactive || lShortEdge == lLongEdge)
        {
            return string.Create(CultureInfo.InvariantCulture, $"scale={lWidth}:{lHeight}");
        }

        int lEdgeSpan = lLongEdge - lShortEdge;
        return string.Create(
            CultureInfo.InvariantCulture,
            $"scale=w={lShortEdge}+{lEdgeSpan}*gte(iw\\,ih):h={lLongEdge}-{lEdgeSpan}*gte(iw\\,ih)");
    }

    private static string? LEncodeSizeRead(string lSize)
    {
        if (LEncodeSourceCheck(lSize) || string.Equals(lSize, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string[] lParts = lSize.Split(['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lParts.Length != 2 || !int.TryParse(lParts[0], out int lWidth) || !int.TryParse(lParts[1], out int lHeight))
        {
            return null;
        }

        return $"{lWidth}x{lHeight}";
    }

    private static bool LEncodeSourceCheck(string lValue) =>
        string.IsNullOrWhiteSpace(lValue) || string.Equals(lValue, "Same as source", StringComparison.OrdinalIgnoreCase);

    private static string LEncodeTimeFormat(TimeSpan lTime) =>
        lTime.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);

    private static string LEncodeFormat(string lPath) => $"\"{lPath}\"";
}
