using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PInspectorRatio
{
    private static readonly Brush pInspectorWarnBrush = new SolidColorBrush(Color.FromRgb(0xC2, 0x5A, 0x1E));

    private readonly TextBox pInspectorRatioWidth;
    private readonly TextBox pInspectorRatioHeight;
    private readonly ComboBox pInspectorRatioPreset;
    private readonly StackPanel pInspectorCustomPanel;

    internal PInspectorRatio(QInspectorRatio lRatio, QInspectorCrop lCrop)
    {
        QInspectorRatio = lRatio;
        QInspectorCrop = lCrop;
        pInspectorRatioWidth = PInspectorNumber.PInspectorNumberBuild();
        pInspectorRatioHeight = PInspectorNumber.PInspectorNumberBuild();
        pInspectorRatioPreset = PInspectorRatioBuild();

        PInspectorRatioFixed = new CheckBox
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.FixedRatio"),
            FontSize = 12,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = QPanel.QPanelTextRead(),
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(PInspectorLook.PInspectorLabelWidth, 8, 0, 0)
        };
        QInteractionClick.QInteractionClickAttach(PInspectorRatioFixed);
        QCheckbox.QCheckboxAttach(PInspectorRatioFixed);

        PInspectorRatioLenient = new CheckBox
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Lenient"),
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.LenientTooltip"),
            FontSize = 12,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = QPanel.QPanelTextRead(),
            VerticalContentAlignment = VerticalAlignment.Center,
            IsEnabled = false,
            Margin = new Thickness(PInspectorLook.PInspectorLabelWidth, 4, 0, 0)
        };
        QInteractionClick.QInteractionClickAttach(PInspectorRatioLenient);
        QCheckbox.QCheckboxAttach(PInspectorRatioLenient);

        PInspectorRatioNotice = new TextBlock
        {
            FontSize = 11,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = pInspectorWarnBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(PInspectorLook.PInspectorLabelWidth, 6, 0, 0),
            Visibility = Visibility.Collapsed
        };

        pInspectorCustomPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(PInspectorLook.PInspectorLabelWidth, 6, 0, 0)
        };
        PInspectorRatioBody = PCropRatioBuild();
        PInspectorRatioAttach();
    }

    private QInspectorRatio QInspectorRatio { get; }

    private QInspectorCrop QInspectorCrop { get; }

    internal UIElement PInspectorRatioBody { get; }

    internal CheckBox PInspectorRatioFixed { get; }

    internal CheckBox PInspectorRatioLenient { get; }

    internal TextBlock PInspectorRatioNotice { get; }

    private void PInspectorRatioAttach()
    {
        pInspectorRatioWidth.TextChanged += (_, _) => PInspectorRatioCommit(pInspectorRatioWidth);
        pInspectorRatioHeight.TextChanged += (_, _) => PInspectorRatioCommit(pInspectorRatioHeight);
        PInspectorSwitch.PInspectorSwitchAttach(PInspectorRatioFixed, QInspectorRatio.QInspectorFixedSet);
        PInspectorSwitch.PInspectorSwitchAttach(PInspectorRatioLenient, QInspectorRatio.QInspectorLenientSet);
    }

    private void PInspectorRatioCommit(TextBox pRatioBox) =>
        QInspectorRatio.QInspectorRatioCommit(
            pInspectorRatioWidth.Text, pInspectorRatioHeight.Text, pRatioBox.IsKeyboardFocusWithin);

    private ComboBox PInspectorRatioBuild()
    {
        ComboBox pPresetCombo = PCropPresetBuild();
        PCropPresetAttach(pPresetCombo);
        return pPresetCombo;
    }

    private static ComboBox PCropPresetBuild()
    {
        ComboBox pPresetCombo = PInspectorChoice.PInspectorComboBuild();
        pPresetCombo.Items.Add(new LLocalizationChoice("Custom", "Inspector.Crop.RatioCustom"));
        pPresetCombo.Items.Add("16:9");
        pPresetCombo.Items.Add("9:16");
        pPresetCombo.Items.Add("4:3");
        pPresetCombo.Items.Add("3:4");
        pPresetCombo.Items.Add("1:1");
        pPresetCombo.Items.Add("21:9");
        pPresetCombo.SelectedIndex = 0;
        return pPresetCombo;
    }

    private void PCropPresetAttach(ComboBox pPresetCombo) =>
        pPresetCombo.SelectionChanged += (_, _) =>
            QInspectorRatio.QInspectorPresetSelect(pPresetCombo.SelectedIndex);

    private UIElement PCropRatioBuild()
    {
        var pRatioRoot = new StackPanel
        {
            Margin = new Thickness(0, 14, 0, 0)
        };
        var pPresetPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pPresetPanel.Children.Add(PInspectorField.PInspectorLabelBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Inspector.Crop.Ratio")));
        pPresetPanel.Children.Add(pInspectorRatioPreset);

        pInspectorCustomPanel.Children.Add(pInspectorRatioWidth);
        pInspectorCustomPanel.Children.Add(new TextBlock
        {
            Text = "×",
            FontSize = 12,
            FontFamily = PInspectorLook.PInspectorFontFamily,
            Foreground = PInspectorLook.PInspectorMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(7, 0, 7, 0)
        });
        pInspectorCustomPanel.Children.Add(pInspectorRatioHeight);
        pRatioRoot.Children.Add(pPresetPanel);
        pRatioRoot.Children.Add(pInspectorCustomPanel);
        return pRatioRoot;
    }

    internal void PInspectorRatioUpdate()
    {
        QInspectorCrop lCrop = QInspectorCrop;
        pInspectorRatioPreset.SelectedIndex = lCrop.QInspectorPresetIndex;
        pInspectorCustomPanel.Visibility = PLook.PLookVisible[lCrop.QInspectorCustomShown];
        PInspectorRatioFixed.IsChecked = PLook.PLookChecked[lCrop.QInspectorRatioFixed];
        PInspectorRatioLenient.IsChecked = PLook.PLookChecked[lCrop.QInspectorRatioLenient];
        PInspectorRatioLenient.IsEnabled = lCrop.QInspectorRatioFixed;
        pInspectorRatioWidth.Text = QInspectorRatio.QInspectorRatioFormat(true, pInspectorRatioWidth.Text);
        pInspectorRatioHeight.Text = QInspectorRatio.QInspectorRatioFormat(false, pInspectorRatioHeight.Text);
        PInspectorRatioNotice.Text = lCrop.QInspectorNoticeRead();
        PInspectorRatioNotice.Visibility = PLook.PLookVisible[lCrop.QInspectorNoticeShown];
    }
}
