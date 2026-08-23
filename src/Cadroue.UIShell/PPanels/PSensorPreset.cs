using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private readonly Dictionary<LDetectorKind, ComboBox> pSensorPresetBoxes = new();
    private readonly Dictionary<LDetectorKind, string?> pSensorPresetTokens = new();
    private bool pSensorPresetSuppress;

    private void PSensorPresetBuild(StackPanel pStack, LDetectorKind pDetectorKind, int pIndex)
    {
        var pBox = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pBox);
        pBox.Items.Add(new LLocalizationChoice("Conservative", "Inspector.Detector.Conservative"));
        pBox.Items.Add(new LLocalizationChoice("Normal", "Inspector.Detector.Normal"));
        pBox.Items.Add(new LLocalizationChoice("Sensitive", "Inspector.Detector.Sensitive"));
        pBox.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        pBox.SelectedIndex = 1;
        pBox.SelectionChanged += (_, _) => PSensorPresetHandle(pDetectorKind);

        pSensorPresetBoxes[pDetectorKind] = pBox;
        pSensorPresetTokens[pDetectorKind] = "Normal";

        pStack.Children.Insert(pIndex, PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pBox));
    }

    private PSensorSection? PSensorSectionRead(LDetectorKind pDetectorKind) =>
        pSensorSections.TryGetValue(pDetectorKind, out PSensorSection? pSection) ? pSection : null;

    public string PSensorPresetRead(LDetectorKind pDetectorKind)
    {
        if (!pSensorPresetBoxes.TryGetValue(pDetectorKind, out ComboBox? pBox))
        {
            return string.Empty;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pBox.SelectedItem);
        return pName is "Conservative" or "Normal" or "Sensitive" ? pName : string.Empty;
    }

    public void PSensorPresetApply(LDetectorKind pDetectorKind, string pToken)
    {
        if (!pSensorPresetBoxes.TryGetValue(pDetectorKind, out ComboBox? pBox))
        {
            return;
        }

        pSensorPresetSuppress = true;
        if (pToken is "Conservative" or "Normal" or "Sensitive")
        {
            pSensorPresetTokens[pDetectorKind] = pToken;
            PSensorCustomReset(pDetectorKind);
            PSensorPresetSelect(pDetectorKind, pToken);
        }
        else
        {
            pSensorPresetTokens[pDetectorKind] = null;
            PSensorCustomReset(pDetectorKind);
            pBox.SelectedIndex = pBox.Items.Count - 1;
        }

        pSensorPresetSuppress = false;
    }

    private void PSensorPresetHandle(LDetectorKind pDetectorKind)
    {
        if (pSensorPresetSuppress || !pSensorPresetBoxes.TryGetValue(pDetectorKind, out ComboBox? pBox))
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pBox.SelectedItem);
        if (pName is not ("Conservative" or "Normal" or "Sensitive"))
        {
            pSensorPresetTokens[pDetectorKind] = null;
            return;
        }

        pSensorPresetTokens[pDetectorKind] = pName;
        PSensorPresetSet(pDetectorKind, pName);
        PSensorCustomReset(pDetectorKind);
        PSensorRaise();
    }

    private void PSensorPresetSet(LDetectorKind pDetectorKind, string pToken)
    {
        if (PSensorSectionRead(pDetectorKind) is not { } pSection)
        {
            return;
        }

        pSection.PSensorSuppress = true;
        if (pDetectorKind == LDetectorKind.LDetectorKindScene)
        {
            if (LDetector.LDetectorSceneResolve(pToken) is { } pSensitivity
                && pSection.PSensorThreshold is { } pSceneThreshold)
            {
                pSceneThreshold.Text = pSensitivity.ToString(
                    PSensorShapeRead(pDetectorKind).Format, CultureInfo.InvariantCulture);
            }

            pSection.PSensorSuppress = false;
            return;
        }

        if (pDetectorKind == LDetectorKind.LDetectorKindStill)
        {
            if (LDetector.LDetectorStillResolve(pToken) is { } pStill)
            {
                if (pSection.PSensorThreshold is { } pStillThreshold)
                {
                    pStillThreshold.Text = pStill.Tolerance.ToString(
                        PSensorShapeRead(pDetectorKind).Format, CultureInfo.InvariantCulture);
                }

                if (pSection.PSensorMinimum is { } pStillMinimum)
                {
                    pStillMinimum.Text = pStill.Minimum.ToString("0.0", CultureInfo.InvariantCulture);
                }
            }

            pSection.PSensorSuppress = false;
            return;
        }

        if (LDetector.LDetectorPresetRead(pToken) is not { } pPreset)
        {
            pSection.PSensorSuppress = false;
            return;
        }

        double pThreshold = LDetector.LDetectorPresetResolve(pPreset, PSensorMetricRead(pDetectorKind));
        if (pSection.PSensorThreshold is { } pThresholdBox)
        {
            pThresholdBox.Text = pThreshold.ToString(
                PSensorShapeRead(pDetectorKind).Format, CultureInfo.InvariantCulture);
        }

        if (pSection.PSensorWindow is { } pWindowBox)
        {
            pWindowBox.Text = pPreset.LDetectorPresetWindow.ToString("0.0", CultureInfo.InvariantCulture);
        }

        if (pSection.PSensorMinimum is { } pMinimumBox)
        {
            pMinimumBox.Text = pPreset.LDetectorPresetMinimum.ToString("0.0", CultureInfo.InvariantCulture);
        }

        pSection.PSensorSuppress = false;
    }

    private bool PSensorPresetMatch(LDetectorKind pDetectorKind, string pBase)
    {
        if (PSensorSectionRead(pDetectorKind) is not { } pSection)
        {
            return false;
        }

        LDetectorStep pDefault = LDetector.LDetectorCreate(pDetectorKind);
        if (pDetectorKind == LDetectorKind.LDetectorKindScene)
        {
            double pSensitivity = pSection.PSensorThreshold is { } pSceneThreshold
                ? PInspectorDecimalRead(pSceneThreshold, pDefault.LDetectorStepThreshold)
                : pDefault.LDetectorStepThreshold;
            return LDetector.LDetectorSceneResolve(pBase) is { } pTarget
                && Math.Abs(pSensitivity - pTarget) < 0.5;
        }

        if (pDetectorKind == LDetectorKind.LDetectorKindStill)
        {
            double pStillTolerance = pSection.PSensorThreshold is { } pStillThreshold
                ? PInspectorDecimalRead(pStillThreshold, pDefault.LDetectorStepThreshold)
                : pDefault.LDetectorStepThreshold;
            double pStillMinimum = pSection.PSensorMinimum is { } pStillMinimumBox
                ? PInspectorDecimalRead(pStillMinimumBox, pDefault.LDetectorStepMinimum)
                : pDefault.LDetectorStepMinimum;
            return LDetector.LDetectorStillMatch(pStillTolerance, pStillMinimum) == pBase;
        }

        string? pMatch = LDetector.LDetectorPresetMatch(
            PSensorMetricRead(pDetectorKind),
            pSection.PSensorThreshold is { } pThreshold
                ? PInspectorDecimalRead(pThreshold, pDefault.LDetectorStepThreshold)
                : pDefault.LDetectorStepThreshold,
            pSection.PSensorWindow is { } pWindow
                ? PInspectorDecimalRead(pWindow, pDefault.LDetectorStepWindow)
                : pDefault.LDetectorStepWindow,
            pSection.PSensorMinimum is { } pMinimum
                ? PInspectorDecimalRead(pMinimum, pDefault.LDetectorStepMinimum)
                : pDefault.LDetectorStepMinimum);
        return pMatch == pBase;
    }

    private void PSensorPresetCheck(LDetectorKind pDetectorKind)
    {
        if (pSensorPresetSuppress
            || pSensorPresetTokens.GetValueOrDefault(pDetectorKind) is not { } pBase)
        {
            return;
        }

        pSensorPresetSuppress = true;
        if (PSensorPresetMatch(pDetectorKind, pBase))
        {
            PSensorCustomReset(pDetectorKind);
            PSensorPresetSelect(pDetectorKind, pBase);
        }
        else
        {
            PSensorCustomSet(pDetectorKind, pBase);
        }

        pSensorPresetSuppress = false;
    }

    private void PSensorPresetSync(LDetectorKind pDetectorKind)
    {
        if (pSensorPresetTokens.GetValueOrDefault(pDetectorKind) is not { } pBase
            || LDetector.LDetectorPresetRead(pBase) is not { } pPreset
            || PSensorSectionRead(pDetectorKind) is not { PSensorThreshold: { } pThreshold } pSection)
        {
            return;
        }

        pSensorPresetSuppress = true;
        pSection.PSensorSuppress = true;
        pThreshold.Text = LDetector
            .LDetectorPresetResolve(pPreset, PSensorMetricRead(pDetectorKind))
            .ToString(PSensorShapeRead(pDetectorKind).Format, CultureInfo.InvariantCulture);
        pSection.PSensorSuppress = false;
        pSensorPresetSuppress = false;
    }

    private void PSensorCustomSet(LDetectorKind pDetectorKind, string pBase)
    {
        if (!pSensorPresetBoxes.TryGetValue(pDetectorKind, out ComboBox? pBox))
        {
            return;
        }

        int pLast = pBox.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PSensorKeyRead(pBase)));
        pBox.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pBox.SelectedIndex = pLast;
    }

    private void PSensorCustomReset(LDetectorKind pDetectorKind)
    {
        if (!pSensorPresetBoxes.TryGetValue(pDetectorKind, out ComboBox? pBox))
        {
            return;
        }

        int pLast = pBox.Items.Count - 1;
        pBox.Items[pLast] = new LLocalizationChoice("Custom", "Inspector.Common.Custom");
    }

    private void PSensorPresetSelect(LDetectorKind pDetectorKind, string pToken)
    {
        if (!pSensorPresetBoxes.TryGetValue(pDetectorKind, out ComboBox? pBox))
        {
            return;
        }

        for (int pIndex = 0; pIndex < pBox.Items.Count; pIndex++)
        {
            if (LLocalizationChoice.LLocalizationChoiceRead(pBox.Items[pIndex]) == pToken)
            {
                pBox.SelectedIndex = pIndex;
                return;
            }
        }
    }

    private static string PSensorKeyRead(string pToken) => pToken switch
    {
        "Conservative" => "Inspector.Detector.Conservative",
        "Normal" => "Inspector.Detector.Normal",
        "Sensitive" => "Inspector.Detector.Sensitive",
        _ => "Inspector.Common.Custom"
    };
}
