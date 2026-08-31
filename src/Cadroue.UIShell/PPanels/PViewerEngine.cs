using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Cadroue.Core;
using Cadroue.UIShell;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private static readonly Brush pViewerEngineActive = new SolidColorBrush(Color.FromRgb(0xCE, 0xE1, 0xFB));
    private static readonly Brush pViewerEngineLine = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pViewerEngineTitle = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pViewerEngineMuted = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly FontFamily pViewerEngineFont = new("Segoe UI");

    private void PViewerEngineShow()
    {
        bool pViewerEngineMpv = PViewerEngineRead() == LPreviewEngine.LPreviewEngineMpv;
        bool pViewerEngineInstalled = LMpv.LMpvInstalledCheck();
        pViewerEngineSurface.Child = PViewerEngineBuild(pViewerEngineMpv, pViewerEngineInstalled);
        pViewerEngineOverlay.Child = PViewerEngineBuild(pViewerEngineMpv, pViewerEngineInstalled);
    }

    private Border PViewerEngineBuild(bool pViewerEngineMpv, bool pViewerEngineInstalled)
    {
        static Border PViewerChoiceBuild(
            string pChoiceText, string pChoiceTip, bool pChoiceActive, bool pChoiceEnabled, Action pChoiceClick)
        {
            var pChoiceLabel = new TextBlock
            {
                Text = pChoiceText,
                FontSize = 12,
                FontFamily = pViewerEngineFont,
                FontWeight = pChoiceActive ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = pChoiceActive ? pViewerEngineTitle : pViewerEngineMuted,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var pChoice = new Border
            {
                Background = pChoiceActive ? pViewerEngineActive : Brushes.Transparent,
                Padding = new Thickness(12, 3, 12, 3),
                Cursor = pChoiceEnabled ? Cursors.Hand : Cursors.Arrow,
                Opacity = pChoiceEnabled ? 1 : 0.4,
                ToolTip = pChoiceTip,
                Child = pChoiceLabel
            };
            if (pChoiceEnabled)
            {
                pChoice.MouseLeftButtonUp += (_, _) => pChoiceClick();
            }

            return pChoice;
        }

        Border pViewerEngineFlyleaf = PViewerChoiceBuild(
            LLocalization.LLocalizationTextRead("Processing.Engine.Flyleaf"),
            LLocalization.LLocalizationTextRead("Processing.Engine.FlyleafTooltip"),
            !pViewerEngineMpv,
            true,
            () => PViewerEngineSelect(false));
        Border pViewerEngineMpvOption = PViewerChoiceBuild(
            LLocalization.LLocalizationTextRead("Processing.Engine.Mpv"),
            LLocalization.LLocalizationTextRead(
                pViewerEngineInstalled ? "Processing.Engine.MpvTooltip" : "Processing.Engine.MpvMissing"),
            pViewerEngineMpv,
            pViewerEngineInstalled,
            () => PViewerEngineSelect(true));

        var pViewerEngineInner = new StackPanel { Orientation = Orientation.Horizontal };
        pViewerEngineInner.Children.Add(pViewerEngineFlyleaf);
        pViewerEngineInner.Children.Add(new Border { Width = 1, Background = pViewerEngineLine });
        pViewerEngineInner.Children.Add(pViewerEngineMpvOption);

        return new Border
        {
            BorderBrush = pViewerEngineLine,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            SnapsToDevicePixels = true,
            Child = pViewerEngineInner
        };
    }

    private void PViewerEngineSelect(bool pViewerEngineMpv)
    {
        if (pViewerEngineMpv == (PViewerEngineRead() == LPreviewEngine.LPreviewEngineMpv))
        {
            return;
        }

        LPreferenceState pViewerEnginePreference = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        pViewerEnginePreference.LPreferencePreviewEngine =
            pViewerEngineMpv ? LRenderer.LRendererMpvToken : LRenderer.LRendererFlyleafToken;
        LPreference.LPreferenceStateSet(pViewerEnginePreference);
        LRenderer.LRendererEngineSet(
            pViewerEngineMpv ? LPreviewEngine.LPreviewEngineMpv : LPreviewEngine.LPreviewEngineFlyleaf);
    }

    private void PViewerEngineDetach()
    {
        (pViewerEngineSurface.Parent as Panel)?.Children.Remove(pViewerEngineSurface);
        (pViewerEngineOverlay.Parent as Panel)?.Children.Remove(pViewerEngineOverlay);
    }
}
