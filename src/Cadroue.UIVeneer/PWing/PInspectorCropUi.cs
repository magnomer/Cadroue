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
        pInspectorRatioWidth = PCropFieldBuild();
        pInspectorRatioHeight = PCropFieldBuild();
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
        PInspectorSwitchAttach(pInspectorRatioFixed, pFixed => PInspectorRatioCommit(pFixed, null));

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
        PInspectorSwitchAttach(pInspectorRatioLenient, pLenient => PInspectorRatioCommit(null, pLenient));

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
        PInspectorSwitchAttach(pInspectorFlipHorizontal, pFlipped => PInspectorFlipChange(true, pFlipped));
        PInspectorSwitchAttach(pInspectorFlipVertical, pFlipped => PInspectorFlipChange(false, pFlipped));
        pInspectorRotateCombo = PInspectorRotateBuild();
        pInspectorCropTool = PInspectorToolBuild();

        pInspectorApplyBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Crop.ApplyTooltip"));
        pInspectorApplyBox.Checked += (_, _) => LCropboxState.LCropboxApplySet(true);
        pInspectorApplyBox.Unchecked += (_, _) =>
        {
            LCropboxState.LCropboxApplySet(false);
            PInspectorRatioReset();
        };

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
}
