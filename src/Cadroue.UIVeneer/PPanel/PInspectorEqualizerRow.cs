using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private readonly record struct PEqualizerCurrent(double[] PEqualizerFrequencies, double[] PEqualizerGains);

    private const string pEqualizerRemoveIcon = "/PAsset/PPanel/PFunnelRemove.svg";

    private sealed class PInspectorBand
    {
        public required Grid PInspectorBandRow { get; init; }
        public required TextBox PInspectorBandFrequency { get; init; }
        public required Slider PInspectorBandSlider { get; init; }
        public required TextBox PInspectorBandValue { get; init; }
        public bool PInspectorBandSuppress { get; set; }
    }

    private readonly List<PInspectorBand> pEqualizerRows = new();

    private void PEqualizerRowAdd(double pFrequency, double pGain, bool pRaise)
    {
        var pFrequencyBox = PInspectorDecimalBuild();
        pFrequencyBox.Width = 56;
        pFrequencyBox.Text = pFrequency.ToString("0.###", CultureInfo.InvariantCulture);

        var pSlider = new Slider
        {
            Minimum = LContourCatalog.LContourGainLeast,
            Maximum = LContourCatalog.LContourGainMost,
            Value = Math.Clamp(pGain, LContourCatalog.LContourGainLeast, LContourCatalog.LContourGainMost),
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
            pSlider.Value = Math.Clamp(
                PInspectorDecimalRead(pValueBox, 0),
                LContourCatalog.LContourGainLeast, LContourCatalog.LContourGainMost);
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
        pEqualizerRowPanel.Children.Add(pLine);

        if (pRaise)
        {
            PInspectorActiveRaise();
        }
    }

    private void PEqualizerRowRemove(PInspectorBand pBand)
    {
        pEqualizerRows.Remove(pBand);
        pEqualizerRowPanel.Children.Remove(pBand.PInspectorBandRow);
        PInspectorActiveRaise();
        PEqualizerDeviationCheck();
    }

    private void PEqualizerRowsApply(double[] pGains)
    {
        double[] pGrid = LContourCatalog.LContourBandGrid;
        pEqualizerRows.Clear();
        pEqualizerRowPanel.Children.Clear();
        for (int pIndex = 0; pIndex < pGrid.Length; pIndex++)
        {
            PEqualizerRowAdd(pGrid[pIndex], pGains[pIndex], false);
        }
    }

    private PEqualizerCurrent PEqualizerCurrentRead()
    {
        var pFrequencies = new double[pEqualizerRows.Count];
        var pGains = new double[pEqualizerRows.Count];
        for (int pIndex = 0; pIndex < pEqualizerRows.Count; pIndex++)
        {
            pFrequencies[pIndex] = PInspectorDecimalRead(pEqualizerRows[pIndex].PInspectorBandFrequency, 0);
            pGains[pIndex] = PInspectorDecimalRead(pEqualizerRows[pIndex].PInspectorBandValue, 0);
        }

        return new PEqualizerCurrent(pFrequencies, pGains);
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
}
