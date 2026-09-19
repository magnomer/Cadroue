using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private const string pEqualizerRemoveIcon = "/PAsset/PPanel/PFunnelRemove.svg";

    private sealed class PInspectorBand
    {
        public required Grid PInspectorBandRow { get; init; }
        public required TextBox PInspectorBandFrequency { get; init; }
        public required Slider PInspectorBandSlider { get; init; }
        public required TextBox PInspectorBandValue { get; init; }
    }

    private readonly List<PInspectorBand> pEqualizerRows = new();

    private void PEqualizerRowsUpdate()
    {
        IReadOnlyList<LWorkBand> pBands = LEqualizer.LEqualizerBands;
        while (pEqualizerRows.Count > pBands.Count)
        {
            PInspectorBand pLast = pEqualizerRows[^1];
            pEqualizerRows.RemoveAt(pEqualizerRows.Count - 1);
            pEqualizerRowPanel.Children.Remove(pLast.PInspectorBandRow);
        }

        while (pEqualizerRows.Count < pBands.Count)
        {
            PEqualizerRowAdd(pEqualizerRows.Count);
        }

        for (int pIndex = 0; pIndex < pBands.Count; pIndex++)
        {
            PInspectorBand pRow = pEqualizerRows[pIndex];
            PInspectorTextSet(pRow.PInspectorBandFrequency, pBands[pIndex].LWorkBandFrequency, "0.###");
            PInspectorValueUpdate(
                pRow.PInspectorBandSlider, pRow.PInspectorBandValue, pBands[pIndex].LWorkBandGain, "0.#");
        }
    }

    private LWorkBand PEqualizerBandRead(int pIndex) =>
        pIndex < LEqualizer.LEqualizerBands.Count
            ? LEqualizer.LEqualizerBands[pIndex]
            : new LWorkBand(LContourCatalog.LContourFrequencyDefault, 0);

    private void PEqualizerRowAdd(int pIndex)
    {
        var pFrequencyBox = PInspectorDecimalBuild();
        pFrequencyBox.Width = 56;

        var pSlider = new Slider
        {
            Minimum = LContourCatalog.LContourGainLeast,
            Maximum = LContourCatalog.LContourGainMost,
            Value = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
        PSlider.PSliderApply(pSlider);
        PSlider.PSliderResetApply(pSlider, static () => 0);

        var pValueBox = PInspectorDecimalBuild();
        pValueBox.Width = PInspectorInsetWidth / 2;

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

        PInspectorValueAttach(
            pSlider,
            pValueBox,
            LContourCatalog.LContourGainLeast,
            LContourCatalog.LContourGainMost,
            () => PEqualizerBandRead(pEqualizerRows.IndexOf(pBand)).LWorkBandGain,
            pGain => PEqualizerBandSet(pBand, pFrequencyBox, pGain));
        pFrequencyBox.LostFocus += (_, _) => PEqualizerFrequencyCommit(pBand, pFrequencyBox);
        pFrequencyBox.KeyDown += (_, pKeyEvent) =>
        {
            if (pKeyEvent.Key != Key.Enter)
            {
                return;
            }

            pKeyEvent.Handled = true;
            PEqualizerFrequencyCommit(pBand, pFrequencyBox);
        };
        pRemoveButton.Click += (_, _) => LEqualizer.LEqualizerBandRemove(pEqualizerRows.IndexOf(pBand));

        pEqualizerRows.Insert(pIndex, pBand);
        pEqualizerRowPanel.Children.Insert(pIndex, pLine);
    }

    private void PEqualizerFrequencyCommit(PInspectorBand pBand, TextBox pFrequencyBox)
    {
        int pSlot = pEqualizerRows.IndexOf(pBand);
        LWorkBand pCurrent = PEqualizerBandRead(pSlot);
        LEqualizer.LEqualizerBandSet(
            pSlot,
            PInspectorDecimalRead(pFrequencyBox, pCurrent.LWorkBandFrequency),
            pCurrent.LWorkBandGain);
        PInspectorTextSet(pFrequencyBox, PEqualizerBandRead(pSlot).LWorkBandFrequency, "0.###");
    }

    private void PEqualizerBandSet(PInspectorBand pBand, TextBox pFrequencyBox, double pGain)
    {
        int pSlot = pEqualizerRows.IndexOf(pBand);
        LWorkBand pCurrent = PEqualizerBandRead(pSlot);
        LEqualizer.LEqualizerBandSet(
            pSlot, PInspectorDecimalRead(pFrequencyBox, pCurrent.LWorkBandFrequency), pGain);
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
