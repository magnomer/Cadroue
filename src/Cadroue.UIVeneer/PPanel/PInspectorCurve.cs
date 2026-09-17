using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private CheckBox pCurveBox = null!;
    private CheckBox pCurvePersistent = null!;
    private ComboBox pCurveChannel = null!;
    private Border pCurveCanvasHost = null!;
    private TextBox pCurveInputValue = null!;
    private TextBox pCurveOutputValue = null!;
    private StackPanel pCurveStack = null!;
    private StackPanel pCurveBody = null!;

    private StackPanel PCurveBuild()
    {
        pCurveBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Video.ApplyCurve"));
        pCurvePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Video.PersistCurve"));
        PInspectorSwitchAttach(pCurveBox, LCurve.LCurveActiveSet);
        PInspectorSwitchAttach(pCurvePersistent, LCurve.LCurvePersistentSet);

        pCurveChannel = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pCurveChannel);
        pCurveChannel.Items.Add(new LLocalizationChoice(
            "Master", "Inspector.Video.CurveChannelMaster"));
        pCurveChannel.Items.Add(new LLocalizationChoice(
            "Red", "Inspector.Video.CurveChannelRed"));
        pCurveChannel.Items.Add(new LLocalizationChoice(
            "Green", "Inspector.Video.CurveChannelGreen"));
        pCurveChannel.Items.Add(new LLocalizationChoice(
            "Blue", "Inspector.Video.CurveChannelBlue"));
        pCurveChannel.SelectedIndex = 0;
        pCurveChannel.SelectionChanged += (_, _) =>
        {
            if (pCurveChannel.SelectedIndex >= 0)
            {
                LCurve.LCurveChannelSelect(pCurveChannel.SelectedIndex);
            }
        };

        pCurveCanvasHost = new Border
        {
            Height = 160,
            Margin = new Thickness(0, 0, 0, 8),
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(0xF6, 0xF8, 0xFA)),
            Child = PCurveCanvasBuild()
        };

        pCurveInputValue = PInspectorDecimalBuild();
        pCurveInputValue.TextChanged += (_, _) => PCurvePointCommit();
        pCurveOutputValue = PInspectorDecimalBuild();
        pCurveOutputValue.TextChanged += (_, _) => PCurvePointCommit();

        var pCurveDeletePoint = PCurveActionBuild(
            "Inspector.Video.CurveDeletePoint", LCurve.LCurvePointDelete);
        var pCurveResetChannel = PCurveActionBuild(
            "Inspector.Video.CurveResetChannel", LCurve.LCurveChannelReset);
        var pCurveResetAll = PCurveActionBuild(
            "Inspector.Video.CurveResetAll", LCurve.LCurveReset);
        var pCurveButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 4, 0, 0)
        };
        pCurveDeletePoint.Margin = new Thickness(0, 0, 6, 0);
        pCurveResetChannel.Margin = new Thickness(0, 0, 6, 0);
        pCurveButtons.Children.Add(pCurveDeletePoint);
        pCurveButtons.Children.Add(pCurveResetChannel);
        pCurveButtons.Children.Add(pCurveResetAll);

        pCurveStack = new StackPanel();
        pCurveStack.Children.Add(PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Video.CurveChannel"), pCurveChannel));
        pCurveStack.Children.Add(pCurveCanvasHost);
        pCurveStack.Children.Add(PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Video.CurveInput"), pCurveInputValue));
        pCurveStack.Children.Add(PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Video.CurveOutput"), pCurveOutputValue));
        pCurveStack.Children.Add(pCurveButtons);

        pCurveBody = PToneBodyBuild(pCurveBox, pCurveStack);
        return pCurveBody;
    }

    private Button PCurveActionBuild(string pLabelKey, Action pClick)
    {
        var pButton = new Button
        {
            Content = LLocalization.LLocalizationTextRead(pLabelKey),
            Height = 28,
            MinWidth = 64,
            Padding = new Thickness(8, 0, 8, 0),
            FontSize = 11,
            FontFamily = pInspectorFontFamily,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => pClick();
        return pButton;
    }

    public void PCurveHistogramApply(LHistogramCounts? pHistogram) => LCurve.LCurveHistogramSet(pHistogram);

    private void PCurvePointCommit()
    {
        LWorkCurvePoint pPoint = LCurve.LCurvePointRead();
        double pInput = PInspectorDecimalRead(pCurveInputValue, pPoint.LWorkCurveInput * 100) / 100;
        double pOutput = PInspectorDecimalRead(pCurveOutputValue, pPoint.LWorkCurveOutput * 100) / 100;
        LCurve.LCurvePointSet(pInput, pOutput);
    }

    private void PCurveUpdate()
    {
        PInspectorSwitchUpdate(pCurveBox, LCurve.LCurveActive, false);
        PInspectorSwitchUpdate(pCurvePersistent, LCurve.LCurvePersistent, true);
        if (pCurveChannel.SelectedIndex != LCurve.LCurveChannel)
        {
            pCurveChannel.SelectedIndex = LCurve.LCurveChannel;
        }

        LWorkCurvePoint pPoint = LCurve.LCurvePointRead();
        PInspectorTextSet(pCurveInputValue, pPoint.LWorkCurveInput * 100, "0.#");
        PInspectorTextSet(pCurveOutputValue, pPoint.LWorkCurveOutput * 100, "0.#");
        PInspectorSectionApply(
            pCurveBox, pCurvePersistent, pCurveStack, pCurveBody, LCurve.LCurveActive,
            LCurve.LCurveCapable, LCurve.LCurvePreview,
            "Inspector.Video.CurveRequiresEq", "Inspector.Video.CurvePreviewMpv",
            "Inspector.Video.ApplyCurve", "Inspector.Video.PersistCurve");
        PCurveRebuild();
    }
}
