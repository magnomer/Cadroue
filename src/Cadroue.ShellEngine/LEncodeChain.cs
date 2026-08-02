using System.Globalization;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LEncodeChain
{
    internal static int LEncodePassRead(LWorkAudio lWorkAudio)
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

    internal static string? LEncodeChainBuild(LWorkAudio lWorkAudio, LEncodeChainMode lChainMode, int lTwoPassIndex)
    {
        var lFilters = new List<string>();
        for (int lIndex = 0; lIndex < lWorkAudio.LWorkAudioSteps.Count; lIndex++)
        {
            LWorkAudioStep lStep = lWorkAudio.LWorkAudioSteps[lIndex];
            if (!lStep.LWorkAudioStepActive)
            {
                continue;
            }

            switch (lStep)
            {
                case LWorkVolumeStep lVolume:
                    lFilters.Add(string.Create(
                        CultureInfo.InvariantCulture,
                        $"volume={lVolume.LWorkVolumeGain.ToString("0.###", CultureInfo.InvariantCulture)}dB"));
                    break;
                case LWorkNoiseStep lNoise:
                    string lNoiseType = lNoise.LWorkNoiseType switch
                    {
                        LWorkAudioNoiseType.LWorkAudioNoiseVinyl => "vinyl",
                        LWorkAudioNoiseType.LWorkAudioNoiseShellac => "shellac",
                        _ => "white"
                    };
                    string lDenoise = string.Create(
                        CultureInfo.InvariantCulture,
                        $"afftdn=nr={lNoise.LWorkNoiseReduction.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"nf={lNoise.LWorkNoiseFloor.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"rf={lNoise.LWorkNoiseResidual.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"ad={lNoise.LWorkNoiseAdaptivity.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"gs={lNoise.LWorkNoiseSmooth.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"nt={lNoiseType}");
                    if (lNoise.LWorkNoiseTrack)
                    {
                        lDenoise += ":tn=1";
                    }

                    lFilters.Add(lDenoise);
                    break;
                case LWorkPassStep lPass:
                    LEncodePassAppend(lFilters, lPass, lPass.LWorkPassHigh ? "highpass" : "lowpass");
                    break;
                case LWorkEqualizerStep lEqualizer:
                    foreach (LWorkEqualizerBand lBand in lEqualizer.LWorkEqualizerBands)
                    {
                        LEncodeEqualizerAppend(lFilters, lBand.LWorkEqualizerBandFrequency, lBand.LWorkEqualizerBandGain);
                    }

                    break;
                case LWorkNormalizeStep lNormalize:
                    if (lNormalize.LWorkNormalizeMode == LWorkAudioNormalizeMode.LWorkAudioNormalizeDynamic)
                    {
                        int lFrame = (int)Math.Clamp(Math.Round(lNormalize.LWorkNormalizeFrame), 10, 8000);
                        int lGauss = (int)Math.Clamp(Math.Round(lNormalize.LWorkNormalizeGauss), 3, 301);
                        if (lGauss % 2 == 0)
                        {
                            lGauss++;
                        }

                        double lMaxGain = Math.Clamp(lNormalize.LWorkNormalizeMaxGain, 1, 100);
                        string lDynamic = string.Create(
                            CultureInfo.InvariantCulture,
                            $"dynaudnorm=f={lFrame}:g={lGauss}:m={lMaxGain.ToString("0.###", CultureInfo.InvariantCulture)}:p=0.95");
                        if (lNormalize.LWorkNormalizeCompress >= 3)
                        {
                            lDynamic += string.Create(
                                CultureInfo.InvariantCulture,
                                $":s={Math.Clamp(lNormalize.LWorkNormalizeCompress, 3, 30).ToString("0.###", CultureInfo.InvariantCulture)}");
                        }

                        lFilters.Add(lDynamic);
                        break;
                    }

                    string lLoudnorm = string.Create(
                        CultureInfo.InvariantCulture,
                        $"loudnorm=I={lNormalize.LWorkNormalizeTarget.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"TP={lNormalize.LWorkNormalizePeak.ToString("0.###", CultureInfo.InvariantCulture)}:" +
                        $"LRA={lNormalize.LWorkNormalizeRange.ToString("0.###", CultureInfo.InvariantCulture)}");

                    if (lIndex == lTwoPassIndex)
                    {
                        lLoudnorm += lChainMode switch
                        {
                            LEncodeChainMode.LEncodeChainAnalyze => ":print_format=json",
                            LEncodeChainMode.LEncodeChainApply => LEncode.LEncodeMeasureToken,
                            _ => string.Empty
                        };
                    }

                    lFilters.Add(lLoudnorm);
                    break;
            }
        }

        return lFilters.Count > 0 ? string.Join(',', lFilters) : null;
    }

    private static void LEncodePassAppend(List<string> lFilters, LWorkPassStep lStep, string lFilterName)
    {
        int lStages = Math.Max(1, lStep.LWorkPassStages);
        int lPoles = lStep.LWorkPassPoles == 1 ? 1 : 2;
        string lFragment = string.Create(
            CultureInfo.InvariantCulture,
            $"{lFilterName}=f={lStep.LWorkPassFrequency.ToString("0.###", CultureInfo.InvariantCulture)}:" +
            $"poles={lPoles}:width_type=q:width={lStep.LWorkPassResonance.ToString("0.###", CultureInfo.InvariantCulture)}");

        for (int lStage = 0; lStage < lStages; lStage++)
        {
            lFilters.Add(lFragment);
        }
    }

    private static void LEncodeEqualizerAppend(List<string> lFilters, double lFrequency, double lGain)
    {
        if (lGain == 0)
        {
            return;
        }

        lFilters.Add(string.Create(
            CultureInfo.InvariantCulture,
            $"equalizer=f={lFrequency.ToString("0.###", CultureInfo.InvariantCulture)}:t=q:w=1:" +
            $"g={lGain.ToString("0.###", CultureInfo.InvariantCulture)}"));
    }
}
