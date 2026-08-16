using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PProcessing
{
    private static readonly Brush pProcessingEngineActiveBrush = new SolidColorBrush(Color.FromRgb(0xCE, 0xE1, 0xFB));
    private static readonly Brush pProcessingEngineLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pProcessingEngineTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pProcessingEngineMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));

    public event Action<bool>? PProcessingEngineChange;

    private Border? pProcessingEngineRow;
    private bool pProcessingEngineMpv;
    private bool pProcessingEngineInstalled;

    public void PProcessingEngineSet(bool pEngineMpvActive, bool pEngineMpvInstalled)
    {
        pProcessingEngineMpv = pEngineMpvActive;
        pProcessingEngineInstalled = pEngineMpvInstalled;
        if (pProcessingEngineRow is null)
        {
            pProcessingEngineRow = PProcessingEngineBuild();
            PProcessingEngineMount(pProcessingEngineRow);
        }

        Border pToggle = PProcessingEngineToggleBuild();
        pToggle.HorizontalAlignment = HorizontalAlignment.Left;
        pProcessingEngineRow.Child = pToggle;
    }

    private void PProcessingEngineMount(Border pEngineRow)
    {
        if (pProcessingSkipRow.Parent is not DockPanel pRoot)
        {
            return;
        }

        DockPanel.SetDock(pEngineRow, Dock.Bottom);
        pRoot.Children.Insert(pRoot.Children.IndexOf(pProcessingSkipRow), pEngineRow);
    }

    private Border PProcessingEngineBuild() => new()
    {
        Padding = new Thickness(12, 8, 12, 8),
        Background = Brushes.White,
        BorderBrush = pProcessingEngineLineBrush,
        BorderThickness = new Thickness(0, 1, 0, 0)
    };

    private Border PProcessingEngineToggleBuild()
    {
        Border pFlyleaf = PProcessingEngineSegmentBuild(
            LLocalization.LLocalizationTextRead("Processing.Engine.Flyleaf"),
            LLocalization.LLocalizationTextRead("Processing.Engine.FlyleafTooltip"),
            !pProcessingEngineMpv,
            true,
            () => PProcessingEnginePick(false));
        Border pMpv = PProcessingEngineSegmentBuild(
            LLocalization.LLocalizationTextRead("Processing.Engine.Mpv"),
            LLocalization.LLocalizationTextRead(
                pProcessingEngineInstalled ? "Processing.Engine.MpvTooltip" : "Processing.Engine.MpvMissing"),
            pProcessingEngineMpv,
            pProcessingEngineInstalled,
            () => PProcessingEnginePick(true));

        var pInner = new StackPanel { Orientation = Orientation.Horizontal };
        pInner.Children.Add(pFlyleaf);
        pInner.Children.Add(new Border { Width = 1, Background = pProcessingEngineLineBrush });
        pInner.Children.Add(pMpv);

        return new Border
        {
            BorderBrush = pProcessingEngineLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            SnapsToDevicePixels = true,
            Child = pInner
        };
    }

    private static Border PProcessingEngineSegmentBuild(
        string pEngineText, string pEngineTip, bool pEngineActive, bool pEngineEnabled, Action pEngineClick)
    {
        var pLabel = new TextBlock
        {
            Text = pEngineText,
            FontSize = 12,
            FontFamily = pProcessingFontFamily,
            FontWeight = pEngineActive ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = pEngineActive ? pProcessingEngineTitleBrush : pProcessingEngineMutedBrush,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var pSegment = new Border
        {
            Background = pEngineActive ? pProcessingEngineActiveBrush : Brushes.Transparent,
            Padding = new Thickness(12, 3, 12, 3),
            Cursor = pEngineEnabled ? Cursors.Hand : Cursors.Arrow,
            Opacity = pEngineEnabled ? 1 : 0.4,
            ToolTip = pEngineTip,
            Child = pLabel
        };
        if (pEngineEnabled)
        {
            pSegment.MouseLeftButtonUp += (_, _) => pEngineClick();
        }

        return pSegment;
    }

    private void PProcessingEnginePick(bool pEngineMpv)
    {
        if (pEngineMpv == pProcessingEngineMpv)
        {
            return;
        }

        PProcessingEngineChange?.Invoke(pEngineMpv);
    }
}
