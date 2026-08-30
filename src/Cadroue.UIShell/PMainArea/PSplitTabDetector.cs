using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PSplitTab
{
    private void PSplitPersistentHandle(bool pSplitPersistent)
    {
        PSplitDetectorSave();
        if (pSplitPersistent)
        {
            PSplitSweepRun();
        }
    }

    private LSidecarSplitRecord PSplitSidecarRead()
    {
        var pSplitDetectors = new List<LSidecarDetectorRecord>();
        foreach (LDetectorKind pDetectorKind in LDetector.LDetectorKinds)
        {
            if (pDetectorKind == LDetectorKind.LDetectorKindBlank)
            {
                LDetectorBlank pBlank = pInspector.PBlankRead();
                pSplitDetectors.Add(new LSidecarDetectorRecord
                {
                    LSidecarDetectorKind = (int)pDetectorKind,
                    LSidecarDetectorEnabled = pBlank.LDetectorBlankEnabled,
                    LSidecarDetectorType = (int)pBlank.LDetectorBlankType,
                    LSidecarDetectorHue = pBlank.LDetectorBlankHue,
                    LSidecarDetectorSaturation = pBlank.LDetectorBlankSaturation,
                    LSidecarDetectorBrightness = pBlank.LDetectorBlankBrightness,
                    LSidecarDetectorTolerance = pBlank.LDetectorBlankTolerance,
                    LSidecarDetectorCoverage = pBlank.LDetectorBlankCoverage,
                    LSidecarDetectorMinimum = pBlank.LDetectorBlankMinimum
                });
                continue;
            }

            LDetectorStep pStep = pInspector.PSensorStepRead(pDetectorKind);
            pSplitDetectors.Add(new LSidecarDetectorRecord
            {
                LSidecarDetectorKind = (int)pDetectorKind,
                LSidecarDetectorEnabled = pStep.LDetectorStepEnabled,
                LSidecarDetectorThreshold = pStep.LDetectorStepThreshold,
                LSidecarDetectorMinimum = pStep.LDetectorStepMinimum,
                LSidecarDetectorWindow = pStep.LDetectorStepWindow,
                LSidecarDetectorType = PSplitModeRead(pDetectorKind),
                LSidecarDetectorPreset = pInspector.PSensorPresetRead(pDetectorKind)
            });
        }

        return new LSidecarSplitRecord
        {
            LSidecarSplitDetectors = pSplitDetectors
        };
    }

    private void PSplitDetectorSave()
    {
        if (pSplitDetectorLoading
            || pViewer.PViewerSourcePath is not { } pSplitSourcePath
            || pList.PListLockCheck(pSplitSourcePath))
        {
            return;
        }

        LSidecarSplitRecord pSplitRecord = PSplitSidecarRead();
        if (!pSplitRecord.LSidecarSplitActive && LLibrarian.LLibrarianSplitLoad(pSplitSourcePath) is null)
        {
            return;
        }

        LLibrarian.LLibrarianSplitSave(pSplitSourcePath, pSplitRecord);
    }

    private void PSplitDetectorLoad(string pSplitSourcePath)
    {
        if (LLibrarian.LLibrarianSplitLoad(pSplitSourcePath) is not { } pSplitRecord)
        {
            return;
        }

        pSplitDetectorLoading = true;
        try
        {
            foreach (LSidecarDetectorRecord pDetector in pSplitRecord.LSidecarSplitDetectors)
            {
                if (!Enum.IsDefined(typeof(LDetectorKind), pDetector.LSidecarDetectorKind))
                {
                    continue;
                }

                var pDetectorKind = (LDetectorKind)pDetector.LSidecarDetectorKind;
                if (pDetectorKind == LDetectorKind.LDetectorKindBlank)
                {
                    pInspector.PBlankApply(new LDetectorBlank(
                        pDetector.LSidecarDetectorEnabled,
                        Enum.IsDefined(typeof(LDetectorType), pDetector.LSidecarDetectorType)
                            ? (LDetectorType)pDetector.LSidecarDetectorType
                            : LDetectorType.LDetectorTypeBlack,
                        pDetector.LSidecarDetectorHue,
                        pDetector.LSidecarDetectorSaturation,
                        pDetector.LSidecarDetectorBrightness,
                        pDetector.LSidecarDetectorTolerance,
                        pDetector.LSidecarDetectorCoverage,
                        pDetector.LSidecarDetectorMinimum));
                    continue;
                }

                pInspector.PSensorApply(new LDetectorStep(
                    pDetectorKind,
                    pDetector.LSidecarDetectorEnabled,
                    pDetector.LSidecarDetectorThreshold,
                    pDetector.LSidecarDetectorMinimum,
                    pDetector.LSidecarDetectorWindow));

                if (pDetectorKind == LDetectorKind.LDetectorKindStill)
                {
                    pInspector.PSensorModeApply(pDetectorKind,
                        Enum.IsDefined(typeof(LDetectorStillMode), pDetector.LSidecarDetectorType)
                            ? (LDetectorStillMode)pDetector.LSidecarDetectorType
                            : LDetectorStillMode.LDetectorStillDiscard);
                }
                else if (pDetectorKind == LDetectorKind.LDetectorKindLuminance)
                {
                    pInspector.PSensorSpeedApply(pDetectorKind,
                        Enum.IsDefined(typeof(LDetectorLuminanceMode), pDetector.LSidecarDetectorType)
                            ? (LDetectorLuminanceMode)pDetector.LSidecarDetectorType
                            : LDetectorLuminanceMode.LDetectorLuminanceNormal);
                }
                else if (pDetectorKind == LDetectorKind.LDetectorKindVolume)
                {
                    pInspector.PSensorMetricApply(pDetectorKind,
                        Enum.IsDefined(typeof(LDetectorMetricMode), pDetector.LSidecarDetectorType)
                            ? (LDetectorMetricMode)pDetector.LSidecarDetectorType
                            : LDetectorMetricMode.LDetectorMetricLufs);
                }

                if (pDetectorKind is LDetectorKind.LDetectorKindVolume
                    or LDetectorKind.LDetectorKindScene
                    or LDetectorKind.LDetectorKindStill
                    or LDetectorKind.LDetectorKindLuminance)
                {
                    pInspector.PSensorPresetApply(pDetectorKind, pDetector.LSidecarDetectorPreset);
                }
            }

            PSplitActiveUpdate();
        }
        finally
        {
            pSplitDetectorLoading = false;
        }
    }

    private List<LSceneDetector> PSplitDetectorRead()
    {
        var pDetectors = new List<LSceneDetector>();
        foreach (LDetectorKind pDetectorKind in LDetector.LDetectorKinds)
        {
            if (pDetectorKind == LDetectorKind.LDetectorKindBlank)
            {
                LDetectorBlank pBlank = pInspector.PBlankRead();
                pDetectors.Add(new LSceneDetector
                {
                    LSceneDetectorKind = (int)pDetectorKind,
                    LSceneDetectorEnabled = pBlank.LDetectorBlankEnabled,
                    LSceneDetectorType = (int)pBlank.LDetectorBlankType,
                    LSceneDetectorHue = pBlank.LDetectorBlankHue,
                    LSceneDetectorSaturation = pBlank.LDetectorBlankSaturation,
                    LSceneDetectorBrightness = pBlank.LDetectorBlankBrightness,
                    LSceneDetectorTolerance = pBlank.LDetectorBlankTolerance,
                    LSceneDetectorCoverage = pBlank.LDetectorBlankCoverage,
                    LSceneDetectorMinimum = pBlank.LDetectorBlankMinimum
                });
                continue;
            }

            LDetectorStep pStep = pInspector.PSensorStepRead(pDetectorKind);
            pDetectors.Add(new LSceneDetector
            {
                LSceneDetectorKind = (int)pDetectorKind,
                LSceneDetectorEnabled = pStep.LDetectorStepEnabled,
                LSceneDetectorThreshold = pStep.LDetectorStepThreshold,
                LSceneDetectorMinimum = pStep.LDetectorStepMinimum,
                LSceneDetectorWindow = pStep.LDetectorStepWindow,
                LSceneDetectorType = PSplitModeRead(pDetectorKind),
                LSceneDetectorPreset = pInspector.PSensorPresetRead(pDetectorKind)
            });
        }

        return pDetectors;
    }

    private int PSplitModeRead(LDetectorKind pDetectorKind) => pDetectorKind switch
    {
        LDetectorKind.LDetectorKindStill => (int)pInspector.PSensorModeRead(pDetectorKind),
        LDetectorKind.LDetectorKindLuminance => (int)pInspector.PSensorSpeedRead(pDetectorKind),
        LDetectorKind.LDetectorKindVolume => (int)pInspector.PSensorMetricRead(pDetectorKind),
        _ => 0
    };

    private void PSplitDetectorRestore(LSceneTabRecord? lPreferenceTabLayout)
    {
        if (lPreferenceTabLayout is null)
        {
            return;
        }

        foreach (LSceneDetector pDetector in lPreferenceTabLayout.LSceneDetectors)
        {
            if (!Enum.IsDefined(typeof(LDetectorKind), pDetector.LSceneDetectorKind))
            {
                continue;
            }

            var pDetectorKind = (LDetectorKind)pDetector.LSceneDetectorKind;
            if (pDetectorKind == LDetectorKind.LDetectorKindBlank)
            {
                pInspector.PBlankApply(new LDetectorBlank(
                    pDetector.LSceneDetectorEnabled,
                    Enum.IsDefined(typeof(LDetectorType), pDetector.LSceneDetectorType)
                        ? (LDetectorType)pDetector.LSceneDetectorType
                        : LDetectorType.LDetectorTypeBlack,
                    pDetector.LSceneDetectorHue,
                    pDetector.LSceneDetectorSaturation,
                    pDetector.LSceneDetectorBrightness,
                    pDetector.LSceneDetectorTolerance,
                    pDetector.LSceneDetectorCoverage,
                    pDetector.LSceneDetectorMinimum));
                continue;
            }

            pInspector.PSensorApply(new LDetectorStep(
                pDetectorKind,
                pDetector.LSceneDetectorEnabled,
                pDetector.LSceneDetectorThreshold,
                pDetector.LSceneDetectorMinimum,
                pDetector.LSceneDetectorWindow));

            if (pDetectorKind == LDetectorKind.LDetectorKindStill)
            {
                pInspector.PSensorModeApply(pDetectorKind,
                    Enum.IsDefined(typeof(LDetectorStillMode), pDetector.LSceneDetectorType)
                        ? (LDetectorStillMode)pDetector.LSceneDetectorType
                        : LDetectorStillMode.LDetectorStillDiscard);
            }
            else if (pDetectorKind == LDetectorKind.LDetectorKindLuminance)
            {
                pInspector.PSensorSpeedApply(pDetectorKind,
                    Enum.IsDefined(typeof(LDetectorLuminanceMode), pDetector.LSceneDetectorType)
                        ? (LDetectorLuminanceMode)pDetector.LSceneDetectorType
                        : LDetectorLuminanceMode.LDetectorLuminanceNormal);
            }
            else if (pDetectorKind == LDetectorKind.LDetectorKindVolume)
            {
                pInspector.PSensorMetricApply(pDetectorKind,
                    Enum.IsDefined(typeof(LDetectorMetricMode), pDetector.LSceneDetectorType)
                        ? (LDetectorMetricMode)pDetector.LSceneDetectorType
                        : LDetectorMetricMode.LDetectorMetricLufs);
            }

            if (pDetectorKind is LDetectorKind.LDetectorKindVolume
                or LDetectorKind.LDetectorKindScene
                or LDetectorKind.LDetectorKindStill
                or LDetectorKind.LDetectorKindLuminance)
            {
                pInspector.PSensorPresetApply(pDetectorKind, pDetector.LSceneDetectorPreset);
            }
        }

        pInspector.PSensorPersistentApply(lPreferenceTabLayout.LSceneDetectPersistent);
        PSplitActiveUpdate();
    }
}
