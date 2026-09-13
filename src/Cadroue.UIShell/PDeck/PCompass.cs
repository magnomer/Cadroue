using Cadroue.Core;
using Cadroue.Application;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PDeck;

public sealed partial class PCompass : UserControl
{
    private sealed record PCompassButton(
        PCompassAction PCompassButtonAction,
        string PCompassButtonIcon,
        string PCompassButtonLabel,
        string PCompassButtonTooltip,
        Action PCompassButtonHandler,
        bool PCompassButtonLast,
        bool PCompassButtonSection);

    private enum PCompassAction
    {
        PCompassZoomIn,
        PCompassZoomOut,
        PCompassPlayback,
        PCompassStandby,
        PCompassSectionNew,
        PCompassSectionDrop,
        PCompassSectionIn,
        PCompassSectionCut,
        PCompassSectionOut,
        PCompassKeyframePrevious,
        PCompassKeyframeNearest,
        PCompassKeyframeNext
    }

    private static readonly Brush pCompassPositiveBrush = new SolidColorBrush(Color.FromRgb(0x2F, 0x9E, 0x64));
    private static readonly Brush pCompassNegativeBrush = new SolidColorBrush(Color.FromRgb(0xD6, 0x45, 0x45));

    private readonly WrapPanel pCompassLinePanel;
    private readonly PFlowControl pCompassFlow;

    public PCompass(PFlowControl pFlow, bool pCompassSectionShow = false)
    {
        pCompassFlow = pFlow;
        pCompassLinePanel = new WrapPanel { VerticalAlignment = VerticalAlignment.Center };

        PCompassButton[] pButtons =
        {
            new(PCompassAction.PCompassZoomIn, "PCompassZoomIncrease.svg", "Compass.ZoomIn.Label", "Compass.ZoomIn.Tooltip", () => pFlow.PFlowShortcutDispatch("zoomIn"), false, false),
            new(PCompassAction.PCompassZoomOut, "PCompassZoomDecrease.svg", "Compass.ZoomOut.Label", "Compass.ZoomOut.Tooltip", () => pFlow.PFlowShortcutDispatch("zoomOut"), true, false),
            new(PCompassAction.PCompassPlayback, "PCompassPlay.svg", "Compass.Play.Label", "Compass.Play.Tooltip", PCompassPlayToggle, true, false),
            new(PCompassAction.PCompassSectionNew, "PCompassSectionAdd.svg", "Compass.SectionAdd.Label", "Compass.SectionAdd.Tooltip", () => pFlow.PFlowShortcutDispatch("addSection"), false, true),
            new(PCompassAction.PCompassSectionDrop, "PCompassRemove.svg", "Compass.SectionDelete.Label", "Compass.SectionDelete.Tooltip", () => pFlow.PFlowShortcutDispatch("deleteSection"), true, true),
            new(PCompassAction.PCompassSectionIn, "PCompassStart.svg", "Compass.SectionStart.Label", "Compass.SectionStart.Tooltip", () => pFlow.PFlowShortcutDispatch("setStart"), false, true),
            new(PCompassAction.PCompassSectionCut, "PCompassSplit.svg", "Compass.SectionSplit.Label", "Compass.SectionSplit.Tooltip", () => pFlow.PFlowShortcutDispatch("splitSection"), false, true),
            new(PCompassAction.PCompassSectionOut, "PCompassEnd.svg", "Compass.SectionEnd.Label", "Compass.SectionEnd.Tooltip", () => pFlow.PFlowShortcutDispatch("setEnd"), true, true),
            new(PCompassAction.PCompassKeyframePrevious, "PCompassKeyframePrevious.svg", "Compass.KeyframePrevious.Label", "Compass.KeyframePrevious.Tooltip", () => pFlow.PFlowShortcutDispatch("previousKey"), false, false),
            new(PCompassAction.PCompassKeyframeNearest, "PCompassKeyframeNear.svg", "Compass.KeyframeNearest.Label", "Compass.KeyframeNearest.Tooltip", () => pFlow.PFlowShortcutDispatch("nearestKey"), false, false),
            new(PCompassAction.PCompassKeyframeNext, "PCompassKeyframeNext.svg", "Compass.KeyframeNext.Label", "Compass.KeyframeNext.Tooltip", () => pFlow.PFlowShortcutDispatch("nextKey"), true, false)
        };

        StackPanel pGroup = PCompassGroupBuild();
        foreach (PCompassButton pEntry in pButtons)
        {
            if (pEntry.PCompassButtonSection && !pCompassSectionShow)
            {
                continue;
            }

            Button pButton = pEntry.PCompassButtonAction == PCompassAction.PCompassPlayback
                ? PCompassToggleBuild()
                : PCompassButtonBuild(pEntry.PCompassButtonAction, pEntry.PCompassButtonIcon, pEntry.PCompassButtonLabel, pEntry.PCompassButtonTooltip);
            pButton.Click += (_, _) => pEntry.PCompassButtonHandler();
            pGroup.Children.Add(pButton);
            if (pEntry.PCompassButtonLast)
            {
                pCompassLinePanel.Children.Add(pGroup);
                pGroup = PCompassGroupBuild();
            }
        }

        pCompassVolumeText = new TextBlock { Width = 32, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right };
        pCompassVolumeSlider = new Slider
        {
            Width = 132,
            Minimum = 0,
            Maximum = 100,
            VerticalAlignment = VerticalAlignment.Center,
            Focusable = false,
            ToolTip = LLocalization.LLocalizationTextRead("Compass.Volume.Label"),
            Style = PCompassSliderBuild()
        };
        pCompassVolumeSlider.ValueChanged += (_, _) => PCompassVolumeHandle(pFlow);
        pFlow.PFlowVolumeValue += PCompassValueHandle;
        pFlow.PFlowPlayingChange += PCompassPlayingApply;

        StackPanel pVolumeGroup = PCompassGroupBuild();
        pVolumeGroup.Children.Add(PCompassVolumeBuild());
        pVolumeGroup.Children.Add(PCompassWaveformBuild(pFlow));
        pCompassLinePanel.Children.Add(pVolumeGroup);
        PCompassValueHandle(LPreference.LPreferenceStateCurrent.LPreferenceVolume);
        pFlow.PFlowWaveformChange += PCompassWaveformApply;
        PCompassWaveformApply(pFlow.PFlowWaveformCheck());

        Content = new Border
        {
            MinHeight = 72,
            Child = pCompassLinePanel,
            SnapsToDevicePixels = true
        };
        pCompassLinePanel.SizeChanged += PCompassSizeHandle;
    }

    private static StackPanel PCompassGroupBuild()
    {
        var pGroup = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pGroup.Children.Add(PCompassSeparatorBuild());
        return pGroup;
    }

    private void PCompassSizeHandle(object sender, SizeChangedEventArgs e)
    {
        PCompassSeparatorUpdate();
    }

    private void PCompassSeparatorUpdate()
    {
        double pLineTop = double.NaN;
        foreach (UIElement pChild in pCompassLinePanel.Children)
        {
            if (pChild is not Panel pGroup || pGroup.Children.Count == 0) continue;
            double pGroupTop = pGroup.TranslatePoint(new Point(0, 0), pCompassLinePanel).Y;
            bool pLineStart = double.IsNaN(pLineTop) || pGroupTop > pLineTop;
            if (pLineStart)
            {
                pLineTop = pGroupTop;
            }

            if (pGroup.Children[0] is Border pSeparator)
            {
                pSeparator.Opacity = pLineStart ? 0 : 1;
            }
        }
    }

    private static Border PCompassSeparatorBuild() => new() { Width = 1, Margin = new Thickness(1, 14, 1, 12), Background = new SolidColorBrush(Color.FromRgb(0xDD, 0xE3, 0xEC)) };

    private static Brush? PCompassAccentRead(PCompassAction pAction) => pAction switch
    {
        PCompassAction.PCompassPlayback => pCompassPositiveBrush,
        PCompassAction.PCompassSectionDrop => pCompassNegativeBrush,
        _ => null
    };
}
