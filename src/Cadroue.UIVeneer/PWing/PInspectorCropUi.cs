using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private StackPanel PCropBodyBuild()
    {
        pInspectorInsetLeft = PInspectorInsetBuild(0);
        pInspectorInsetRight = PInspectorInsetBuild(2);
        pInspectorInsetTop = PInspectorInsetBuild(1);
        pInspectorInsetBottom = PInspectorInsetBuild(3);
        pInspectorRatioWidth = PInspectorNumberBuild();
        pInspectorRatioHeight = PInspectorNumberBuild();
        pInspectorRatioWidth.TextChanged += (_, _) => PInspectorRatioCommit(pInspectorRatioWidth);
        pInspectorRatioHeight.TextChanged += (_, _) => PInspectorRatioCommit(pInspectorRatioHeight);
        pInspectorRatioPreset = PInspectorRatioBuild();
        pInspectorResolution = new TextBlock
        {
            Text = "—",
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        pInspectorRatioFixed = new CheckBox
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.FixedRatio"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(PInspectorLabelWidth, 8, 0, 0)
        };
        PInteraction.PInteractionButtonAttach(pInspectorRatioFixed);
        QCheckbox.QCheckboxAttach(pInspectorRatioFixed);
        PInspectorSwitchAttach(
            pInspectorRatioFixed, QInspector.QInspectorRatio.QInspectorFixedSet);

        pInspectorRatioLenient = new CheckBox
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Lenient"),
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.LenientTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            IsEnabled = false,
            Margin = new Thickness(PInspectorLabelWidth, 4, 0, 0)
        };
        PInteraction.PInteractionButtonAttach(pInspectorRatioLenient);
        QCheckbox.QCheckboxAttach(pInspectorRatioLenient);
        PInspectorSwitchAttach(
            pInspectorRatioLenient, QInspector.QInspectorRatio.QInspectorLenientSet);

        pInspectorRatioNotice = new TextBlock
        {
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorWarnBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(PInspectorLabelWidth, 6, 0, 0),
            Visibility = Visibility.Collapsed
        };

        pInspectorFlipHorizontal = PCropCheckBuild(QLocalizationCatalog.QLocalizationTextRead(
            "Inspector.Crop.Horizontal"));
        pInspectorFlipVertical = PCropCheckBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Vertical"));
        PInspectorSwitchAttach(
            pInspectorFlipHorizontal, pFlipped => QInspector.QInspectorOrientation.QInspectorFlipSet(true, pFlipped));
        PInspectorSwitchAttach(
            pInspectorFlipVertical, pFlipped => QInspector.QInspectorOrientation.QInspectorFlipSet(false, pFlipped));
        pInspectorRotateCombo = PInspectorRotateBuild();
        pInspectorCropTool = PInspectorToolBuild();

        pInspectorApplyBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.ApplyTooltip"));
        PInspectorSwitchAttach(pInspectorApplyBox, QInspectorCrop.QInspectorApplySet);
        pInspectorPersistentBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.PersistentTooltip"));
        PInspectorSwitchAttach(pInspectorPersistentBox, QInspectorCrop.QInspectorPersistentSet);

        pInspectorCropStack = new StackPanel();
        pInspectorCropStack.Children.Add(
            PInspectorFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Tool"),
                pInspectorCropTool));
        pInspectorCropStack.Children.Add(
            PInspectorFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Flip"), PCropFlipBuild()));
        pInspectorCropStack.Children.Add(
            PInspectorFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Crop.Rotate"),
                pInspectorRotateCombo));
        pInspectorCropStack.Children.Add(PInspectorEdgeBuild());
        pInspectorCropStack.Children.Add(PCropRatioBuild());
        pInspectorCropStack.Children.Add(pInspectorRatioFixed);
        pInspectorCropStack.Children.Add(pInspectorRatioLenient);
        pInspectorCropStack.Children.Add(pInspectorRatioNotice);

        pInspectorCropBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12),
            Visibility = Visibility.Collapsed
        };
        pInspectorCropBody.Children.Add(pInspectorApplyBox);
        pInspectorCropBody.Children.Add(PInspectorSeparatorBuild());
        pInspectorCropBody.Children.Add(pInspectorCropStack);
        return pInspectorCropBody;
    }

    private void PInspectorRatioCommit(TextBox pRatioBox) =>
        QInspector.QInspectorRatio.QInspectorRatioCommit(
            pInspectorRatioWidth.Text, pInspectorRatioHeight.Text, pRatioBox.IsKeyboardFocusWithin);

    private TextBox PInspectorInsetBuild(int pEdge)
    {
        TextBox pInsetBox = PInspectorNumberBuild();
        pInsetBox.TextChanged += (_, _) =>
            QInspectorCrop.QInspectorEdgeCommit(pEdge, pInsetBox.Text, pInsetBox.IsKeyboardFocusWithin);
        return pInsetBox;
    }

    private UIElement PCropFlipBuild()
    {
        var pFlipPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pFlipPanel.Children.Add(pInspectorFlipHorizontal);
        pFlipPanel.Children.Add(pInspectorFlipVertical);
        return pFlipPanel;
    }

    private static CheckBox PCropCheckBuild(string pFlipLabel)
    {
        var pFlip = new CheckBox
        {
            Content = pFlipLabel,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 14, 0)
        };
        PInteraction.PInteractionButtonAttach(pFlip);
        QCheckbox.QCheckboxAttach(pFlip);
        return pFlip;
    }

    private ComboBox PInspectorRotateBuild()
    {
        ComboBox pRotateCombo = PInspectorComboBuild();
        pRotateCombo.Items.Add(new LLocalizationChoice("None", "Inspector.Crop.None"));
        pRotateCombo.Items.Add(new LLocalizationChoice("Clockwise90", "Inspector.Crop.Clockwise90"));
        pRotateCombo.Items.Add(new LLocalizationChoice("Degrees180", "Inspector.Crop.Degrees180"));
        pRotateCombo.Items.Add(new LLocalizationChoice("Clockwise270", "Inspector.Crop.Clockwise270"));
        pRotateCombo.SelectedIndex = 0;
        pRotateCombo.SelectionChanged += (_, _) =>
            QInspector.QInspectorOrientation.QInspectorRotateSelect(pRotateCombo.SelectedIndex);
        return pRotateCombo;
    }

    private ComboBox PInspectorRatioBuild()
    {
        ComboBox pPresetCombo = PInspectorComboBuild();
        pPresetCombo.Items.Add(new LLocalizationChoice("Custom", "Inspector.Crop.RatioCustom"));
        pPresetCombo.Items.Add("16:9");
        pPresetCombo.Items.Add("9:16");
        pPresetCombo.Items.Add("4:3");
        pPresetCombo.Items.Add("3:4");
        pPresetCombo.Items.Add("1:1");
        pPresetCombo.Items.Add("21:9");
        pPresetCombo.SelectedIndex = 0;
        pPresetCombo.SelectionChanged += (_, _) =>
            QInspector.QInspectorRatio.QInspectorPresetSelect(pPresetCombo.SelectedIndex);
        return pPresetCombo;
    }

    private static ComboBox PInspectorComboBuild()
    {
        var pCombo = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pCombo);
        return pCombo;
    }
}
