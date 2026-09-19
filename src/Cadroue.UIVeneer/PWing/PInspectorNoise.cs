using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private CheckBox pNoiseApplyBox = null!;
    private CheckBox pNoisePersistent = null!;
    private ComboBox pNoisePreset = null!;
    private ComboBox pNoiseType = null!;
    private CheckBox pNoiseTrack = null!;
    private StackPanel pNoiseStack = null!;
    private StackPanel pNoiseBody = null!;
    private readonly Slider[] pNoiseSliders = new Slider[5];
    private readonly TextBox[] pNoiseValues = new TextBox[5];

    private static readonly string[] PNoiseLabelKeys =
    {
        "Inspector.Common.Amount",
        "Inspector.Noise.Floor",
        "Inspector.Noise.Smoothing",
        "Inspector.Noise.Adaptivity",
        "Inspector.Noise.Residual"
    };

    private static readonly string[] PNoiseUnits = { "dB", "dB", "gs", "0-1", "dB" };

    private static readonly string[] PNoiseFormats = { "0.#", "0.#", "0.#", "0.###", "0.#" };

    private static readonly double[] PNoiseLeast =
    {
        LGrainCatalog.LGrainReductionLeast,
        LGrainCatalog.LGrainFloorLeast,
        LGrainCatalog.LGrainSmoothLeast,
        LGrainCatalog.LGrainAdaptivityLeast,
        LGrainCatalog.LGrainFloorLeast
    };

    private static readonly double[] PNoiseMost =
    {
        LGrainCatalog.LGrainReductionMost,
        LGrainCatalog.LGrainFloorMost,
        LGrainCatalog.LGrainSmoothMost,
        LGrainCatalog.LGrainAdaptivityMost,
        LGrainCatalog.LGrainFloorMost
    };

    private static readonly int[] PNoiseOrder = { 0, 2, 1, 4, 3 };

    private StackPanel PNoiseBodyBuild()
    {
        pNoiseApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Noise.ApplyTooltip"));
        PInspectorSwitchAttach(pNoiseApplyBox, LNoise.LNoiseActiveSet);

        pNoisePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Noise.PersistentTooltip"));
        PInspectorSwitchAttach(pNoisePersistent, LNoise.LNoisePersistentSet);

        pNoisePreset = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pNoisePreset);
        pNoisePreset.Items.Add(new LLocalizationChoice("Light", "Inspector.Noise.Light"));
        pNoisePreset.Items.Add(new LLocalizationChoice("Medium", "Inspector.Noise.Medium"));
        pNoisePreset.Items.Add(new LLocalizationChoice("Strong", "Inspector.Noise.Strong"));
        pNoisePreset.Items.Add(new LLocalizationChoice("Dialogue", "Inspector.Noise.Dialogue"));
        pNoisePreset.Items.Add(new LLocalizationChoice("Vinyl", "Inspector.Noise.Vinyl"));
        pNoisePreset.Items.Add(new LLocalizationChoice("Shellac", "Inspector.Noise.Shellac"));
        pNoisePreset.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        pNoisePreset.SelectedIndex = 1;
        pNoisePreset.SelectionChanged += (_, _) =>
        {
            if (PInspectorPresetRead(pNoisePreset, LNoise.LNoiseToken, LNoise.LNoiseMatchRead()) is { } pToken)
            {
                LNoise.LNoisePresetSelect(pToken);
            }
        };

        for (int pIndex = 0; pIndex < pNoiseSliders.Length; pIndex++)
        {
            int pSlot = pIndex;
            pNoiseSliders[pSlot] = new Slider
            {
                Minimum = PNoiseLeast[pSlot],
                Maximum = PNoiseMost[pSlot],
                Value = PNoiseValueRead(pSlot),
                VerticalAlignment = VerticalAlignment.Center
            };
            PSlider.PSliderApply(pNoiseSliders[pSlot]);
            PSlider.PSliderResetApply(pNoiseSliders[pSlot], () => PNoiseDefaultRead(pSlot));
            pNoiseValues[pSlot] = PInspectorDecimalBuild();
            PInspectorTextSet(pNoiseValues[pSlot], PNoiseValueRead(pSlot), PNoiseFormats[pSlot]);
            PInspectorValueAttach(
                pNoiseSliders[pSlot],
                pNoiseValues[pSlot],
                PNoiseLeast[pSlot],
                PNoiseMost[pSlot],
                () => PNoiseValueRead(pSlot),
                pNumber => LNoise.LNoiseValueSet(pSlot, pNumber));
        }

        pNoiseType = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pNoiseType);
        pNoiseType.Items.Add(new LLocalizationChoice("White", "Inspector.Noise.White"));
        pNoiseType.Items.Add(new LLocalizationChoice("Vinyl", "Inspector.Noise.Vinyl"));
        pNoiseType.Items.Add(new LLocalizationChoice("Shellac", "Inspector.Noise.Shellac"));
        pNoiseType.SelectedIndex = 0;
        pNoiseType.SelectionChanged += (_, _) =>
        {
            if (pNoiseType.SelectedIndex >= 0)
            {
                LNoise.LNoiseTypeSet(LGrainCatalog.LGrainParse(
                    LLocalizationChoice.LLocalizationChoiceRead(pNoiseType.SelectedItem)));
            }
        };

        pNoiseTrack = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Noise.Track"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Noise.TrackTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };
        PHouse.PCheckbox.PCheckboxApply(pNoiseTrack);
        PInspectorSwitchAttach(pNoiseTrack, LNoise.LNoiseTrackSet);

        pNoiseStack = new StackPanel();
        pNoiseStack.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pNoisePreset));
        foreach (int pSlot in PNoiseOrder)
        {
            pNoiseStack.Children.Add(
                PFilterSliderBuild(
                    LLocalization.LLocalizationTextRead(PNoiseLabelKeys[pSlot]),
                    pNoiseSliders[pSlot],
                    PNoiseUnits[pSlot],
                    pNoiseValues[pSlot]));
        }

        pNoiseStack.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Noise.Noise"), pNoiseType));
        pNoiseStack.Children.Add(pNoiseTrack);

        pNoiseBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pNoiseBody.Children.Add(pNoiseApplyBox);
        pNoiseBody.Children.Add(PInspectorSeparatorBuild());
        pNoiseBody.Children.Add(pNoiseStack);
        return pNoiseBody;
    }

    private double PNoiseValueRead(int pSlot)
    {
        LWorkNoiseStep pStep = LNoise.LNoiseStep;
        return pSlot switch
        {
            1 => pStep.LWorkNoiseFloor,
            2 => pStep.LWorkNoiseSmooth,
            3 => pStep.LWorkNoiseAdaptivity,
            4 => pStep.LWorkNoiseResidual,
            _ => pStep.LWorkNoiseReduction
        };
    }

    private double PNoiseDefaultRead(int pSlot)
    {
        LGrainPreset? pPreset = LNoise.LNoisePresetRead();
        return pSlot switch
        {
            1 => pPreset?.LGrainFloor ?? -50,
            2 => pPreset?.LGrainSmooth ?? 6,
            3 => pPreset?.LGrainAdaptivity ?? 0.5,
            4 => pPreset?.LGrainResidual ?? -38,
            _ => pPreset?.LGrainReduction ?? 12
        };
    }

    private void PNoiseUpdate()
    {
        LWorkNoiseStep pStep = LNoise.LNoiseStep;
        PInspectorSwitchUpdate(pNoiseApplyBox, pStep.LWorkStepActive, false);
        PInspectorSwitchUpdate(pNoisePersistent, LNoise.LNoisePersistent, true);
        PInspectorSwitchUpdate(pNoiseTrack, pStep.LWorkNoiseTrack, false);
        for (int pSlot = 0; pSlot < pNoiseSliders.Length; pSlot++)
        {
            PInspectorValueUpdate(
                pNoiseSliders[pSlot], pNoiseValues[pSlot], PNoiseValueRead(pSlot), PNoiseFormats[pSlot]);
        }

        PInspectorChoiceUpdate(pNoiseType, LGrainCatalog.LGrainFormat(pStep.LWorkNoiseType));
        PInspectorPresetUpdate(pNoisePreset, LNoise.LNoiseMatchRead(), LNoise.LNoiseToken, PNoiseKeyRead);
        PInspectorSectionUpdate(pNoiseStack, pStep.LWorkStepActive);
    }
}
