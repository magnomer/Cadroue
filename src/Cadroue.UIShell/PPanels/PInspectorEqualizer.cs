using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private const string pEqualizerAddIcon = "/PAssets/PPanels/PFunnelAdd.svg";
    private const string pEqualizerRemoveIcon = "/PAssets/PPanels/PFunnelRemove.svg";
    private const double PEqualizerLeastDb = -12;
    private const double PEqualizerMostDb = 12;
    private const double PEqualizerLeastHz = 20;
    private const double PEqualizerMostHz = 20000;
    private const double PEqualizerDefaultHz = 1000;

    private sealed class PInspectorBand
    {
        public required Grid PInspectorBandRow { get; init; }
        public required TextBox PInspectorBandFrequency { get; init; }
        public required Slider PInspectorBandSlider { get; init; }
        public required TextBox PInspectorBandValue { get; init; }
        public bool PInspectorBandSuppress { get; set; }
    }

    private CheckBox pEqualizerApplyBox = null!;
    private CheckBox pInspectorEqualizerPersistent = null!;
    private ComboBox pInspectorEqualizerPreset = null!;
    private StackPanel pInspectorEqualizerStack = null!;
    private StackPanel pInspectorEqualizerRowPanel = null!;
    private StackPanel pInspectorEqualizerBody = null!;
    private bool pInspectorEqualizerPresetSuppress;
    private string? pEqualizerBaseToken;
    private readonly List<PInspectorBand> pEqualizerRows = new();

    private static readonly double[] pEqualizerBandGrid =
        { 31, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };

    public LWorkAudioStep PEqualizerStepRead()
    {
        var pBands = new List<LWorkBand>();
        foreach (PInspectorBand pRow in pEqualizerRows)
        {
            double pFrequency = Math.Clamp(
                PInspectorDecimalRead(pRow.PInspectorBandFrequency, PEqualizerDefaultHz),
                PEqualizerLeastHz, PEqualizerMostHz);
            double pGain = Math.Clamp(
                PInspectorDecimalRead(pRow.PInspectorBandValue, 0), PEqualizerLeastDb, PEqualizerMostDb);
            pBands.Add(new LWorkBand(pFrequency, pGain));
        }

        return LWorkAudioStep.LWorkEqualizerCreate(pEqualizerApplyBox.IsChecked == true, pBands);
    }

    private void PEqualizerActiveSet(LWorkEqualizerStep pStep)
    {
        pEqualizerApplyBox.IsChecked = pStep.LWorkStepActive;
        pEqualizerRows.Clear();
        pInspectorEqualizerRowPanel.Children.Clear();
        foreach (LWorkBand pBand in pStep.LWorkEqualizerBands)
        {
            PEqualizerRowAdd(pBand.LWorkBandFrequency, pBand.LWorkBandGain, false);
        }

        PEqualizerPresetUpdate();
        PEqualizerApplyUpdate();
    }

    private StackPanel PEqualizerBodyBuild()
    {
        pEqualizerApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Equalizer.ApplyTooltip"));
        pEqualizerApplyBox.Checked += (_, _) => PEqualizerApplyUpdate();
        pEqualizerApplyBox.Unchecked += (_, _) => PEqualizerApplyUpdate();

        pInspectorEqualizerPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Equalizer.PersistentTooltip"));

        pInspectorEqualizerPreset = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pInspectorEqualizerPreset);
        pInspectorEqualizerPreset.Items.Add(new LLocalizationChoice("Flat", "Inspector.Equalizer.Preset.Flat"));
        pInspectorEqualizerPreset.Items.Add(new LLocalizationChoice("Bass boost", "Inspector.Equalizer.Preset.BassBoost"));
        pInspectorEqualizerPreset.Items.Add(new LLocalizationChoice("Bright", "Inspector.Equalizer.Preset.Bright"));
        pInspectorEqualizerPreset.Items.Add(new LLocalizationChoice("Warm", "Inspector.Equalizer.Preset.Warm"));
        pInspectorEqualizerPreset.Items.Add(new LLocalizationChoice("Loudness", "Inspector.Equalizer.Preset.Loudness"));
        pInspectorEqualizerPreset.Items.Add(new LLocalizationChoice("Vocal", "Inspector.Equalizer.Preset.Vocal"));
        pInspectorEqualizerPreset.Items.Add(new LLocalizationChoice("De-ess", "Inspector.Equalizer.Preset.Deess"));
        pInspectorEqualizerPreset.Items.Add(new LLocalizationChoice("Podcast", "Inspector.Equalizer.Preset.Podcast"));
        pInspectorEqualizerPreset.Items.Add(new LLocalizationChoice("Telephone", "Inspector.Equalizer.Preset.Telephone"));
        pInspectorEqualizerPreset.Items.Add(new LLocalizationChoice("Custom", "Inspector.Common.Custom"));
        pInspectorEqualizerPreset.SelectedIndex = 0;
        pInspectorEqualizerPreset.SelectionChanged += (_, _) => PEqualizerPresetApply();

        pInspectorEqualizerRowPanel = new StackPanel();

        Button pAddButton = new()
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pEqualizerAddIcon, pInspectorIconBrush),
                Stretch = Stretch.Uniform
            },
            Width = 28,
            Height = 26,
            HorizontalAlignment = HorizontalAlignment.Left,
            Style = PButton.PButtonPanelCreate(),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Equalizer.Add")
        };
        pAddButton.Click += (_, _) =>
        {
            PEqualizerRowAdd(PEqualizerDefaultHz, 0, true);
            PEqualizerDeviationCheck();
        };

        pInspectorEqualizerStack = new StackPanel();
        pInspectorEqualizerStack.Children.Add(PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pInspectorEqualizerPreset));
        pInspectorEqualizerStack.Children.Add(pInspectorEqualizerRowPanel);
        pInspectorEqualizerStack.Children.Add(pAddButton);

        pInspectorEqualizerBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pInspectorEqualizerBody.Children.Add(pEqualizerApplyBox);
        pInspectorEqualizerBody.Children.Add(PInspectorSeparatorBuild());
        pInspectorEqualizerBody.Children.Add(pInspectorEqualizerStack);

        foreach (LWorkBand pBand in LWorkEqualizerStep.LWorkBandsCreate())
        {
            PEqualizerRowAdd(pBand.LWorkBandFrequency, pBand.LWorkBandGain, false);
        }

        PEqualizerPresetUpdate();
        PEqualizerApplyUpdate();
        return pInspectorEqualizerBody;
    }

    private void PEqualizerRowAdd(double pFrequency, double pGain, bool pRaise)
    {
        var pFrequencyBox = PInspectorDecimalBuild();
        pFrequencyBox.Width = 56;
        pFrequencyBox.Text = pFrequency.ToString("0.###", CultureInfo.InvariantCulture);

        var pSlider = new Slider
        {
            Minimum = PEqualizerLeastDb,
            Maximum = PEqualizerMostDb,
            Value = Math.Clamp(pGain, PEqualizerLeastDb, PEqualizerMostDb),
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        PSlider.PSliderResetApply(pSlider, static () => 0);

        var pValueBox = PInspectorDecimalBuild();
        pValueBox.Width = PInspectorInsetWidth / 2;
        pValueBox.Text = pGain.ToString("0.#", CultureInfo.InvariantCulture);

        var pRemoveButton = new Button
        {
            Content = new Image
            {
                Width = 12,
                Height = 12,
                Source = PIcon.PIconRead(pEqualizerRemoveIcon, pInspectorIconBrush),
                Stretch = Stretch.Uniform
            },
            Width = 26,
            Height = PInspectorFieldHeight,
            Margin = new Thickness(6, 0, 0, 0),
            Style = PButton.PButtonPanelCreate(),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Equalizer.Remove")
        };

        var pRow = new Grid
        {
            Height = PInspectorRowHeight,
            Margin = new Thickness(0, 0, 0, 8)
        };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var pHzUnit = PEqualizerUnitBuild("Hz");
        var pDbUnit = PEqualizerUnitBuild("dB");
        pSlider.VerticalAlignment = VerticalAlignment.Center;
        pValueBox.VerticalAlignment = VerticalAlignment.Center;

        Grid.SetColumn(pFrequencyBox, 0);
        Grid.SetColumn(pHzUnit, 1);
        Grid.SetColumn(pSlider, 2);
        Grid.SetColumn(pValueBox, 3);
        Grid.SetColumn(pDbUnit, 4);
        pRow.Children.Add(pFrequencyBox);
        pRow.Children.Add(pHzUnit);
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pValueBox);
        pRow.Children.Add(pDbUnit);

        var pLine = new Grid();
        pLine.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pLine.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pRow, 0);
        Grid.SetColumn(pRemoveButton, 1);
        pRemoveButton.VerticalAlignment = VerticalAlignment.Center;
        pRemoveButton.Margin = new Thickness(6, 0, 0, 8);
        pLine.Children.Add(pRow);
        pLine.Children.Add(pRemoveButton);

        var pBand = new PInspectorBand
        {
            PInspectorBandRow = pLine,
            PInspectorBandFrequency = pFrequencyBox,
            PInspectorBandSlider = pSlider,
            PInspectorBandValue = pValueBox
        };

        pSlider.ValueChanged += (_, _) =>
        {
            if (pBand.PInspectorBandSuppress)
            {
                return;
            }

            pBand.PInspectorBandSuppress = true;
            pValueBox.Text = pSlider.Value.ToString("0.#", CultureInfo.InvariantCulture);
            pBand.PInspectorBandSuppress = false;
            PInspectorActiveRaise();
            PEqualizerDeviationCheck();
        };
        pValueBox.TextChanged += (_, _) =>
        {
            if (pBand.PInspectorBandSuppress)
            {
                return;
            }

            pBand.PInspectorBandSuppress = true;
            pSlider.Value = Math.Clamp(PInspectorDecimalRead(pValueBox, 0), PEqualizerLeastDb, PEqualizerMostDb);
            pBand.PInspectorBandSuppress = false;
            PInspectorActiveRaise();
            PEqualizerDeviationCheck();
        };
        pFrequencyBox.TextChanged += (_, _) =>
        {
            PInspectorActiveRaise();
            PEqualizerDeviationCheck();
        };
        pRemoveButton.Click += (_, _) => PEqualizerRowRemove(pBand);

        pEqualizerRows.Add(pBand);
        pInspectorEqualizerRowPanel.Children.Add(pLine);

        if (pRaise)
        {
            PInspectorActiveRaise();
        }
    }

    private void PEqualizerRowRemove(PInspectorBand pBand)
    {
        pEqualizerRows.Remove(pBand);
        pInspectorEqualizerRowPanel.Children.Remove(pBand.PInspectorBandRow);
        PInspectorActiveRaise();
        PEqualizerDeviationCheck();
    }

    private static double[]? PEqualizerValuesRead(string pToken) => pToken switch
    {
        "Flat" => new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        "Bass boost" => new double[] { 6, 5, 3, 1, 0, 0, 0, 0, 0, 0 },
        "Bright" => new double[] { 0, 0, 0, 0, 0, 0, 1, 3, 5, 6 },
        "Warm" => new double[] { 2, 3, 2, 1, 0, 0, -1, -2, -3, -2 },
        "Loudness" => new double[] { 6, 4, 2, 0, -2, -3, -1, 1, 4, 6 },
        "Vocal" => new double[] { -3, -2, 0, 1, 2, 2, 3, 2, 1, 0 },
        "De-ess" => new double[] { 0, 0, 0, 0, 0, 0, 0, -2, -6, -3 },
        "Podcast" => new double[] { -6, -3, 0, -1, 0, 1, 2, 3, 2, 1 },
        "Telephone" => new double[] { -12, -10, -4, 0, 2, 4, 3, 0, -8, -12 },
        _ => null
    };

    private static string PEqualizerKeyRead(string pToken) => pToken switch
    {
        "Flat" => "Inspector.Equalizer.Preset.Flat",
        "Bass boost" => "Inspector.Equalizer.Preset.BassBoost",
        "Bright" => "Inspector.Equalizer.Preset.Bright",
        "Warm" => "Inspector.Equalizer.Preset.Warm",
        "Loudness" => "Inspector.Equalizer.Preset.Loudness",
        "Vocal" => "Inspector.Equalizer.Preset.Vocal",
        "De-ess" => "Inspector.Equalizer.Preset.Deess",
        "Podcast" => "Inspector.Equalizer.Preset.Podcast",
        "Telephone" => "Inspector.Equalizer.Preset.Telephone",
        _ => "Inspector.Common.Custom"
    };

    private void PEqualizerRowsApply(double[] pGains)
    {
        pEqualizerRows.Clear();
        pInspectorEqualizerRowPanel.Children.Clear();
        for (int pIndex = 0; pIndex < pEqualizerBandGrid.Length; pIndex++)
        {
            PEqualizerRowAdd(pEqualizerBandGrid[pIndex], pGains[pIndex], false);
        }
    }

    private bool PEqualizerValuesMatch(double[] pGains)
    {
        if (pEqualizerRows.Count != pEqualizerBandGrid.Length)
        {
            return false;
        }

        for (int pIndex = 0; pIndex < pEqualizerRows.Count; pIndex++)
        {
            double pFrequency = PInspectorDecimalRead(pEqualizerRows[pIndex].PInspectorBandFrequency, 0);
            double pGain = PInspectorDecimalRead(pEqualizerRows[pIndex].PInspectorBandValue, 0);
            if (Math.Abs(pFrequency - pEqualizerBandGrid[pIndex]) > 0.5 || Math.Abs(pGain - pGains[pIndex]) > 0.05)
            {
                return false;
            }
        }

        return true;
    }

    private void PEqualizerPresetApply()
    {
        if (pInspectorEqualizerPresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pInspectorEqualizerPreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom" || PEqualizerValuesRead(pName) is not { } pGains)
        {
            pEqualizerBaseToken = null;
            return;
        }

        pInspectorEqualizerPresetSuppress = true;
        pEqualizerBaseToken = pName;
        PEqualizerRowsApply(pGains);
        PEqualizerCustomReset();
        pInspectorEqualizerPresetSuppress = false;
        PInspectorActiveRaise();
    }

    private void PEqualizerDeviationCheck()
    {
        if (pInspectorEqualizerPresetSuppress || pEqualizerBaseToken is not { } pBase
            || PEqualizerValuesRead(pBase) is not { } pGains)
        {
            return;
        }

        pInspectorEqualizerPresetSuppress = true;
        if (PEqualizerValuesMatch(pGains))
        {
            PEqualizerCustomReset();
            PEqualizerPresetSelect(pBase);
        }
        else
        {
            PEqualizerCustomSet(pBase);
        }

        pInspectorEqualizerPresetSuppress = false;
    }

    private void PEqualizerPresetUpdate()
    {
        pInspectorEqualizerPresetSuppress = true;
        string? pMatch = null;
        foreach (string pToken in new[] { "Flat", "Bass boost", "Bright", "Warm", "Loudness", "Vocal", "De-ess", "Podcast", "Telephone" })
        {
            if (PEqualizerValuesRead(pToken) is { } pGains && PEqualizerValuesMatch(pGains))
            {
                pMatch = pToken;
                break;
            }
        }

        if (pMatch is not null)
        {
            pEqualizerBaseToken = pMatch;
            PEqualizerCustomReset();
            PEqualizerPresetSelect(pMatch);
        }
        else
        {
            pEqualizerBaseToken = null;
            PEqualizerCustomReset();
            pInspectorEqualizerPreset.SelectedIndex = pInspectorEqualizerPreset.Items.Count - 1;
        }

        pInspectorEqualizerPresetSuppress = false;
    }

    private void PEqualizerCustomSet(string pBase)
    {
        int pLast = pInspectorEqualizerPreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PEqualizerKeyRead(pBase)));
        pInspectorEqualizerPreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pInspectorEqualizerPreset.SelectedIndex = pLast;
    }

    private void PEqualizerCustomReset()
    {
        int pLast = pInspectorEqualizerPreset.Items.Count - 1;
        pInspectorEqualizerPreset.Items[pLast] = new LLocalizationChoice("Custom", "Inspector.Common.Custom");
    }

    private void PEqualizerPresetSelect(string pToken)
    {
        for (int pIndex = 0; pIndex < pInspectorEqualizerPreset.Items.Count; pIndex++)
        {
            if (LLocalizationChoice.LLocalizationChoiceRead(pInspectorEqualizerPreset.Items[pIndex]) == pToken)
            {
                pInspectorEqualizerPreset.SelectedIndex = pIndex;
                return;
            }
        }
    }

    private static TextBlock PEqualizerUnitBuild(string pUnit) => new()
    {
        Text = pUnit,
        FontSize = 11,
        FontFamily = pInspectorFontFamily,
        Foreground = pInspectorMutedBrush,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(6, 0, 6, 0)
    };

    private void PEqualizerApplyUpdate()
    {
        bool pEqualizerActive = pEqualizerApplyBox.IsChecked == true;
        pInspectorEqualizerStack.IsEnabled = pEqualizerActive;
        pInspectorEqualizerStack.Opacity = pEqualizerActive ? 1 : 0.4;
        PInspectorActiveRaise();
    }
}
