using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

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
        StackPanel pBody = PCurvePanelBuild();
        PCurveAttach();
        return pBody;
    }

    private StackPanel PCurvePanelBuild()
    {
        pCurveBox = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyCurve"));
        pCurvePersistent = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistCurve"));

        pCurveChannel = PInspectorComboBuild();
        pCurveChannel.Items.Add(new LLocalizationChoice(
            "Master", "Inspector.Video.CurveChannelMaster"));
        pCurveChannel.Items.Add(new LLocalizationChoice(
            "Red", "Inspector.Video.CurveChannelRed"));
        pCurveChannel.Items.Add(new LLocalizationChoice(
            "Green", "Inspector.Video.CurveChannelGreen"));
        pCurveChannel.Items.Add(new LLocalizationChoice(
            "Blue", "Inspector.Video.CurveChannelBlue"));
        pCurveChannel.SelectedIndex = 0;

        pCurveCanvasHost = new Border
        {
            Height = 160,
            Margin = new Thickness(0, 0, 0, 8),
            BorderBrush = QPanel.QPanelLineRead(),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(0xF6, 0xF8, 0xFA)),
            Child = PCurveCanvasBuild()
        };

        pCurveInputValue = PInspectorDecimalBuild();
        pCurveOutputValue = PInspectorDecimalBuild();

        var pCurveDeletePoint = PCurveActionBuild(
            "Inspector.Video.CurveDeletePoint", QCurve.QCurvePointDelete);
        var pCurveResetChannel = PCurveActionBuild(
            "Inspector.Video.CurveResetChannel", QCurve.QCurveChannelReset);
        var pCurveResetAll = PCurveActionBuild(
            "Inspector.Video.CurveResetAll", QCurve.QCurveReset);
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
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveChannel"), pCurveChannel));
        pCurveStack.Children.Add(pCurveCanvasHost);
        pCurveStack.Children.Add(PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveInput"), pCurveInputValue));
        pCurveStack.Children.Add(PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveOutput"), pCurveOutputValue));
        pCurveStack.Children.Add(pCurveButtons);

        pCurveBody = PToneBodyBuild(pCurveBox, pCurveStack);
        return pCurveBody;
    }

    private void PCurveAttach()
    {
        PInspectorSwitchAttach(pCurveBox, QCurve.QCurveActiveSet);
        PInspectorSwitchAttach(pCurvePersistent, QCurve.QCurvePersistentSet);
        pCurveChannel.SelectionChanged += (_, _) => QCurve.QCurveChannelSelect(pCurveChannel.SelectedIndex);
        pCurveInputValue.TextChanged += (_, _) => PCurvePointCommit();
        pCurveOutputValue.TextChanged += (_, _) => PCurvePointCommit();
    }

    private static Button PCurveActionBuild(string pLabelKey, Action pClick)
    {
        Button pButton = PCurveCaptionBuild(pLabelKey);
        PInspectorClickAttach(pButton, pClick);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }

    private static Button PCurveCaptionBuild(string pLabelKey) => new()
    {
        Content = QLocalizationCatalog.QLocalizationTextRead(pLabelKey),
        Height = 28,
        MinWidth = 64,
        Padding = new Thickness(8, 0, 8, 0),
        FontSize = 11,
        FontFamily = pInspectorFontFamily
    };

    private void PCurvePointCommit() => QCurve.QCurvePointCommit(pCurveInputValue.Text, pCurveOutputValue.Text);

    private void PCurveUpdate()
    {
        PCurveStateApply();
        PInspectorSectionApply(pCurveBox, pCurvePersistent, pCurveStack, pCurveBody, QInspectorTip.QInspectorTipResolve(
            QCurve.QCurveActive,
            QCurve.QCurveCapable,
            QCurve.QCurvePreview,
            "Inspector.Video.CurveRequiresEq",
            "Inspector.Video.CurvePreviewMpv",
            "Inspector.Video.ApplyCurve",
            "Inspector.Video.PersistCurve"));
        PCurveRebuild();
    }

    private void PCurveStateApply()
    {
        PInspectorSwitchUpdate(pCurveBox, QCurve.QCurveActive);
        PInspectorSwitchUpdate(pCurvePersistent, QCurve.QCurvePersistent);
        pCurveChannel.SelectedIndex = QCurve.QCurveChannel;
        PInspectorTextSet(pCurveInputValue, QCurve.QCurveInputPercent, "0.#");
        PInspectorTextSet(pCurveOutputValue, QCurve.QCurveOutputPercent, "0.#");
    }
}
