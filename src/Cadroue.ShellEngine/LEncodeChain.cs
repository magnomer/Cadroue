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
}
