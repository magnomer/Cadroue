using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PInspectorCurve
{
    private readonly PInspectorCurveCanvas pCurveCanvas;
    private readonly CheckBox pCurveBox;
    private readonly ComboBox pCurveChannel;
    private readonly Border pCurveCanvasHost;
    private readonly TextBox pCurveInputValue;
    private readonly TextBox pCurveOutputValue;
    private readonly StackPanel pCurveStack;

    internal PInspectorCurve(QCurve lCurve)
    {
        QCurve = lCurve;
        pCurveCanvas = new PInspectorCurveCanvas(lCurve);
        pCurveBox = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.ApplyCurve"));
        PCurvePersistent = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.PersistCurve"));

        pCurveChannel = PInspectorChoice.PInspectorComboBuild();
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
            Child = pCurveCanvas.PCurveCanvas
        };

        pCurveInputValue = PInspectorNumber.PInspectorDecimalBuild();
        pCurveOutputValue = PInspectorNumber.PInspectorDecimalBuild();

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
        pCurveStack.Children.Add(PInspectorField.PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveChannel"), pCurveChannel));
        pCurveStack.Children.Add(pCurveCanvasHost);
        pCurveStack.Children.Add(PInspectorField.PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveInput"), pCurveInputValue));
        pCurveStack.Children.Add(PInspectorField.PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Video.CurveOutput"), pCurveOutputValue));
        pCurveStack.Children.Add(pCurveButtons);

        PCurveBody = PInspectorField.PToneBodyBuild(pCurveBox, pCurveStack);
        PCurveAttach();
    }

    private QCurve QCurve { get; }

    internal CheckBox PCurvePersistent { get; }

    internal StackPanel PCurveBody { get; }

    private void PCurveAttach()
    {
        PInspectorSwitch.PInspectorSwitchAttach(pCurveBox, QCurve.QCurveActiveSet);
        PInspectorSwitch.PInspectorSwitchAttach(PCurvePersistent, QCurve.QCurvePersistentSet);
        pCurveChannel.SelectionChanged += (_, _) => QCurve.QCurveChannelSelect(pCurveChannel.SelectedIndex);
        pCurveInputValue.TextChanged += (_, _) => PCurvePointCommit();
        pCurveOutputValue.TextChanged += (_, _) => PCurvePointCommit();
    }

    internal void PCurveChangeAttach() => QCurve.QCurveChanged += PCurveUpdate;

    private static Button PCurveActionBuild(string pLabelKey, Action pClick)
    {
        Button pButton = PCurveCaptionBuild(pLabelKey);
        PInspectorButton.PInspectorClickAttach(pButton, pClick);
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
        FontFamily = PInspectorLook.PInspectorFontFamily
    };

    private void PCurvePointCommit() => QCurve.QCurvePointCommit(pCurveInputValue.Text, pCurveOutputValue.Text);

    internal void PCurveUpdate()
    {
        PCurveStateApply();
        PInspectorState.PInspectorSectionApply(
            pCurveBox, PCurvePersistent, pCurveStack, PCurveBody, QInspectorTip.QInspectorTipResolve(
            QCurve.QCurveActive,
            QCurve.QCurveCapable,
            QCurve.QCurvePreview,
            "Inspector.Video.CurveRequiresEq",
            "Inspector.Video.CurvePreviewMpv",
            "Inspector.Video.ApplyCurve",
            "Inspector.Video.PersistCurve"));
        pCurveCanvas.PCurveRebuild();
    }

    private void PCurveStateApply()
    {
        PInspectorSwitch.PInspectorSwitchUpdate(pCurveBox, QCurve.QCurveActive);
        PInspectorSwitch.PInspectorSwitchUpdate(PCurvePersistent, QCurve.QCurvePersistent);
        pCurveChannel.SelectedIndex = QCurve.QCurveChannel;
        PInspectorNumber.PInspectorTextSet(pCurveInputValue, QCurve.QCurveInputPercent, "0.#");
        PInspectorNumber.PInspectorTextSet(pCurveOutputValue, QCurve.QCurveOutputPercent, "0.#");
    }
}
