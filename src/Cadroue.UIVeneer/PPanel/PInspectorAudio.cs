using System.Globalization;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    public event Action? PInspectorAudioChange;

    private void PInspectorActiveRaise() => PInspectorAudioChange?.Invoke();

    public LWorkAudioStep PInspectorStepRead(LAudioKind pStepKind) => pStepKind switch
    {
        LAudioKind.LAudioKindLeveling => LWorkAudioStep.LWorkNormalizeCreate(
            pLoudnessApplyBox.IsChecked == true,
            PLoudnessModeRead(),
            PInspectorDecimalRead(pLoudnessTarget, -16),
            PInspectorDecimalRead(pLoudnessPeak, -1.5),
            PInspectorDecimalRead(pLoudnessRange, 11),
            pLoudnessTwoPass.IsChecked == true,
            PInspectorDecimalRead(pDynamicFrame, LLevelingCatalog.LLevelingDefaultRead().LLevelingFrame),
            PInspectorDecimalRead(pDynamicGauss, LLevelingCatalog.LLevelingDefaultRead().LLevelingGauss),
            PInspectorDecimalRead(pDynamicMaxGain, LLevelingCatalog.LLevelingDefaultRead().LLevelingMaxGain),
            PInspectorDecimalRead(pDynamicCompress, LLevelingCatalog.LLevelingDefaultRead().LLevelingCompress)),
        LAudioKind.LAudioKindDenoise => LWorkAudioStep.LWorkNoiseCreate(
            pNoiseApplyBox.IsChecked == true,
            Math.Clamp(
                PInspectorDecimalRead(pNoiseReductionValue, 12),
                LGrainCatalog.LGrainReductionLeast,
                LGrainCatalog.LGrainReductionMost),
            PInspectorDecimalRead(pNoiseFloor, -50),
            pNoiseTrack.IsChecked == true,
            PNoiseTypeRead(),
            Math.Clamp(
                PInspectorDecimalRead(pNoiseSmoothValue, 6),
                LGrainCatalog.LGrainSmoothLeast,
                LGrainCatalog.LGrainSmoothMost),
            Math.Clamp(PInspectorDecimalRead(pNoiseAdaptivity, 0.5), 0, 1),
            PInspectorDecimalRead(pNoiseResidual, -38)),
        LAudioKind.LAudioKindHighpass => LWorkAudioStep.LWorkHighCreate(
            pInspectorHighPass.PFilterApplyBox.IsChecked == true,
            PInspectorPassRead(pInspectorHighPass),
            PFilterStagesRead(pInspectorHighPass),
            PFilterPolesRead(pInspectorHighPass),
            PFilterResonanceRead(pInspectorHighPass)),
        LAudioKind.LAudioKindLowpass => LWorkAudioStep.LWorkLowCreate(
            pInspectorLowPass.PFilterApplyBox.IsChecked == true,
            PInspectorPassRead(pInspectorLowPass),
            PFilterStagesRead(pInspectorLowPass),
            PFilterPolesRead(pInspectorLowPass),
            PFilterResonanceRead(pInspectorLowPass)),
        LAudioKind.LAudioKindEqualizer => PEqualizerStepRead(),
        _ => LWorkAudioStep.LWorkVolumeCreate(
            pVolumeApplyBox.IsChecked == true,
            Math.Clamp(
                PInspectorDecimalRead(pInspectorVolumeValue, 0),
                LWorkAudio.LWorkGainLeast,
                LWorkAudio.LWorkGainMost))
    };

    public void PInspectorPlanApply(LWorkAudio pInspectorPlan)
    {
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LAudioKind.LAudioKindHighpass)
                ?? LPassband.LPassbandStepCreate(true, false));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LAudioKind.LAudioKindLowpass)
                ?? LPassband.LPassbandStepCreate(false, false));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LAudioKind.LAudioKindDenoise)
                ?? LWorkAudioStep.LWorkNoiseCreate(false, 12, -50, false, LGrain.LGrainWhite, 6, 0.5, -38));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(
                pStep => pStep.LWorkStepKind == LAudioKind.LAudioKindEqualizer)
                ?? LWorkAudioStep.LWorkEqualizerCreate(false, LWorkEqualizerStep.LWorkBandsCreate()));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LAudioKind.LAudioKindVolume)
                ?? LWorkAudioStep.LWorkVolumeCreate(false, 0));
        PInspectorStepApply(
            pInspectorPlan.LWorkAudioSteps.FirstOrDefault(pStep => pStep.LWorkStepKind == LAudioKind.LAudioKindLeveling)
                ?? LAudio.LAudioNormalizeCreate());
        PInspectorActiveRaise();
    }

    public void PInspectorMediaReset()
    {
        LWorkAudio pCurrent = PInspectorPersistentRead();
        PInspectorPlanApply(pCurrent);
    }

    public bool PInspectorPersistentCheck() =>
        pInspectorVolumePersistent.IsChecked == true
        || pLoudnessPersistent.IsChecked == true
        || pNoisePersistent.IsChecked == true
        || pInspectorHighPass.PInspectorPassPersistent.IsChecked == true
        || pInspectorLowPass.PInspectorPassPersistent.IsChecked == true
        || pEqualizerPersistent.IsChecked == true
        || pSkipPersistentBox.IsChecked == true;

    public void PInspectorPersistentApply(LWorkAudio pInspectorPlan, bool pSkipPersistent)
    {
        foreach (LWorkAudioStep pStep in pInspectorPlan.LWorkAudioSteps)
        {
            switch (pStep.LWorkStepKind)
            {
                case LAudioKind.LAudioKindHighpass:
                    pInspectorHighPass.PInspectorPassPersistent.IsChecked = true;
                    break;
                case LAudioKind.LAudioKindLowpass:
                    pInspectorLowPass.PInspectorPassPersistent.IsChecked = true;
                    break;
                case LAudioKind.LAudioKindDenoise:
                    pNoisePersistent.IsChecked = true;
                    break;
                case LAudioKind.LAudioKindVolume:
                    pInspectorVolumePersistent.IsChecked = true;
                    break;
                case LAudioKind.LAudioKindLeveling:
                    pLoudnessPersistent.IsChecked = true;
                    break;
                case LAudioKind.LAudioKindEqualizer:
                    pEqualizerPersistent.IsChecked = true;
                    break;
            }
        }

        pSkipPersistentBox.IsChecked = pSkipPersistent;
    }

    public LWorkAudio PInspectorPersistentRead()
    {
        var pSteps = new List<LWorkAudioStep>();
        if (pInspectorHighPass.PInspectorPassPersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LAudioKind.LAudioKindHighpass));
        }

        if (pInspectorLowPass.PInspectorPassPersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LAudioKind.LAudioKindLowpass));
        }

        if (pNoisePersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LAudioKind.LAudioKindDenoise));
        }

        if (pInspectorVolumePersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LAudioKind.LAudioKindVolume));
        }

        if (pLoudnessPersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LAudioKind.LAudioKindLeveling));
        }

        if (pEqualizerPersistent.IsChecked == true)
        {
            pSteps.Add(PInspectorStepRead(LAudioKind.LAudioKindEqualizer));
        }

        return new LWorkAudio(pSteps)
        {
            LWorkAudioSkip = pSkipPersistentBox.IsChecked == true && pSkipApplyBox.IsChecked == true
        };
    }

    private void PInspectorStepApply(LWorkAudioStep pStep)
    {
        switch (pStep)
        {
            case LWorkNormalizeStep pNormalize:
                pLoudnessApplyBox.IsChecked = pNormalize.LWorkStepActive;
                pLoudnessPresetSuppress = true;
                pLoudnessMode.SelectedIndex = pNormalize.LWorkNormalizeMode == LLeveling.LLevelingDynamic ? 1 : 0;
                pLoudnessTarget.Text = pNormalize.LWorkNormalizeTarget.ToString("0.###", CultureInfo.InvariantCulture);
                pLoudnessPeak.Text = pNormalize.LWorkNormalizePeak.ToString("0.###", CultureInfo.InvariantCulture);
                pLoudnessRange.Text = pNormalize.LWorkNormalizeRange.ToString("0.###", CultureInfo.InvariantCulture);
                pLoudnessTwoPass.IsChecked = pNormalize.LWorkTwoPass;
                pDynamicFrame.Text = pNormalize.LWorkNormalizeFrame.ToString("0.###", CultureInfo.InvariantCulture);
                pDynamicGauss.Text = pNormalize.LWorkNormalizeGauss.ToString("0.###", CultureInfo.InvariantCulture);
                pDynamicMaxGain.Text = pNormalize.LWorkNormalizeGain.ToString("0.###", CultureInfo.InvariantCulture);
                pDynamicCompress.Text = pNormalize.LWorkNormalizeCompress.ToString(
                    "0.###",
                    CultureInfo.InvariantCulture);
                pLoudnessPresetSuppress = false;
                PLoudnessApplyUpdate();
                PLoudnessModeUpdate();
                break;
            case LWorkNoiseStep pNoise:
                pNoiseApplyBox.IsChecked = pNoise.LWorkStepActive;
                PNoiseValueSet(pNoise);
                pNoiseTrack.IsChecked = pNoise.LWorkNoiseTrack;
                PNoiseApplyUpdate();
                break;
            case LWorkPassStep pPass:
                PFilterActiveSet(pPass.LWorkPassHigh ? pInspectorHighPass : pInspectorLowPass, pPass);
                break;
            case LWorkEqualizerStep pEqualizer:
                PEqualizerActiveSet(pEqualizer);
                break;
            case LWorkVolumeStep pVolume:
                pVolumeApplyBox.IsChecked = pVolume.LWorkStepActive;
                pInspectorVolumeValue.Text = pVolume.LWorkVolumeGain.ToString("0.#", CultureInfo.InvariantCulture);
                pInspectorVolumeSlider.Value = Math.Clamp(
                    pVolume.LWorkVolumeGain,
                    LWorkAudio.LWorkGainLeast,
                    LWorkAudio.LWorkGainMost);
                PVolumeWarnUpdate();
                PVolumeApplyUpdate();
                break;
        }
    }
}
