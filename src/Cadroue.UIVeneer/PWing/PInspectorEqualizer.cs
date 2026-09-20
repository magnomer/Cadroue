using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private const string pEqualizerAddIcon = "/PAsset/PPanel/PFunnelAdd.svg";
    private const string pEqualizerRemoveIcon = "/PAsset/PPanel/PFunnelRemove.svg";
    private const double pEqualizerValueWidth = 34;

    private sealed record PInspectorBand(
        int PInspectorBandIndex,
        Grid PInspectorBandRow,
        TextBox PInspectorBandFrequency,
        Slider PInspectorBandSlider,
        TextBox PInspectorBandValue);

    private readonly List<PInspectorBand> pEqualizerRows = new();

    private CheckBox pEqualizerApplyBox = null!;
    private CheckBox pEqualizerPersistent = null!;
    private ComboBox pEqualizerPreset = null!;
    private StackPanel pEqualizerStack = null!;
    private StackPanel pEqualizerRowPanel = null!;
    private StackPanel pEqualizerBody = null!;

    private StackPanel PEqualizerBodyBuild()
    {
        pEqualizerApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Equalizer.ApplyTooltip"));
        PInspectorSwitchAttach(pEqualizerApplyBox, LEqualizer.LEqualizerActiveSet);

        pEqualizerPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Equalizer.PersistentTooltip"));
        PInspectorSwitchAttach(pEqualizerPersistent, LEqualizer.LEqualizerPersistentSet);

        pEqualizerPreset = PInspectorChoiceBuild(
            LEqualizer.LEqualizerChoiceRead().LInspectorChoiceNames, LEqualizer.LEqualizerChoiceSelect);

        pEqualizerRowPanel = new StackPanel();

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
        pAddButton.Click += (_, _) => LEqualizer.LEqualizerBandAdd();

        pEqualizerStack = new StackPanel();
        pEqualizerStack.Children.Add(PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pEqualizerPreset));
        pEqualizerStack.Children.Add(pEqualizerRowPanel);
        pEqualizerStack.Children.Add(pAddButton);

        pEqualizerBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pEqualizerBody.Children.Add(pEqualizerApplyBox);
        pEqualizerBody.Children.Add(PInspectorSeparatorBuild());
        pEqualizerBody.Children.Add(pEqualizerStack);
        LEqualizer.LEqualizerRowsChange += PEqualizerRowsRebuild;
        PEqualizerRowsRebuild();
        return pEqualizerBody;
    }

    private void PEqualizerUpdate()
    {
        PInspectorSwitchUpdate(pEqualizerApplyBox, LEqualizer.LEqualizerActive);
        PInspectorSwitchUpdate(pEqualizerPersistent, LEqualizer.LEqualizerPersistent);
        pEqualizerRows.ForEach(PEqualizerRowUpdate);
        PInspectorChoiceApply(pEqualizerPreset, LEqualizer.LEqualizerChoiceRead());
        PInspectorSectionUpdate(pEqualizerStack, LEqualizer.LEqualizerActive);
    }

    private void PEqualizerRowsRebuild()
    {
        pEqualizerRows.Clear();
        pEqualizerRowPanel.Children.Clear();
        pEqualizerRows.AddRange(LEqualizer.LEqualizerRows.Select(PEqualizerRowBuild));
        pEqualizerRows.ForEach(pBand => pEqualizerRowPanel.Children.Add(pBand.PInspectorBandRow));
    }

    private void PEqualizerRowUpdate(PInspectorBand pBand)
    {
        pBand.PInspectorBandFrequency.Text = LEqualizer.LEqualizerFrequencyRead(pBand.PInspectorBandIndex);
        PInspectorValueUpdate(
            pBand.PInspectorBandSlider,
            pBand.PInspectorBandValue,
            LEqualizer.LEqualizerGainRead(pBand.PInspectorBandIndex),
            "0.#");
    }

    private PInspectorBand PEqualizerRowBuild(LEqualizerBand lBand)
    {
        int pIndex = lBand.LEqualizerBandIndex;
        TextBox pFrequencyBox = PInspectorDecimalBuild();
        pFrequencyBox.Width = 56;
        pFrequencyBox.Text = lBand.LEqualizerBandFrequency;

        Slider pSlider = PInspectorSliderBuild(
            LEqualizer.LEqualizerGainLeast, LEqualizer.LEqualizerGainMost, lBand.LEqualizerBandGain, static () => 0);

        TextBox pValueBox = PInspectorDecimalBuild();
        pValueBox.Width = pEqualizerValueWidth;

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
            Margin = new Thickness(6, 0, 0, 8),
            VerticalAlignment = VerticalAlignment.Center,
            Style = PButton.PButtonPanelCreate(),
            ToolTip = LEqualizer.LEqualizerRemoveTip
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

        TextBlock pHzUnit = PEqualizerUnitBuild("Hz");
        TextBlock pDbUnit = PEqualizerUnitBuild("dB");
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
        pLine.Children.Add(pRow);
        pLine.Children.Add(pRemoveButton);

        PInspectorValueAttach(
            pSlider,
            pValueBox,
            LEqualizer.LEqualizerGainLeast,
            LEqualizer.LEqualizerGainMost,
            () => LEqualizer.LEqualizerGainRead(pIndex),
            pGain => LEqualizer.LEqualizerGainSet(pIndex, pGain));
        var pFrequencyKeys = new Dictionary<Key, Action>
        {
            [Key.Enter] = () => PEqualizerFrequencyCommit(pIndex, pFrequencyBox),
        };
        pFrequencyBox.LostFocus += (_, _) => PEqualizerFrequencyCommit(pIndex, pFrequencyBox);
        pFrequencyBox.KeyDown += (_, pKeyEvent) =>
        {
            pFrequencyKeys.GetValueOrDefault(pKeyEvent.Key)?.Invoke();
            pKeyEvent.Handled = pInspectorValueHandled.GetValueOrDefault(pKeyEvent.Key);
        };
        pRemoveButton.Click += (_, _) => LEqualizer.LEqualizerBandRemove(pIndex);
        return new PInspectorBand(pIndex, pLine, pFrequencyBox, pSlider, pValueBox);
    }

    private void PEqualizerFrequencyCommit(int pIndex, TextBox pFrequencyBox)
    {
        pFrequencyBox.Text = LEqualizer.LEqualizerFrequencyCommit(pIndex, pFrequencyBox.Text);
        pFrequencyBox.CaretIndex = pFrequencyBox.Text.Length;
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
