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
    private StackPanel pInspectorEqualizerStack = null!;
    private StackPanel pInspectorEqualizerRowPanel = null!;
    private StackPanel pInspectorEqualizerBody = null!;
    private readonly List<PInspectorBand> pEqualizerRows = new();

    public LWorkAudioStep PEqualizerStepRead()
    {
        var pBands = new List<LWorkEqualizerBand>();
        foreach (PInspectorBand pRow in pEqualizerRows)
        {
            double pFrequency = Math.Clamp(
                PInspectorDecimalRead(pRow.PInspectorBandFrequency, PEqualizerDefaultHz),
                PEqualizerLeastHz, PEqualizerMostHz);
            double pGain = Math.Clamp(
                PInspectorDecimalRead(pRow.PInspectorBandValue, 0), PEqualizerLeastDb, PEqualizerMostDb);
            pBands.Add(new LWorkEqualizerBand(pFrequency, pGain));
        }

        return LWorkAudioStep.LWorkEqualizerCreate(pEqualizerApplyBox.IsChecked == true, pBands);
    }

    private void PEqualizerActiveSet(LWorkEqualizerStep pStep)
    {
        pEqualizerApplyBox.IsChecked = pStep.LWorkAudioStepActive;
        pEqualizerRows.Clear();
        pInspectorEqualizerRowPanel.Children.Clear();
        foreach (LWorkEqualizerBand pBand in pStep.LWorkEqualizerBands)
        {
            PEqualizerRowAdd(pBand.LWorkEqualizerBandFrequency, pBand.LWorkEqualizerBandGain, false);
        }

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
        pAddButton.Click += (_, _) => PEqualizerRowAdd(PEqualizerDefaultHz, 0, true);

        pInspectorEqualizerStack = new StackPanel();
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

        foreach (LWorkEqualizerBand pBand in LWorkEqualizerStep.LWorkEqualizerDefaultCreate())
        {
            PEqualizerRowAdd(pBand.LWorkEqualizerBandFrequency, pBand.LWorkEqualizerBandGain, false);
        }

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
        };
        pFrequencyBox.TextChanged += (_, _) => PInspectorActiveRaise();
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
