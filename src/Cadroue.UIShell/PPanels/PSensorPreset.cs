using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private ComboBox pSensorPreset = null!;
    private string? pSensorPresetToken;
    private bool pSensorPresetSuppress;

    private const LDetectorKind PSensorPresetKind = LDetectorKind.LDetectorKindVolume;

    private void PSensorPresetBuild(StackPanel pStack)
    {
        pSensorPreset = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pSensorPreset);
        pSensorPreset.Items.Add(new LLocalizationChoice("Conservative", "Inspector.Detector.Conservative"));
        pSensorPreset.Items.Add(new LLocalizationChoice("Normal", "Inspector.Detector.Normal"));
        pSensorPreset.Items.Add(new LLocalizationChoice("Sensitive", "Inspector.Detector.Sensitive"));
        pSensorPreset.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        pSensorPreset.SelectedIndex = 1;
        pSensorPreset.SelectionChanged += (_, _) => PSensorPresetHandle();

        pStack.Children.Insert(1, PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pSensorPreset));
    }

    private PSensorSection? PSensorSectionRead() =>
        pSensorSections.TryGetValue(PSensorPresetKind, out PSensorSection? pSection) ? pSection : null;

    private void PSensorPresetHandle()
    {
        if (pSensorPresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pSensorPreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom"
            || LDetector.LDetectorPresetRead(pName) is not { } pPreset)
        {
            pSensorPresetToken = null;
            return;
        }

        pSensorPresetToken = pName;
        PSensorPresetApply(pPreset);
        PSensorCustomReset();
        PSensorRaise();
    }

    private void PSensorPresetApply(LDetectorPreset pPreset)
    {
        if (PSensorSectionRead() is not { } pSection)
        {
            return;
        }

        pSection.PSensorSuppress = true;
        double pThreshold = LDetector.LDetectorPresetResolve(pPreset, PSensorMetricRead(PSensorPresetKind));
        if (pSection.PSensorThreshold is { } pThresholdBox)
        {
            pThresholdBox.Text = pThreshold.ToString(
                PSensorShapeRead(PSensorPresetKind).Format, CultureInfo.InvariantCulture);
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

    private string? PSensorPresetMatch()
    {
        if (PSensorSectionRead() is not { } pSection)
        {
            return null;
        }

        LDetectorStep pDefault = LDetector.LDetectorCreate(PSensorPresetKind);
        return LDetector.LDetectorPresetMatch(
            PSensorMetricRead(PSensorPresetKind),
            pSection.PSensorThreshold is { } pThreshold
                ? PInspectorDecimalRead(pThreshold, pDefault.LDetectorStepThreshold)
                : pDefault.LDetectorStepThreshold,
            pSection.PSensorWindow is { } pWindow
                ? PInspectorDecimalRead(pWindow, pDefault.LDetectorStepWindow)
                : pDefault.LDetectorStepWindow,
            pSection.PSensorMinimum is { } pMinimum
                ? PInspectorDecimalRead(pMinimum, pDefault.LDetectorStepMinimum)
                : pDefault.LDetectorStepMinimum);
    }

    private void PSensorPresetCheck()
    {
        if (pSensorPresetSuppress || pSensorPresetToken is not { } pBase
            || LDetector.LDetectorPresetRead(pBase) is null)
        {
            return;
        }

        pSensorPresetSuppress = true;
        if (PSensorPresetMatch() == pBase)
        {
            PSensorCustomReset();
            PSensorPresetSelect(pBase);
        }
        else
        {
            PSensorCustomSet(pBase);
        }

        pSensorPresetSuppress = false;
    }

    private void PSensorPresetSync()
    {
        if (pSensorPresetToken is not { } pBase
            || LDetector.LDetectorPresetRead(pBase) is not { } pPreset
            || PSensorSectionRead() is not { PSensorThreshold: { } pThreshold } pSection)
        {
            return;
        }

        pSensorPresetSuppress = true;
        pSection.PSensorSuppress = true;
        pThreshold.Text = LDetector
            .LDetectorPresetResolve(pPreset, PSensorMetricRead(PSensorPresetKind))
            .ToString(PSensorShapeRead(PSensorPresetKind).Format, CultureInfo.InvariantCulture);
        pSection.PSensorSuppress = false;
        pSensorPresetSuppress = false;
    }

    private void PSensorPresetUpdate()
    {
        pSensorPresetSuppress = true;
        string? pMatch = PSensorPresetMatch();
        if (pMatch is not null)
        {
            pSensorPresetToken = pMatch;
            PSensorCustomReset();
            PSensorPresetSelect(pMatch);
        }
        else
        {
            pSensorPresetToken = null;
            PSensorCustomReset();
            pSensorPreset.SelectedIndex = pSensorPreset.Items.Count - 1;
        }

        pSensorPresetSuppress = false;
    }

    private void PSensorCustomSet(string pBase)
    {
        int pLast = pSensorPreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PSensorKeyRead(pBase)));
        pSensorPreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pSensorPreset.SelectedIndex = pLast;
    }

    private void PSensorCustomReset()
    {
        int pLast = pSensorPreset.Items.Count - 1;
        pSensorPreset.Items[pLast] = new LLocalizationChoice("Custom", "Inspector.Common.Custom");
    }

    private void PSensorPresetSelect(string pToken)
    {
        for (int pIndex = 0; pIndex < pSensorPreset.Items.Count; pIndex++)
        {
            if (LLocalizationChoice.LLocalizationChoiceRead(pSensorPreset.Items[pIndex]) == pToken)
            {
                pSensorPreset.SelectedIndex = pIndex;
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
