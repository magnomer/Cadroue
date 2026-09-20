using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
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
        pInspectorRatioWidth.TextChanged += (_, _) => PInspectorRatioCommit();
        pInspectorRatioHeight.TextChanged += (_, _) => PInspectorRatioCommit();
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
            Content = LLocalization.LLocalizationTextRead("Inspector.Crop.FixedRatio"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            Margin = new Thickness(PInspectorLabelWidth, 8, 0, 0)
        };
        PCheckbox.PCheckboxApply(pInspectorRatioFixed);
        PInspectorSwitchAttach(pInspectorRatioFixed, LInspectorCrop.LInspectorFixedSet);

        pInspectorRatioLenient = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Crop.Lenient"),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Crop.LenientTooltip"),
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            Foreground = PPanelTextBrush,
            VerticalContentAlignment = VerticalAlignment.Center,
            IsEnabled = false,
            Margin = new Thickness(PInspectorLabelWidth, 4, 0, 0)
        };
        PCheckbox.PCheckboxApply(pInspectorRatioLenient);
        PInspectorSwitchAttach(pInspectorRatioLenient, LInspectorCrop.LInspectorLenientSet);

        pInspectorRatioNotice = new TextBlock
        {
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Foreground = pInspectorWarnBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(PInspectorLabelWidth, 6, 0, 0),
            Visibility = Visibility.Collapsed
        };

        pInspectorFlipHorizontal = PCropCheckBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Horizontal"));
        pInspectorFlipVertical = PCropCheckBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Vertical"));
        PInspectorSwitchAttach(pInspectorFlipHorizontal, pFlipped => LInspectorCrop.LInspectorFlipSet(true, pFlipped));
        PInspectorSwitchAttach(pInspectorFlipVertical, pFlipped => LInspectorCrop.LInspectorFlipSet(false, pFlipped));
        pInspectorRotateCombo = PInspectorRotateBuild();
        pInspectorCropTool = PInspectorToolBuild();

        pInspectorApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Crop.ApplyTooltip"));
        PInspectorSwitchAttach(pInspectorApplyBox, LInspectorCrop.LInspectorApplySet);
        pInspectorPersistentBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Crop.PersistentTooltip"));
        PInspectorSwitchAttach(pInspectorPersistentBox, LInspectorCrop.LInspectorPersistentSet);

        pInspectorCropStack = new StackPanel();
        pInspectorCropStack.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Tool"), pInspectorCropTool));
        pInspectorCropStack.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Flip"), PCropFlipBuild()));
        pInspectorCropStack.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Crop.Rotate"), pInspectorRotateCombo));
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

    private void PInspectorRatioCommit() =>
        LInspectorCrop.LInspectorRatioCommit(pInspectorRatioWidth.Text, pInspectorRatioHeight.Text);

    private TextBox PInspectorInsetBuild(int pEdge)
    {
        TextBox pInsetBox = PInspectorNumberBuild();
        pInsetBox.TextChanged += (_, _) => LInspectorCrop.LInspectorEdgeCommit(pEdge, pInsetBox.Text);
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
        PCheckbox.PCheckboxApply(pFlip);
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
        pRotateCombo.SelectionChanged += (_, _) => LInspectorCrop.LInspectorRotateSelect(pRotateCombo.SelectedIndex);
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
        pPresetCombo.SelectionChanged += (_, _) => LInspectorCrop.LInspectorPresetSelect(pPresetCombo.SelectedIndex);
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
