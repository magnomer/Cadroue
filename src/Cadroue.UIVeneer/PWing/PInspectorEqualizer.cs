using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
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
        StackPanel pBody = PEqualizerPanelBuild();
        PEqualizerAttach();
        PEqualizerRowsRebuild();
        return pBody;
    }

    private StackPanel PEqualizerPanelBuild()
    {
        pEqualizerApplyBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Equalizer.ApplyTooltip"));

        pEqualizerPersistent = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Equalizer.PersistentTooltip"));

        pEqualizerPreset = PInspectorChoiceBuild(
            QEqualizer.QEqualizerChoiceRead().QInspectorChoiceNames, QEqualizer.QEqualizerChoiceSelect);

        pEqualizerRowPanel = new StackPanel();

        Button pAddButton = PInspectorButtonBuild(
            pEqualizerAddIcon,
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Equalizer.Add"),
            () => QEqualizer.QEqualizerBandAdd());
        pAddButton.HorizontalAlignment = HorizontalAlignment.Left;

        pEqualizerStack = new StackPanel();
        pEqualizerStack.Children.Add(PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Preset"), pEqualizerPreset));
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
        return pEqualizerBody;
    }

    private void PEqualizerAttach()
    {
        PInspectorSwitchAttach(pEqualizerApplyBox, QEqualizer.QEqualizerActiveSet);
        PInspectorSwitchAttach(pEqualizerPersistent, QEqualizer.QEqualizerPersistentSet);
        QEqualizer.QEqualizerRowsChanged += PEqualizerRowsRebuild;
    }

    private void PEqualizerUpdate()
    {
        PInspectorSwitchUpdate(pEqualizerApplyBox, QEqualizer.QEqualizerActive);
        PInspectorSwitchUpdate(pEqualizerPersistent, QEqualizer.QEqualizerPersistent);
        pEqualizerRows.ForEach(PEqualizerRowUpdate);
        PInspectorChoiceApply(pEqualizerPreset, QEqualizer.QEqualizerChoiceRead());
        PInspectorSectionUpdate(pEqualizerStack, QEqualizer.QEqualizerActive);
    }

    private void PEqualizerRowsRebuild()
    {
        pEqualizerRows.Clear();
        pEqualizerRowPanel.Children.Clear();
        pEqualizerRows.AddRange(QEqualizer.QEqualizerRows.Select(PEqualizerRowBuild));
        pEqualizerRows.ForEach(pBand => pEqualizerRowPanel.Children.Add(pBand.PInspectorBandRow));
    }

    private void PEqualizerRowUpdate(PInspectorBand pBand)
    {
        pBand.PInspectorBandFrequency.Text = QEqualizer.QEqualizerFrequencyRead(pBand.PInspectorBandIndex);
        PInspectorValueUpdate(
            pBand.PInspectorBandSlider,
            pBand.PInspectorBandValue,
            QEqualizer.QEqualizerGainRead(pBand.PInspectorBandIndex),
            "0.#");
    }

    private PInspectorBand PEqualizerRowBuild(QEqualizerBand lBand)
    {
        (PInspectorBand pBand, Button pRemoveButton) = PEqualizerBandBuild(lBand);
        PEqualizerBandAttach(pBand, pRemoveButton);
        return pBand;
    }

    private (PInspectorBand, Button) PEqualizerBandBuild(QEqualizerBand lBand)
    {
        int pIndex = lBand.QEqualizerBandIndex;
        TextBox pFrequencyBox = PInspectorDecimalBuild();
        pFrequencyBox.Width = 56;
        pFrequencyBox.Text = lBand.QEqualizerBandFrequency;

        Slider pSlider = PInspectorSliderBuild(
            QEqualizer.QEqualizerGainLeast, QEqualizer.QEqualizerGainMost, lBand.QEqualizerBandGain, static () => 0);

        TextBox pValueBox = PInspectorDecimalBuild();
        pValueBox.Width = pEqualizerValueWidth;

        var pRemoveButton = new Button
        {
            Content = new Image
            {
                Width = 16,
                Height = 16,
                Source = QIcon.QIconRead(pEqualizerRemoveIcon, pInspectorIconBrush),
                Stretch = Stretch.Uniform
            },
            Width = 26,
            Height = PInspectorFieldHeight,
            Margin = new Thickness(6, 0, 0, 8),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = QEqualizer.QEqualizerRemoveTip
        };
        QButton.QButtonAttach(pRemoveButton, QButtonLook.QButtonLookPanel);

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
        return (new PInspectorBand(pIndex, pLine, pFrequencyBox, pSlider, pValueBox), pRemoveButton);
    }

    private void PEqualizerBandAttach(PInspectorBand pBand, Button pRemoveButton)
    {
        int pIndex = pBand.PInspectorBandIndex;
        TextBox pFrequencyBox = pBand.PInspectorBandFrequency;
        PInspectorValueAttach(
            pBand.PInspectorBandSlider,
            pBand.PInspectorBandValue,
            QEqualizer.QEqualizerGainLeast,
            QEqualizer.QEqualizerGainMost,
            () => QEqualizer.QEqualizerGainRead(pIndex),
            pGain => QEqualizer.QEqualizerGainSet(pIndex, pGain));
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
        pRemoveButton.Click += (_, _) => QEqualizer.QEqualizerBandRemove(pIndex);
    }

    private void PEqualizerFrequencyCommit(int pIndex, TextBox pFrequencyBox)
    {
        string pFrequency = QEqualizer.QEqualizerFrequencyCommit(pIndex, pFrequencyBox.Text);
        PEqualizerFrequencyApply(pFrequencyBox, pFrequency);
        PInspectorCaretPlace(pFrequencyBox);
    }

    private static void PEqualizerFrequencyApply(TextBox pFrequencyBox, string pFrequency) =>
        pFrequencyBox.Text = pFrequency;

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
