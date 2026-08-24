using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

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
    private bool pCurveCapable;
    private string pCurveDisabledKey = "Inspector.Video.CurveRequiresMpv";
    private int pCurveSelected = 1;
    private readonly List<LWorkCurvePoint>[] pCurveChannels =
    {
        PCurveIdentityCreate(),
        PCurveIdentityCreate(),
        PCurveIdentityCreate(),
        PCurveIdentityCreate()
    };

    private static List<LWorkCurvePoint> PCurveIdentityCreate() =>
        new() { new LWorkCurvePoint(0, 0), new LWorkCurvePoint(1, 1) };

    private StackPanel PCurveBuild()
    {
        pCurveBox = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"),
            LLocalization.LLocalizationTextRead("Inspector.Video.ApplyCurve"));
        pCurvePersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Video.PersistCurve"));

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
            pCurveSelected = Math.Clamp(pCurveSelected, 0, PCurveActiveRead().Count - 1);
            PCurveBoxesUpdate();
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
        pCurveInputValue.TextChanged += (_, _) => PCurveInputCommit();
        pCurveOutputValue = PInspectorDecimalBuild();
        pCurveOutputValue.TextChanged += (_, _) => PCurveOutputCommit();

        var pCurveDeletePoint = PCurveActionBuild(
            "Inspector.Video.CurveDeletePoint", PCurvePointDelete);
        var pCurveResetChannel = PCurveActionBuild(
            "Inspector.Video.CurveResetChannel", PCurveChannelReset);
        var pCurveResetAll = PCurveActionBuild(
            "Inspector.Video.CurveResetAll", PCurveAllReset);
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
        PCurveBoxesUpdate();

        pCurveBox.Checked += (_, _) => PToneApplyUpdate(pCurveBox, pCurveStack);
        pCurveBox.Unchecked += (_, _) => PToneApplyUpdate(pCurveBox, pCurveStack);

        pCurveBody = PToneBodyBuild(pCurveBox, pCurveStack);
        PToneApplyUpdate(pCurveBox, pCurveStack);
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

    public void PCurveCapabilitySet(bool pCurveCapable, string pCurveDisabledKey)
    {
        this.pCurveCapable = pCurveCapable;
        this.pCurveDisabledKey = pCurveDisabledKey;
        PInspectorSectionApply(
            pCurveBox, pCurvePersistent, pCurveStack, pCurveBody,
            pCurveCapable, pCurveDisabledKey,
            "Inspector.Video.ApplyCurve", "Inspector.Video.PersistCurve");
    }

    private List<LWorkCurvePoint> PCurveActiveRead() =>
        pCurveChannels[Math.Clamp(pCurveChannel.SelectedIndex, 0, pCurveChannels.Length - 1)];

    private void PCurveBoxesUpdate()
    {
        List<LWorkCurvePoint> pPoints = PCurveActiveRead();
        pCurveSelected = Math.Clamp(pCurveSelected, 0, pPoints.Count - 1);
        LWorkCurvePoint pPoint = pPoints[pCurveSelected];
        bool pPrevious = pInspectorVideoSuppress;
        pInspectorVideoSuppress = true;
        pCurveInputValue.Text = (pPoint.LWorkCurveInput * 100).ToString("0.#", CultureInfo.InvariantCulture);
        pCurveOutputValue.Text = (pPoint.LWorkCurveOutput * 100).ToString("0.#", CultureInfo.InvariantCulture);
        pInspectorVideoSuppress = pPrevious;
        PCurveRebuild();
    }

    private void PCurveInputCommit()
    {
        if (pInspectorVideoSuppress)
        {
            return;
        }

        List<LWorkCurvePoint> pPoints = PCurveActiveRead();
        if (pCurveSelected < 0 || pCurveSelected >= pPoints.Count)
        {
            return;
        }

        double pTyped = Math.Clamp(
            PInspectorDecimalRead(pCurveInputValue, pPoints[pCurveSelected].LWorkCurveInput * 100) / 100, 0, 1);
        double pInput;
        if (pCurveSelected == 0)
        {
            pInput = 0;
        }
        else if (pCurveSelected == pPoints.Count - 1)
        {
            pInput = 1;
        }
        else
        {
            pInput = Math.Clamp(
                pTyped,
                pPoints[pCurveSelected - 1].LWorkCurveInput + PCurveMinGap,
                pPoints[pCurveSelected + 1].LWorkCurveInput - PCurveMinGap);
        }

        pPoints[pCurveSelected] = new LWorkCurvePoint(pInput, pPoints[pCurveSelected].LWorkCurveOutput);
        PCurveBoxesUpdate();
        PInspectorVideoChange?.Invoke();
    }

    private void PCurveOutputCommit()
    {
        if (pInspectorVideoSuppress)
        {
            return;
        }

        List<LWorkCurvePoint> pPoints = PCurveActiveRead();
        if (pCurveSelected < 0 || pCurveSelected >= pPoints.Count)
        {
            return;
        }

        double pOutput = Math.Clamp(
            PInspectorDecimalRead(pCurveOutputValue, pPoints[pCurveSelected].LWorkCurveOutput * 100) / 100, 0, 1);
        pPoints[pCurveSelected] = new LWorkCurvePoint(pPoints[pCurveSelected].LWorkCurveInput, pOutput);
        PCurveRebuild();
        PInspectorVideoChange?.Invoke();
    }

    private void PCurvePointDelete()
    {
        List<LWorkCurvePoint> pPoints = PCurveActiveRead();
        if (pPoints.Count <= 2 || pCurveSelected <= 0 || pCurveSelected >= pPoints.Count - 1)
        {
            return;
        }

        pPoints.RemoveAt(pCurveSelected);
        pCurveSelected = Math.Clamp(pCurveSelected, 0, pPoints.Count - 1);
        PCurveBoxesUpdate();
        PInspectorVideoChange?.Invoke();
    }

    private void PCurveChannelReset()
    {
        pCurveChannels[Math.Clamp(pCurveChannel.SelectedIndex, 0, pCurveChannels.Length - 1)] =
            PCurveIdentityCreate();
        pCurveSelected = 1;
        PCurveBoxesUpdate();
        PInspectorVideoChange?.Invoke();
    }

    private void PCurveAllReset()
    {
        for (int pIndex = 0; pIndex < pCurveChannels.Length; pIndex++)
        {
            pCurveChannels[pIndex] = PCurveIdentityCreate();
        }

        pCurveSelected = 1;
        PCurveBoxesUpdate();
        PInspectorVideoChange?.Invoke();
    }
}
